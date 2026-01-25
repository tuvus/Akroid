using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static Planet;

public class PlanetFaction {
    private readonly Planet planet;
    public float desiredForceFraction;

    public PlanetFaction(Planet planet, Faction faction, string special, CombatStrategy combatStrategy) {
        this.planet = planet;
        this.faction = faction;
        this.special = special;
        this.combatStrategy = combatStrategy;
        desiredForceFraction = PopulationCenter.marinePlanetRatio;
    }

    // If faction is null then this PlanetFaction represents unclaimed territory
    public Faction faction { get; }
    public string special { get; private set; }
    public CombatStrategy combatStrategy;

    public enum CombatStrategy {
        AllOut,
        Risky,
        Balanced,
        Cautious,
    }

    public void UpdateFaction(float deltaTime) {
        if (faction == null) return;
        UpdateExpansion(deltaTime);
    }

    private void UpdateExpansion(float deltaTime) {
        var controlledDistrict = planet.planetMap.districts.Where(d => d.owner == this);
        var borderDistricts = controlledDistrict
            .Where(cd => planet.planetMap.GetNeighboringDistricts(cd).Any(nd => nd.owner != this))
            .ToHashSet();
        if (faction.enemyFactions.Any(f => planet.planetFactions.ContainsKey(f))) {
            // We are at war with another faction on the planet
        }

        foreach (District borderDistrict in borderDistricts) {
            var districtFaction = borderDistrict.districtFactions[this];
            if (districtFaction.districtAction != District.DistrictFaction.DistrictAction.None) continue;
            var expandToList = planet.planetMap.GetNeighboringDistricts(borderDistrict)
                .Where(nd => nd.owner != this && nd.GetTotalControl() < 1)
                .Select(nd => (nd, GetValueOfDistrict(nd))).ToList()
                .OrderByDescending(d => d.Item2);
            if (!expandToList.Any()) continue;
            districtFaction.SetExpandTarget(expandToList.First().nd, 0.1f);
        }
        // if (planet.GetUnclaimedFaction().territory.GetTerritoryValue() > 0) {
        //     territoryExpansionProgress += population.marines * deltaTime / 500;
        //     if (territoryExpansionProgress >= 4) {
        //         float randomValue = Random.Range(0, 100);
        //         if (randomValue <= 50 && planet.GetUnclaimedFaction().territory.highQualityArea > 0) {
        //             planet.GetUnclaimedFaction().territory.highQualityArea -= 1;
        //             territory.highQualityArea += 1;
        //             territoryExpansionProgress -= 4;
        //         } else if (randomValue <= 90 && planet.GetUnclaimedFaction().territory.mediumQualityArea > 0) {
        //             planet.GetUnclaimedFaction().territory.mediumQualityArea -= 1;
        //             territory.mediumQualityArea += 1;
        //             territoryExpansionProgress -= 2;
        //         } else {
        //             planet.GetUnclaimedFaction().territory.lowQualityArea -= 1;
        //             territory.lowQualityArea += 1;
        //             territoryExpansionProgress -= 1;
        //         }
        //     }
        // }
    }

    /// <summary>
    ///     Makes this faction fight the defender in order to take their land. Both sides will loose forces and the planet will
    ///     loose population.
    /// </summary>
    /// <param name="forceToAttackWith">
    ///     A value between 0 and 1 which resembles how much of the faction's attack force it
    ///     should use.
    /// </param>
    public void FightFactionForTerritory(Faction otherFaction, float forceToAttackWith, float deltaTime) {
        FightFactionForTerritory(planet.planetFactions[otherFaction], forceToAttackWith, deltaTime);
    }

