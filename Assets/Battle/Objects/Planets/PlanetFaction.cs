using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static Planet;
using Random = UnityEngine.Random;

public class PlanetFaction {
    private Planet planet;
    // If faction is null then this PlanetFaction represents unclaimed territory
    public Faction faction { get; private set; }
    public PlanetTerritory territory { get; private set; }
    public long population { get; private set; }
    public long force { get; private set; }
    public string special { get; private set; }
    private double forceGainFraction;
    private double populationGainFraction;
    private double territoryExpansionProgress;

    public PlanetFaction(Planet planet, Faction faction, PlanetTerritory territory, long population, long force, string special) {
        this.planet = planet;
        this.faction = faction;
        this.territory = territory;
        this.population = population;
        this.force = force;
        this.special = special;
    }


    public void UpdateFaction(float deltaTime) {
        if (faction == null) return;

        UpdateForce(deltaTime);
        UpdatePopulation(deltaTime);
        UpdateExpansion(deltaTime);
    }

    
    private void UpdateForce(float deltaTime) {
        if (force > population) {
            force = population;
            return;
        }
        long desiredForce = population / 200;
        if (desiredForce > force) {
            long forceDifference = desiredForce - force;
            int factionsAtWarWith = 1 + planet.planetFactions.ToList().Count((f) => faction.IsAtWarWithFaction(f.Key));
            double forceRecruited = math.min(forceDifference, population * deltaTime / (10 * factionsAtWarWith) + forceGainFraction);
            force += (long)forceRecruited;
            forceGainFraction = forceRecruited - (long)forceRecruited;
        }
    }

    private void UpdatePopulation(float deltaTime) {
        long populationCapacity = territory.GetTerritoryValue() * populationPerTerritoryValue + 1;
        double populationCapacityRatio = 0;
        double populationGrowthPercent = 0;
        if (populationCapacity >= population) {
            populationCapacityRatio = populationCapacity * 50 / (population + 1);
            populationGrowthPercent = math.min(100, math.pow(populationCapacityRatio, 2) / 200);
        } else {
            populationCapacityRatio = -population * 50 / (populationCapacity + 1);
            populationGrowthPercent = math.max(-50, -math.pow(-populationCapacityRatio, 2.2) / 200);
        }
        double populationGained = populationGrowthPercent * population * deltaTime / 200000 + populationGainFraction;
        population = math.max(0, population + (long)populationGained);
        populationGainFraction = populationGained - (long)populationGained;
    }


    private void UpdateExpansion(float deltaTime) {
        if (planet.GetUnclaimedFaction().territory.GetTerritoryValue() > 0) {
            territoryExpansionProgress += force * deltaTime / 500;
            if (territoryExpansionProgress >= 4) {
                float randomValue = Random.Range(0, 100);
                if (randomValue <= 50 && planet.GetUnclaimedFaction().territory.highQualityArea > 0) {
                    planet.GetUnclaimedFaction().territory.highQualityArea -= 1;
                    territory.highQualityArea += 1;
                    territoryExpansionProgress -= 4;
                } else if (randomValue <= 90 && planet.GetUnclaimedFaction().territory.mediumQualityArea > 0) {
                    planet.GetUnclaimedFaction().territory.mediumQualityArea -= 1;
                    territory.mediumQualityArea += 1;
                    territoryExpansionProgress -= 2;
                } else {
                    planet.GetUnclaimedFaction().territory.lowQualityArea -= 1;
                    territory.lowQualityArea += 1;
                    territoryExpansionProgress -= 1;
                }
            }
        }
    }

    /// <summary>
    /// Makes this faction fight the defender in order to take their land. Both sides will loose forces and the planet will loose population.
    /// </summary>
    /// <param name="forceToAttackWith">A value between 0 and 1 which resembles how much of the faction's attack force it should use.</param>
    public void FightFactionForTerritory(Faction otherFaction, float forceToAttackWith, float deltaTime) {
        FightFactionForTerritory(planet.planetFactions[otherFaction], forceToAttackWith, deltaTime);
    }

