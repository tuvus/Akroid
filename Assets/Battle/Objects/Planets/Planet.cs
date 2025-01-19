using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class Planet : BattleObject, IPositionConfirmer {
    public PlanetScriptableObject planetScriptableObject { get; private set; }

    /// <summary> Determines the base amount of population that one territory value can hold. </summary>
    public static readonly long populationPerTerritoryValue = 15000;

    public float rotationSpeed;
    [SerializeField] long startingPop;
    [field: SerializeField] public long totalArea { get; protected set; }
    [field: SerializeField] public PlanetTerritory areas { get; protected set; }

    [SerializeField] float timeSinceStart;

    public Dictionary<Faction, PlanetFaction> planetFactions;
    private PlanetFaction unclaimedTerritory;

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
            return highQualityArea * 4 + mediumQualityArea * 2 + lowQualityArea;
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

        public void AddRandomTerritory(long value, Random random) {
            highQualityArea = (long)(random.NextFloat(.2f, .5f) * value / 4.0);
            value -= highQualityArea * 4;
            mediumQualityArea = (long)(random.NextFloat(.4f, .7f) * value / 2.0);
            value -= mediumQualityArea * 2;
            lowQualityArea = value;
        }
    }

    public struct PlanetData {
        public BattleObjectData battleObjectData;
        public float highQualityLandFactor;
        public float mediumQualityLandFactor;
        public float lowQualityLandFactor;

        public PlanetData(BattleObjectData battleObjectData, float highQualityLandFactor, float mediumQualityLandFactor,
            float lowQualityLandFactor) {
            this.battleObjectData = battleObjectData;
            this.highQualityLandFactor = highQualityLandFactor;
            this.mediumQualityLandFactor = mediumQualityLandFactor;
            this.lowQualityLandFactor = lowQualityLandFactor;
        }
    }

    public Planet(PlanetData planetData, BattleManager battleManager, PlanetScriptableObject planetScriptableObject) : base(planetData.battleObjectData, battleManager) {
        this.planetScriptableObject = planetScriptableObject;
        rotationSpeed *= random.NextFloat(.5f, 1.5f);
        if (random.NextFloat(-1, 1) < 0) {
            rotationSpeed *= -1;
        }

        planetFactions = new Dictionary<Faction, PlanetFaction>();
        visible = true;
        Spawn();
        SetSize(SetupSize());

        totalArea = (long)(math.pow(GetSize(), 2) * math.PI);
        areas = new PlanetTerritory((long)(totalArea * planetData.highQualityLandFactor),
            (long)(totalArea * planetData.mediumQualityLandFactor), (long)(totalArea * planetData.lowQualityLandFactor));
        unclaimedTerritory = new PlanetFaction(this, null,
            new PlanetTerritory(areas.highQualityArea, areas.mediumQualityArea, areas.lowQualityArea), 0, 0,
            "This territory is open to claim.");
    }

    /// <summary> Adds a planet faction to the planet with the faction, territory, force given </summary>
    public void AddFaction(Faction faction, PlanetTerritory territory, long population, long force, string special) {
        territory.highQualityArea = math.min(territory.highQualityArea, GetUnclaimedFaction().territory.highQualityArea);
        territory.mediumQualityArea = math.min(territory.mediumQualityArea, GetUnclaimedFaction().territory.mediumQualityArea);
        territory.lowQualityArea = math.min(territory.lowQualityArea, GetUnclaimedFaction().territory.lowQualityArea);
        GetUnclaimedFaction().territory.SubtractFrom(territory);
        planetFactions.Add(faction, new PlanetFaction(this, faction, territory, population, force, special));
        faction.AddPlanet(this);
    }

    public void AddFaction(Faction faction, double highQualityAreaFactor, double mediumQualityAreaFactor, double lowQualityAreaFactor,
        long population, long force, string special) {
        PlanetTerritory territory = new PlanetTerritory((long)(GetUnclaimedFaction().territory.highQualityArea * highQualityAreaFactor),
            (long)(GetUnclaimedFaction().territory.mediumQualityArea * mediumQualityAreaFactor),
            (long)(GetUnclaimedFaction().territory.lowQualityArea * lowQualityAreaFactor));
        AddFaction(faction, territory, population, force, special);
    }

    public void AddFaction(Faction faction, double highQualityAreaFactor, double mediumQualityAreaFactor, double lowQualityAreaFactor,
        long population, double forceFraction, string special) {
        PlanetTerritory territory = new PlanetTerritory((long)(GetUnclaimedFaction().territory.highQualityArea * highQualityAreaFactor),
            (long)(GetUnclaimedFaction().territory.mediumQualityArea * mediumQualityAreaFactor),
            (long)(GetUnclaimedFaction().territory.lowQualityArea * lowQualityAreaFactor));
        long force = (long)(population * forceFraction);
        AddFaction(faction, territory, population, force, special);
    }

    public void AddFaction(Faction faction, double territoryFactor, long population, double forceFraction, string special) {
        AddFaction(faction, territoryFactor, territoryFactor, territoryFactor, population, forceFraction, special);
    }

    public void AddColony(Faction faction, long population, string special) {
        long teritoryValue = population / populationPerTerritoryValue;
        long highQualityTerritories = math.min(GetUnclaimedFaction().territory.highQualityArea, teritoryValue / 4);
        teritoryValue -= highQualityTerritories * 2;
        long mediumQualityTerritories = math.min(GetUnclaimedFaction().territory.mediumQualityArea, teritoryValue / 2);
        teritoryValue -= mediumQualityTerritories * 2;
        AddFaction(faction, new PlanetTerritory(highQualityTerritories, mediumQualityTerritories, teritoryValue), population,
            population / 10, special);
    }

    public void RemoveFaction(Faction faction) {
        GetUnclaimedFaction().territory.AddFrom(planetFactions[faction].territory);
        planetFactions.Remove(faction);
        faction.RemovePlanet(this);
    }

    public PlanetFaction GetUnclaimedFaction() {
        return unclaimedTerritory;
    }

    protected override float SetupSize() {
        return GetSpriteSize() * scale.x;
    }

    public void UpdatePlanet(float deltaTime) {
        timeSinceStart += deltaTime;
        SetRotation(rotation + rotationSpeed * deltaTime);
        foreach (var faction in planetFactions) {
            faction.Value.UpdateFaction(deltaTime);
        }
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

        foreach (var planet in BattleManager.Instance.planets) {
            if (Vector2.Distance(position, planet.GetPosition()) <= minDistanceFromObject + planet.GetSize() + GetSize()) {
                return false;
            }
        }

        return true;
    }

    public long GetPopulation() {
        return planetFactions.Sum((f) => f.Value.population);
    }

    public override float GetSpriteSize() {
        return Calculator.GetSpriteSizeFromBounds(planetScriptableObject.spriteBounds, scale);
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/Planet");
    }
}