    /// <summary>
    ///     Makes this faction fight the defender in order to take their land. Both sides will loose forces and the planet will
    ///     loose population.
    /// </summary>
    /// <param name="forceToAttackWith">
    ///     A value between 0 and 1 which resembles how much of the faction's attack force it
    ///     should use.
    /// </param>
    public void FightFactionForTerritory(PlanetFaction defender, float forceToAttackWith, float deltaTime) {
        // Don't include garisons in the attack forces
        // long forcesDedicatedToAttack = math.max(population.marines / 6, population.marines - territory.GetTerritoryValue() * 10);
        // long attackingForce = (long)(forcesDedicatedToAttack * forceToAttackWith);
        // PlanetTerritory warZone = CreateWarZone(defender, attackingForce);
        // // Defense force is based on the forces stationed in the territory being attacked which includes some forces dedecated to attack as well.
        // long defenseForce = math.max(0,
        //     defender.population.marines * warZone.GetTerritoryValue() / defender.territory.GetTerritoryValue());
        //
        // // Random factor of the fight, a higher value means the attackers are doing better
        // float bias = Random.Range(-.3f, .3f);
        // float attackerModifiers = -5 + faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileDamage) +
        //     faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileReload)
        //     + faction.GetImprovementModifier(Faction.ImprovementAreas.LaserDamage) +
        //     faction.GetImprovementModifier(Faction.ImprovementAreas.LaserReload)
        //     + faction.GetImprovementModifier(Faction.ImprovementAreas.MissileDamage) +
        //     faction.GetImprovementModifier(Faction.ImprovementAreas.MissileReload);
        // float defenderModifiers = -5 +
        //     defender.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileDamage) +
        //     defender.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileReload)
        //     + defender.faction.GetImprovementModifier(Faction.ImprovementAreas.LaserDamage) +
        //     defender.faction.GetImprovementModifier(Faction.ImprovementAreas.LaserReload)
        //     + defender.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileDamage) +
        //     defender.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileReload);
        // // Attackers get to attack with more force but defenders will lose less per force
        // long attackersKilled = math.min(attackingForce,
        //     (long)(defenseForce * defenderModifiers * (1 + math.min(-bias, 0)) / 20));
        // long defendersKilled = math.min(defenseForce,
        //     (long)(attackingForce * attackerModifiers * (1 + math.min(bias, 0)) / 50));
        // population.marines -= attackersKilled;
        // defender.population.marines -= defendersKilled;
        //
        // PlanetTerritory territoryTaken;
        // if (defenseForce - defendersKilled <= 0) {
        //     territoryTaken = warZone;
        // } else {
        //     double attackerDefenderRatio =
        //         (attackingForce - attackersKilled) / (double)(defenseForce - defendersKilled);
        //     territoryTaken =
        //         CalculateTerritoryTaken(defender, warZone, defenseForce, defenseForce - defendersKilled,
        //             attackerDefenderRatio);
        // }
        //
        // defender.territory.SubtractFrom(territoryTaken);
        // territory.AddFrom(territoryTaken);
        // if (defender.territory.highQualityArea < 0 || defender.territory.mediumQualityArea < 0 ||
        //     defender.territory.lowQualityArea < 0) {
        //     Debug.LogError(
        //         $"{faction.name} is attacking but the defender {defender.faction.name} doesn't have any territory {defender.territory.highQualityArea}, {defender.territory.mediumQualityArea}, {defender.territory.lowQualityArea}.");
        // }
        //
        // // War is bad for everyone
        // population.civilians -= (long)(attackersKilled * 10 * (1 + math.abs(bias) * 2));
        // defender.population.civilians -= (long)(defendersKilled * 10 * (1 + math.abs(bias) * 2));
    }

    /// <summary>
    ///     Calculates how much territory this amount of force can reasonably attack.
    ///     High quality territory is prefered over lower quality territory.
    /// </summary>
    // private PlanetTerritory CreateWarZone(PlanetFaction defender, long attackingForce) {
    // long territoryValueToAttack = math.max(1, attackingForce / 800);
    // // The attacker can choose to attack areas that are higher quality
    // long highQualityTerritory = math.min((long)((double)Random.Range(0.3f, 0.5f) * territoryValueToAttack / 4),
    //     defender.territory.highQualityArea);
    // territoryValueToAttack -= highQualityTerritory * 2;
    // long mediumQualityTerritory = math.min((long)((double)Random.Range(0.4f, 0.8f) * territoryValueToAttack / 2),
    //     defender.territory.mediumQualityArea);
    // territoryValueToAttack -= mediumQualityTerritory;
    // long lowQualityTerritory = math.min(territoryValueToAttack, defender.territory.lowQualityArea);
    // return new PlanetTerritory(highQualityTerritory, mediumQualityTerritory, lowQualityTerritory);
    // return new PlanetTerritory();
    // }

