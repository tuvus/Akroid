using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

public class PlanetFaction {
    public readonly Planet planet;
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
        // See if we are at war with anyone
        if (faction.enemyFactions.Any(f => planet.planetFactions.ContainsKey(f))) {
            var enemyPlanetFactions = faction.enemyFactions.Where(f => planet.planetFactions.ContainsKey(f))
                .Select(f => planet.planetFactions[f]);
            // We are at war with another faction on the planet
            foreach (District borderDistrict in borderDistricts) {
                var attackList = planet.planetMap.GetNeighboringDistricts(borderDistrict)
                    .Where(nd => enemyPlanetFactions.Any(ef => nd.districtFactions.ContainsKey(ef)))
                    .Select(nd => (nd, GetValueOfDistrict(nd))).ToList()
                    .OrderByDescending(d => d.Item2);
                foreach (var districtToAttack in attackList.Select(d => d.nd)) {
                    var districtFactionsToAttack = districtToAttack.districtFactions
                        .Where(nd => enemyPlanetFactions.Contains(nd.Key)).Select(f => f.Key).ToHashSet();
                    var districtsToAttackFrom = planet.planetMap.GetNeighboringDistricts(districtToAttack)
                        .Where(d => d.owner == this && (d.districtFactions[this].districtAction !=
                            DistrictFaction.DistrictAction.Attack ||
                            d.districtFactions[this].targetDistrict == districtToAttack)).ToList();
                    // Calculate our combined forces
                    long ourForce = districtsToAttackFrom.Select(d =>
                            (long)(d.districtFactions[this].pop.marines * GetAttackRatioOfStrategy())).Sum() +
                        // Add the marines already in the district if there are any
                        (districtToAttack.districtFactions.ContainsKey(this) ?
                            districtToAttack.districtFactions[this].pop.marines : 0);

                    var districtsToDefendFrom = planet.planetMap.GetNeighboringDistricts(districtToAttack)
                        .Where(d => districtFactionsToAttack.Contains(d.owner)).Select(d => d.GetDistrictOwner())
                        .Where(df => !df.IsDefending() && !df.IsReinforcing(districtToAttack)).ToList();
                    // Calculate their combined forces
                    long theirForce = districtToAttack.districtFactions
                        .Where(df => enemyPlanetFactions.Contains(df.Key))
                        .Select(df => (long)(df.Value.pop.marines *
                            // If they are split attacking a different district then only take into account their defending forces
                            (df.Value.districtAction == DistrictFaction.DistrictAction.Attack &&
                                districtsToAttackFrom.All(d => df.Value.targetDistrict != d) ?
                                    df.Key.GetAttackRatioOfStrategy() : 1))).Sum();
                    theirForce += districtsToDefendFrom.Select(df =>
                        (long)(df.pop.marines * df.planetFaction.GetReinforceRatioOfStrategy() * .1f)).Sum();

                    if ((float)ourForce / theirForce > GetAttackBravenessOfStrategy()) {
                        districtsToAttackFrom.ForEach(d =>
                            d.districtFactions[this].SetAttackTarget(districtToAttack, GetAttackRatioOfStrategy()));
                    } else if ((float)ourForce / theirForce < GetRetreatCutoffOfStrategy()) {
                        districtsToAttackFrom.Where(d => d.districtFactions[this].districtAction ==
                                DistrictFaction.DistrictAction.Attack).ToList()
                            .ForEach(d => d.districtFactions[this].StopDistrictAction());
                    }
                }
            }
        }

        foreach (District borderDistrict in borderDistricts) {
            var districtFaction = borderDistrict.districtFactions[this];
            if (districtFaction.districtAction != DistrictFaction.DistrictAction.None) continue;
            var expandToList = planet.planetMap.GetNeighboringDistricts(borderDistrict)
                .Where(nd =>
                    nd.owner != this && nd.GetTotalControl() < 1 &&
                    nd.districtFactions.All(df => !faction.IsAtWarWithFaction(df.Key.faction)))
                .Select(nd => (nd, GetValueOfDistrict(nd))).ToList()
                .OrderByDescending(d => d.Item2);
            if (!expandToList.Any()) continue;
            districtFaction.SetExpandTarget(expandToList.First().nd, 0.1f);
        }
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
    public void FightFactionForTerritory(PlanetFaction defender, float forceToAttackWith, float deltaTime) { }

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

        Tuple<District, DistrictFaction> lastDistrict = null;
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
        Tuple<District, DistrictFaction> lastDistrict = null;
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

    public float GetTotalControl() {
        var districts = GetDistrictsPresent();
        if (!districts.Any()) return 0;
        return districts.Select(d => d.Item2.control)
            .Aggregate((sum, a) => sum + a) * 100f / planet.planetMap.districts.Count;
    }

    public List<(District, DistrictFaction)> GetDistrictsPresent() {
        return planet.planetMap.districts.Where(d => d.districtFactions.ContainsKey(this))
            .Select(d => (d, d.districtFactions[this])).ToList();
    }

    public float GetValueOfDistrict(District district) {
        return district.GetDistrictValue() +
            planet.planetMap.GetNeighboringDistricts(district).Count(d => d.owner == this) * 1.2f +
            -planet.planetMap.GetNeighboringDistricts(district).Count(d => d.owner != this) * .3f;
    }

    /// <summary>
    /// What percent of the force that this strategy attacks with
    /// </summary>
    public float GetAttackRatioOfStrategy() {
        return combatStrategy switch {
            CombatStrategy.AllOut => .95f,
            CombatStrategy.Risky => .75f,
            CombatStrategy.Balanced => .6f,
            CombatStrategy.Cautious => .4f,
        };
    }

    /// <summary>
    /// How much of a superiority this strategy likes to attack with
    /// </summary>
    public float GetAttackBravenessOfStrategy() {
        return combatStrategy switch {
            CombatStrategy.AllOut => .8f,
            CombatStrategy.Risky => 1f,
            CombatStrategy.Balanced => 1.2f,
            CombatStrategy.Cautious => 1.5f,
        };
    }

    public float GetReinforceRatioOfStrategy() {
        return combatStrategy switch {
            CombatStrategy.AllOut => .9f,
            CombatStrategy.Risky => .8f,
            CombatStrategy.Balanced => .7f,
            CombatStrategy.Cautious => .6f,
        };
    }

    /// <summary>
    /// How low does the friendly to enemy ratio need to be before they retreat
    /// </summary>
    /// <returns></returns>
    public float GetRetreatCutoffOfStrategy() {
        return combatStrategy switch {
            CombatStrategy.AllOut => .5f,
            CombatStrategy.Risky => .8f,
            CombatStrategy.Balanced => 1f,
            CombatStrategy.Cautious => 1.2f,
        };
    }
}