    /// <summary>
    /// Makes this faction fight the defender in order to take their land. Both sides will loose forces and the planet will loose population.
    /// </summary>
    /// <param name="forceToAttackWith"> A value between 0 and 1 which resembles how much of the faction's attack force it should use. </param>
    public void FightFactionForTerritory(PlanetFaction defender, float forceToAttackWith, float deltaTime) {
        // Don't include garisons in the attack forces
        long forcesDedicatedToAttack = math.max(force / 6, force - territory.GetTerritoryValue() * 10);
        long attackingForce = (long)(forcesDedicatedToAttack * forceToAttackWith);
        PlanetTerritory warZone = CreateWarZone(defender, attackingForce);
        // Defense force is based on the forces stationed in the territory being attacked which includes some forces dedecated to attack as well.
        long defenseForce = math.max(0, defender.force * warZone.GetTerritoryValue() / defender.territory.GetTerritoryValue());

        // Random factor of the fight, a higher value means the attackers are doing better
        float bias = Random.Range(-.3f, .3f);
        float attackerModifiers = -5 + faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileDamage) + faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileReload)
            + faction.GetImprovementModifier(Faction.ImprovementAreas.LaserDamage) + faction.GetImprovementModifier(Faction.ImprovementAreas.LaserReload)
            + faction.GetImprovementModifier(Faction.ImprovementAreas.MissileDamage) + faction.GetImprovementModifier(Faction.ImprovementAreas.MissileReload);
        float defenderModifiers = -5 + defender.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileDamage) + defender.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileReload)
            + defender.faction.GetImprovementModifier(Faction.ImprovementAreas.LaserDamage) + defender.faction.GetImprovementModifier(Faction.ImprovementAreas.LaserReload)
            + defender.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileDamage) + defender.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileReload);
        // Attackers get to attack with more force but defenders will loose less per force
        long attackersKilled = math.min(attackingForce, (long)(defenseForce * defenderModifiers * (1 + math.min(-bias, 0)) / 20));
        long defendersKilled = math.min(defenseForce, (long)(attackingForce * attackerModifiers * (1 + math.min(bias, 0)) / 50));
        force -= attackersKilled;
        defender.force -= defendersKilled;

        PlanetTerritory territoryTaken;
        if (defenseForce - defendersKilled <= 0) {
            territoryTaken = warZone;
        } else {
            double attackerDefenderRatio = (attackingForce - attackersKilled) / (double)(defenseForce - defendersKilled);
            territoryTaken = CalculateTerritoryTaken(defender, warZone, defenseForce, defenseForce - defendersKilled, attackerDefenderRatio);
        }
        defender.territory.SubtractFrom(territoryTaken);
        territory.AddFrom(territoryTaken);
        if (defender.territory.highQualityArea < 0 || defender.territory.mediumQualityArea < 0 || defender.territory.lowQualityArea < 0) {
            Debug.LogError($"{faction.name} is attacking but the defender {defender.faction.name} doesn't have any territory {defender.territory.highQualityArea}, {defender.territory.mediumQualityArea}, {defender.territory.lowQualityArea}.");
        }
        // War is bad for everyone
        population -= (long)(attackersKilled * 10 * (1 + math.abs(bias) * 2));
        defender.population -= (long)(defendersKilled * 10 * (1 + math.abs(bias) * 2));
    }

    /// <summary> 
    /// Calculates how much territory this amount of force can reasonably attack.
    /// High quality territory is prefered over lower quality territory.
    /// </summary>
    private PlanetTerritory CreateWarZone(PlanetFaction defender, long attackingForce) {
        long territoryValueToAttack = math.max(1, attackingForce / 800);
        // The attacker can choose to attack areas that are higher quality
        long highQualityTerritory = math.min((long)((double)Random.Range(0.3f, 0.5f) * territoryValueToAttack / 4), defender.territory.highQualityArea);
        territoryValueToAttack -= highQualityTerritory * 2;
        long mediumQualityTerritory = math.min((long)((double)Random.Range(0.4f, 0.8f) * territoryValueToAttack / 2), defender.territory.mediumQualityArea);
        territoryValueToAttack -= mediumQualityTerritory;
        long lowQualityTerritory = math.min(territoryValueToAttack, defender.territory.lowQualityArea);
        return new PlanetTerritory(highQualityTerritory, mediumQualityTerritory, lowQualityTerritory);
    }

    private PlanetTerritory CalculateTerritoryTaken(PlanetFaction defender, PlanetTerritory warZone, long initialDefendingForce, long leftoverForce, double attackerDefenderRatio) {
        if (attackerDefenderRatio <= 1.5f)
            return new PlanetTerritory();

        // Calculate the value of territory that the defenders are guaranteed to keep
        long territoryValueDefended = warZone.GetTerritoryValue() * leftoverForce / initialDefendingForce;

        // The attacker will try to take the remaining contested territory based on the force ratio
        long territoryValueContested = warZone.GetTerritoryValue() - territoryValueDefended;
        long territoryGainedValue = (long)(territoryValueContested * math.min(1, attackerDefenderRatio - 1.5));

        long highQualityTerritoryGained = math.min(warZone.highQualityArea, territoryGainedValue / 6);
        territoryGainedValue -= highQualityTerritoryGained * 2;
        long mediumQualityTerritoryGained = math.min(warZone.mediumQualityArea, territoryGainedValue / 2);
        territoryGainedValue -= mediumQualityTerritoryGained;
        long lowQualityTerritoryGained = math.min(warZone.lowQualityArea, territoryGainedValue * 2);

        return new PlanetTerritory(highQualityTerritoryGained, mediumQualityTerritoryGained, lowQualityTerritoryGained);
    }

    public void AddForce(long force) {
        this.population += force;
        this.force += force;
    }

    public long RemoveForce(long force) {
        force = math.min(this.force, force);
        this.force -= force;
        return force;
    }

    public void AddPopulation(long population) {
        this.population += population;
    }
}