    // private PlanetTerritory CalculateTerritoryTaken(PlanetFaction defender, PlanetTerritory warZone,
    // long initialDefendingForce,
    // long leftoverForce, double attackerDefenderRatio) {
    // if (attackerDefenderRatio <= 1.5f)
    // return new PlanetTerritory();

    // Calculate the value of territory that the defenders are guaranteed to keep
    // long territoryValueDefended = warZone.GetTerritoryValue() * leftoverForce / initialDefendingForce;

    // The attacker will try to take the remaining contested territory based on the force ratio
    // long territoryValueContested = warZone.GetTerritoryValue() - territoryValueDefended;
    // long territoryGainedValue = (long)(territoryValueContested * math.min(1, attackerDefenderRatio - 1.5));

    // long highQualityTerritoryGained = math.min(warZone.highQualityArea, territoryGainedValue / 6);
    // territoryGainedValue -= highQualityTerritoryGained * 2;
    // long mediumQualityTerritoryGained = math.min(warZone.mediumQualityArea, territoryGainedValue / 2);
    // territoryGainedValue -= mediumQualityTerritoryGained;
    // long lowQualityTerritoryGained = math.min(warZone.lowQualityArea, territoryGainedValue * 2);

    // return new PlanetTerritory(highQualityTerritoryGained, mediumQualityTerritoryGained, lowQualityTerritoryGained);
    // }
    public void AddForce(long force) {
        long totalForce = force;
        var totalNonMarinePop = GetTotalPopulation().TotalPopulationWithoutMarines();

        GetDistrictsPresent().ForEach(d => {
            long forceToAdd = totalForce * d.Item2.pop.TotalPopulationWithoutMarines() / totalNonMarinePop;
            d.Item2.pop.marines += forceToAdd;
            force -= forceToAdd;
        });
        var lastDistrict = GetDistrictsPresent().FirstOrDefault();
        if (force > 0 && lastDistrict.Item2 != null) {
            lastDistrict.Item2.pop.marines += force;
        }
    }

    public long RemoveForce(long force) {
        long totalForce = force;
        var totalMarinePop = GetTotalPopulation().marines;

        Tuple<District, District.DistrictFaction> lastDistrict = null;
        GetDistrictsPresent().ForEach(d => {
            lastDistrict = d.ToTuple();
            long forceToRemove = totalForce * d.Item2.pop.marines / totalMarinePop;
            d.Item2.pop.marines -= forceToRemove;
            force -= forceToRemove;
        });
        if (force > 0 && lastDistrict.Item2 != null) {
            var finalForce = math.max(0, force - lastDistrict.Item2.pop.marines);
            lastDistrict.Item2.pop.marines -= finalForce;
            return finalForce;
        }
        return force;
    }

    public void AddPopulation(Population population) {
        var initialPopulation = new Population(population);
        var districts = GetDistrictsPresent();
        long totalCapacity = districts.Select(d =>
                d.Item1.GetPopulationCapacity(this) - d.Item2.pop.TotalPopulation())
            .Aggregate((a, b) => a + b);
        Tuple<District, District.DistrictFaction> lastDistrict = null;
        districts.ForEach(d => {
            lastDistrict = d.ToTuple();
            var popToAdd = new Population(initialPopulation).Divide(
                (d.Item1.GetPopulationCapacity(this) - d.Item2.pop.TotalPopulation()) / (float)totalCapacity);
            d.Item2.pop.AddPopulation(popToAdd);
            population.SubtractPopulation(popToAdd);
        });
        if (population.TotalPopulation() > 0 && lastDistrict != null) {
            lastDistrict.Item2.pop.AddPopulation(population);
        }
    }

    public Population GetTotalPopulation() {
        var pop = new Population();
        GetDistrictsPresent().ForEach(d => pop.AddPopulation(d.Item2.pop));
        return pop;
    }

    public List<(District, District.DistrictFaction)> GetDistrictsPresent() {
        return planet.planetMap.districts.Where(d => d.districtFactions.ContainsKey(this))
            .Select(d => (d, d.districtFactions[this])).ToList();
    }

    public float GetValueOfDistrict(District district) {
        return district.GetDistrictValue() +
            planet.planetMap.GetNeighboringDistricts(district).Count(d => d.owner == this) * 1.2f +
            -planet.planetMap.GetNeighboringDistricts(district).Count(d => d.owner != this) * .3f;
    }
}
