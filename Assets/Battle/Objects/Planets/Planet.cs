using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Planet : BattleObject, IPositionConfirmer {
    [field: SerializeField] public long population { get; protected set; }

    public float rotationSpeed;
    public double rateOfGrowth;
    [SerializeField] long carryingCapacity;
    [SerializeField] long startingPop;
    [field: SerializeField] public long totalArea { get; protected set; }
    [field: SerializeField] public PlanetTerritory areas { get; protected set; }

    [SerializeField] float timeSinceStart;

    public Dictionary<Faction, PlanetFaction> planetFactions;
    private PlanetFaction unclaimedTerritory;

    public class PlanetFaction {
        private Planet planet;
        // If faction is null then this PlanetFaction represents unclaimed territory
        public Faction faction { get; private set; }
        public PlanetTerritory territory { get; private set; }
        public long force { get; private set; }
        public string special { get; private set; }

        public PlanetFaction(Planet planet, Faction faction, PlanetTerritory territory, long force, string special) {
            this.planet = planet;
            this.faction = faction;
            this.territory = territory;
            this.force = force;
            this.special = special;
        }

        //public void ChangeTerritory(long territoryChange) {
        //    territory += territoryChange;
        //}


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
            // Attackers get to attack with more force but defenders will loose less per force
            long attackersKilled = math.min(attackingForce, (long)(defenseForce * (1 + math.min(-bias, 0)) / 20));
            long defendersKilled = math.min(defenseForce, (long)(attackingForce * (1 + math.min(bias, 0)) / 50));
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
                print("Error");
            }
            // War is bad for everyone
            planet.population -= (long)((attackersKilled + defendersKilled) * 10 * (1 + math.abs(bias) * 2));
        }

        /// <summary> 
        /// Calculates how much territory this amount of force can reasonably attack.
        /// High quality territory is prefered over lower quality territory.
        /// </summary>
        private PlanetTerritory CreateWarZone(PlanetFaction defender, long attackingForce) {
            long territoryValueToAttack = math.max(1, attackingForce / 800);
            // The attacker can choose to attack areas that are higher quality
            long highQualityTerritory = math.min((long)((double)Random.Range(0.3f, 0.5f) * territoryValueToAttack / 2), defender.territory.highQualityArea);
            territoryValueToAttack -= highQualityTerritory * 2;
            long mediumQualityTerritory = math.min((long)((double)Random.Range(0.4f, 0.8f) * territoryValueToAttack), defender.territory.mediumQualityArea);
            territoryValueToAttack -= mediumQualityTerritory;
            long lowQualityTerritory = math.min(territoryValueToAttack * 2, defender.territory.lowQualityArea);
            return new PlanetTerritory(highQualityTerritory, mediumQualityTerritory, lowQualityTerritory);
        }

        private PlanetTerritory CalculateTerritoryTaken(PlanetFaction defender, PlanetTerritory warZone, long initialDefendingForce, long leftoverForce, double attackerDefenderRatio) {
            if (attackerDefenderRatio <= 1.5f)
                return new PlanetTerritory();

            // Calculate the value of territory that the defenders are guaranteed to keep
            long territoryValueDefended = warZone.GetTerritoryValue() * leftoverForce / initialDefendingForce;

            // The attacker will try to take the remaining contested territory based on the force ratio
            long territoryValueContested = warZone.GetTerritoryValue() - territoryValueDefended;
            long territoryGainedValue = (long)(territoryValueContested * math.max(1, attackerDefenderRatio - 1.5));

            long highQualityTerritoryGained = math.min(warZone.highQualityArea, territoryGainedValue / 6);
            territoryGainedValue -= highQualityTerritoryGained * 2;
            long mediumQualityTerritoryGained = math.min(warZone.mediumQualityArea, territoryGainedValue / 2);
            territoryGainedValue -= mediumQualityTerritoryGained;
            long lowQualityTerritoryGained = math.min(warZone.lowQualityArea, territoryGainedValue * 2);

            return new PlanetTerritory(highQualityTerritoryGained, mediumQualityTerritoryGained, lowQualityTerritoryGained);
        }
    }

    public class PlanetTerritory {
        public long highQualityArea;
        public long mediumQualityArea;
        public long lowQualityArea;

        public PlanetTerritory() {
            highQualityArea = 0;
            mediumQualityArea = 0;
            lowQualityArea = 0;
        }

        public PlanetTerritory(long highQualityArea = 0, long mediumQualityArea = 0, long lowQualityArea = 0) {
            this.highQualityArea = highQualityArea;
            this.mediumQualityArea = mediumQualityArea;
            this.lowQualityArea = lowQualityArea;
        }

        public long GetTotalAreas() {
            return highQualityArea + mediumQualityArea + lowQualityArea;
        }

        public long GetTerritoryValue() {
            return highQualityArea * 2 + mediumQualityArea + lowQualityArea / 2;
        }

        public void AddFrom(PlanetTerritory territory) {
            highQualityArea += territory.highQualityArea;
            mediumQualityArea += territory.mediumQualityArea;
            lowQualityArea += territory.lowQualityArea;
        }

        public void SubtractFrom(PlanetTerritory territory) {
            highQualityArea -= territory.highQualityArea;
            mediumQualityArea -= territory.mediumQualityArea;
            lowQualityArea -= territory.lowQualityArea;
        }
    }

    public struct PlanetData {
        public Faction faction;
        public string name;
        public float rotation;
        public long population;
        public double rateOfGrowth;
        public float highQualityLandFactor;
        public float mediumQualityLandFactor;
        public float lowQualityLandFactor;

        public PlanetData(Faction faction, string name, float rotation, long population, double rateOfGrowth, float highQualityLandFactor, float mediumQualityLandFactor, float lowQualityLandFactor) {
            this.faction = faction;
            this.name = name;
            this.rotation = rotation;
            this.population = population;
            this.rateOfGrowth = rateOfGrowth;
            this.highQualityLandFactor = highQualityLandFactor;
            this.mediumQualityLandFactor = mediumQualityLandFactor;
            this.lowQualityLandFactor = lowQualityLandFactor;
        }
    }

    public void SetupPlanet(BattleManager battleManager, BattleManager.PositionGiver positionGiver, PlanetData planetData) {
        this.faction = planetData.faction;
        float scale = Random.Range(.9f, 1.3f);
        transform.localScale = new Vector3(transform.localScale.x * scale, transform.localScale.y * scale, 1);
        base.SetupBattleObject(battleManager, positionGiver, planetData.rotation);
        objectName = planetData.name;
        this.population = planetData.population;
        this.rateOfGrowth = planetData.rateOfGrowth;
        SetPopulationTarget(population);
        rotationSpeed *= Random.Range(.5f, 1.5f);
        if (Random.Range(-1, 1) < 0) {
            rotationSpeed *= -1;
        }
        planetFactions = new Dictionary<Faction, PlanetFaction>();
        totalArea = (long)(math.pow(GetSize(), 2) * math.PI);
        areas = new PlanetTerritory((long)(totalArea * planetData.highQualityLandFactor), (long)(totalArea * planetData.mediumQualityLandFactor), (long)(totalArea * planetData.lowQualityLandFactor));
        unclaimedTerritory = new PlanetFaction(this, null, new PlanetTerritory(areas.highQualityArea, areas.mediumQualityArea, areas.lowQualityArea), 0, "This territory is open to claim.");
        Spawn();
    }

    /// <summary> Adds a planet faction to the planet with the faction, territory, force given </summary>
    public void AddFaction(Faction faction, PlanetTerritory territory, long force, string special) {
        territory.highQualityArea = math.min(territory.highQualityArea, GetUnclaimedFaction().territory.highQualityArea);
        territory.mediumQualityArea = math.min(territory.mediumQualityArea, GetUnclaimedFaction().territory.mediumQualityArea);
        territory.lowQualityArea = math.min(territory.lowQualityArea, GetUnclaimedFaction().territory.lowQualityArea);
        GetUnclaimedFaction().territory.SubtractFrom(territory);
        planetFactions.Add(faction, new PlanetFaction(this, faction, territory, force, special));
        faction.AddPlanet(this);
    }

    public void AddFaction(Faction faction, double highQualityAreaFactor, double mediumQualityAreaFactor, double lowQualityAreaFactor, long force, string special) {
        PlanetTerritory territory = new PlanetTerritory((long)(GetUnclaimedFaction().territory.highQualityArea * highQualityAreaFactor),
            (long)(GetUnclaimedFaction().territory.mediumQualityArea * mediumQualityAreaFactor),
            (long)(GetUnclaimedFaction().territory.lowQualityArea * lowQualityAreaFactor));
        AddFaction(faction, territory, force, special);
    }

    public void AddFaction(Faction faction, double highQualityAreaFactor, double mediumQualityAreaFactor, double lowQualityAreaFactor, double forceFraction, string special) {
        PlanetTerritory territory = new PlanetTerritory((long)(GetUnclaimedFaction().territory.highQualityArea * highQualityAreaFactor),
            (long)(GetUnclaimedFaction().territory.mediumQualityArea * mediumQualityAreaFactor),
            (long)(GetUnclaimedFaction().territory.lowQualityArea * lowQualityAreaFactor));
        long force = (long)(territory.GetTerritoryValue() * forceFraction * 100);
        AddFaction(faction, territory, force, special);
    }

    public void AddFaction(Faction faction, double territoryFactor, double forceFraction, string special) {
        AddFaction(faction, territoryFactor, territoryFactor, territoryFactor, (long)(GetUnclaimedFaction().territory.GetTerritoryValue() * territoryFactor * forceFraction * 100), special);
    }

    public PlanetFaction GetUnclaimedFaction() {
        return unclaimedTerritory;
    }

    protected override float SetupSize() {
        return GetSpriteSize() * transform.localScale.x;
    }

    public void UpdatePlanet(float deltaTime) {
        timeSinceStart += deltaTime;
        population = (long)((carryingCapacity / (1 + ((carryingCapacity / startingPop) - 1) * Mathf.Pow(math.E, (float)(-rateOfGrowth * timeSinceStart)))) * (-Mathf.Sin(timeSinceStart / 100) / 30.0 + 1));
        SetRotation(transform.eulerAngles.z + rotationSpeed * deltaTime);
    }

    public void SetPopulationTarget(long carryingCapacity) {
        timeSinceStart = 0;
        startingPop = population;
        this.carryingCapacity = carryingCapacity;
    }

    public void SetRateOfGrowth(double rateOfGrowth) {
        this.rateOfGrowth = rateOfGrowth;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.stars) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject + star.GetSize() + GetSize()) {
                return false;
            }
        }
        foreach (var asteroidField in BattleManager.Instance.asteroidFields) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <= minDistanceFromObject + asteroidField.GetSize() + GetSize()) {
                return false;
            }
        }
        foreach (var station in BattleManager.Instance.stations) {
            if (Vector2.Distance(position, station.GetPosition()) <= minDistanceFromObject + station.GetSize() + GetSize()) {
                return false;
            }
        }
        return true;
    }

    public override float GetSpriteSize() {
        return spriteRenderer.sprite.bounds.size.x / 2;
    }

    public long GetPopulation() {
        return population;
    }
}
