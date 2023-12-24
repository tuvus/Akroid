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
    public long areas { get; protected set; }
    public float landFactor { get; protected set; }
    [SerializeField] float timeSinceStart;

    public Dictionary<Faction, PlanetFaction> planetFactions;
    private PlanetFaction unclaimedTerritory;

    public class PlanetFaction {
        private Planet planet;
        // If faction is null then this PlanetFaction represents unclaimed territory
        public Faction faction { get; private set; }
        public long territory { get; private set; }
        public long force { get; private set; }
        public string special { get; private set; }

        public PlanetFaction(Planet planet ,Faction faction, long territory, long force, string special) {
            this.planet = planet;
            this.faction = faction;
            this.territory = territory;
            this.force = force;
            this.special = special;
        }

        public void ChangeTerritory(long territoryChange) {
            territory += territoryChange;
        }

        public void FightFactionForTerritory(Faction otherFaction, long attackWithForce, float deltaTime) {
            FightFactionForTerritory(planet.planetFactions[otherFaction], attackWithForce, deltaTime);
        }

        public void FightFactionForTerritory(PlanetFaction defender, long attackWithForce, float deltaTime) {
            long defenseForce = math.max(0,  (long)(defender.force / (math.max(1, defender.territory) * .8d)));
            long attackersKilled = math.min(attackWithForce, (long)(defenseForce * 10 * deltaTime));
            long defendersKilled = math.min(defenseForce, (long)(attackWithForce * 10 * deltaTime));
            force -= attackersKilled;
            defender.force -= defendersKilled;
            double attackerDefenderRatio = (attackWithForce - attackersKilled) / (double)(defenseForce - defendersKilled + 1);
            if (attackerDefenderRatio < 1) return;
            long territoryTaken = math.min(defender.territory, math.max(0, (long)(100 * attackerDefenderRatio * deltaTime)));
            defender.territory -= territoryTaken;
            territory += territoryTaken;
            planet.population -= (attackersKilled + defendersKilled) * 2;

        }
    }

    public void SetupPlanet(string name, Faction faction, BattleManager.PositionGiver positionGiver, long population, double rateOfGrowth, float rotation, float landFactor = 1) {
        this.faction = faction;
        float scale = Random.Range(.9f, 1.3f);
        transform.localScale = new Vector3(transform.localScale.x * scale, transform.localScale.y * scale, 1);
        base.SetupBattleObject(positionGiver, rotation);
        objectName = name;
        this.population = population;
        this.rateOfGrowth = rateOfGrowth;
        SetPopulationTarget(population);
        rotationSpeed *= Random.Range(.5f, 1.5f);
        if (Random.Range(-1, 1) < 0) {
            rotationSpeed *= -1;
        }
        this.landFactor = landFactor;
        planetFactions = new Dictionary<Faction, PlanetFaction>();
        areas = (long)(math.pow(GetSize(), 2) * math.PI * landFactor);
        unclaimedTerritory = new PlanetFaction(this, null, areas, 0, "This territory is open to claim.");
        Spawn();
    }

    public void AddFaction(Faction faction, long territory, long force, string special) {
        territory = math.min(territory, GetUnclaimedFaction().territory);
        GetUnclaimedFaction().ChangeTerritory(-territory);
        planetFactions.Add(faction, new PlanetFaction(this, faction, territory, force, special));
        faction.AddPlanet(this);
    }

    public void AddFactionTerritoryFraction(Faction faction, double territoryFraction, long force, string special) {
        AddFaction(faction, (long)(GetUnclaimedFaction().territory * territoryFraction), force, special);
    }

    public void AddFactionTerritoryForceFraction(Faction faction, double territoryFraction, double forceFraction, string special) {
        AddFaction(faction, (long)(GetUnclaimedFaction().territory * territoryFraction), (long)(GetUnclaimedFaction().territory * territoryFraction * forceFraction), special);
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
            if (Vector2.Distance(position, star.GetPosition()) <= minDistanceFromObject + star.GetSize() + GetSize()) {
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
