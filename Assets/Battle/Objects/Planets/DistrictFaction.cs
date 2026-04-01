using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class DistrictFaction {
    public District district;
    public PlanetFaction planetFaction;
    public Population pop;
    public float control;
    private double populationGainFraction;
    private double forceGainFraction;

    public DistrictAction districtAction;
    public District targetDistrict;
    public float targetAmount;

    public enum DistrictAction {
        None,
        Expand,
        Attack,
        Reinforce,
        Retreat
    }

    public DistrictFaction(District district, PlanetFaction planetFaction, Population pop, float control) {
        this.district = district;
        this.planetFaction = planetFaction;
        this.pop = pop;
        this.control = control;
        districtAction = DistrictAction.None;
        targetDistrict = null;
        targetAmount = 0;
    }

    public void Update(float deltaTime) {
        // Expand control
        var uncontrolledArea = 1 - district.GetTotalControl();
        if (uncontrolledArea > 0) {
            //TODO: Improve this function to slow the expansion of medium population districts
            var areaToControl = math.min(uncontrolledArea,
                (pop.civilians * .0000003f + pop.marines * 0.00003f) * deltaTime / math.max(1, (district.area * district.landPercent)));
            control += areaToControl;
        }

        // Increase Population
        double popCapRatio = math.min(2, (double)pop.TotalPopulation() / district.GetPopulationCapacity(planetFaction));
        if (double.IsNaN(popCapRatio)) popCapRatio = 0;
        double populationGained =
            pop.TotalPopulationWithoutMarines() * District.terrainModifiers[district.terrainType].popGrowth *
            (1 - popCapRatio) * deltaTime * .003 + populationGainFraction;
        pop.civilians = math.max(0, pop.civilians + (long)populationGained);
        populationGainFraction = populationGained - (long)populationGained;

        // Recruit forces
        long desiredForce = (long)(pop.civilians * planetFaction.desiredForceFraction);
        if (desiredForce > pop.marines) {
            long forceDifference = desiredForce - pop.marines;
            double forceRecruited = math.min(forceDifference, pop.civilians * deltaTime * .0001);
            pop.marines += (long)forceRecruited;
            pop.civilians -= (long)forceRecruited;
            forceGainFraction = forceRecruited - (long)forceRecruited;
        }

        if (districtAction == DistrictAction.Expand)
            DoExpandAction(deltaTime);

        if (control <= 0) {
            district.RemoveFaction(planetFaction);
        }
    }

    private void DoExpandAction(float deltaTime) {
        if (targetDistrict.GetTotalControl() >= 1 &&
            !targetDistrict.districtFactions.ContainsKey(planetFaction)) {
            StopDistrictAction();
            return;
        }
        if (!targetDistrict.districtFactions.ContainsKey(planetFaction)) {
            targetDistrict.AddFaction(planetFaction, 0, 0);
        }
        if (targetDistrict.districtFactions[planetFaction].pop.civilians <
            targetDistrict.GetPopulationCapacity() * (1 - targetDistrict.GetTotalControl() +
                targetDistrict.districtFactions[planetFaction].control) * targetAmount) {
            var targetDistrictFaction = targetDistrict.districtFactions[planetFaction];
            var popToMove = (long)(math.max(pop.civilians / 2f, pop.civilians * deltaTime * targetAmount * .001f));
            targetDistrictFaction.pop.civilians += popToMove;
            pop.civilians -= popToMove;
            var militaryToMove = (long)(math.max(pop.marines / 2f, pop.marines * deltaTime * targetAmount * .0001f));
            targetDistrictFaction.pop.marines += militaryToMove;
            pop.marines -= militaryToMove;
        } else if (targetDistrict.GetTotalControl() >= 1) {
            StopDistrictAction();
            return;
        }

        if (targetDistrict.GetTotalControl() < 1) {
            targetDistrict.districtFactions[planetFaction].AddControl(0.0001f * deltaTime);
        }
    }

    public void AddControl(float controlToAdd) {
        control += math.min(controlToAdd, 1 - district.GetTotalControl());
    }

    public void SetExpandTarget(District targetDistrict, float popToMigrate) {
        StopDistrictAction();
        districtAction = DistrictAction.Expand;
        this.targetDistrict = targetDistrict;
        targetAmount = popToMigrate;
    }

    public void SetAttackTarget(District districtToAttack, float engagedMarinesFactor) {
        if (districtAction == DistrictAction.Attack && districtToAttack == targetDistrict &&
            Mathf.Approximately(engagedMarinesFactor, targetAmount))
            return;
        StopDistrictAction();
        if (districtToAttack.districtFactions.Any(df =>
            planetFaction.faction.IsAtWarWithFaction(df.Key.faction) &&
            df.Value.districtAction == DistrictAction.Attack && df.Value.targetDistrict == district)) {
            // We can't attack into districts that are attacking us
            return;
        }
        districtAction = DistrictAction.Attack;
        targetDistrict = districtToAttack;
        targetAmount = engagedMarinesFactor;
        if (!planetFaction.planet.districtsInCombat.ContainsKey(targetDistrict))
            planetFaction.planet.districtsInCombat.Add(targetDistrict,
                new Planet.DistrictCombat(targetDistrict));
        planetFaction.planet.districtsInCombat[targetDistrict].attackers.Add(this);
    }

    public void SetReinforceTarget(District districtToReinforce, float reinforceFactor) { }

    public void StopDistrictAction() {
        if (districtAction == DistrictAction.Attack) {
            if (planetFaction.planet.districtsInCombat.TryGetValue(district,
                out Planet.DistrictCombat combat)) {
                combat.RemoveAttacker(this);
            }
        }
        districtAction = DistrictAction.None;
    }

    public bool IsDefending() {
        return planetFaction.planet.districtsInCombat.ContainsKey(district) &&
            planetFaction.planet.districtsInCombat[district].attackers
                .Any(a => planetFaction.faction.IsAtWarWithFaction(a.planetFaction.faction));
    }

    public bool IsReinforcing(District districtNotReinforcing = null) {
        return districtAction == DistrictAction.Reinforce && targetDistrict != districtNotReinforcing;
    }
}
