using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class Planet : BattleObject, IPositionConfirmer {
    /// <summary> Determines the base amount of population that one territory value can hold. </summary>
    public static readonly long populationPerTerritoryValue = 15000;

    public enum PlanetType {
        Terran,
        Moon,
        GasGiant,
    }

    public Dictionary<Faction, PlanetFaction> planetFactions;

    public float rotationSpeed;
    [SerializeField] private long startingPop;

    [SerializeField] private float timeSinceStart;
    private readonly PlanetFaction unclaimedTerritory;

    public PlanetScriptableObject planetScriptableObject { get; }
    [field: SerializeField] public long totalArea { get; protected set; }
    public long districtArea { get; protected set; }
    [field: SerializeField] public PlanetTerritory areas { get; protected set; }
    public PlanetMap planetMap;


    public Planet(PlanetData planetData, BattleManager battleManager, PlanetScriptableObject planetScriptableObject) :
        base(planetData.battleObjectData, battleManager) {
        this.planetScriptableObject = planetScriptableObject;
        rotationSpeed = planetScriptableObject.rotationSpeed;
        rotationSpeed *= random.NextFloat(.5f, 1.5f);
        if (random.NextFloat(-1, 1) < 0) {
            rotationSpeed *= -1;
        }

        planetFactions = new Dictionary<Faction, PlanetFaction>();
        visible = true;
        Spawn();
        SetSize(SetupSize());

        totalArea = (long)(math.pow(GetSize(), 2) * math.PI);
        districtArea = totalArea / PlanetMap.GetDistrictCountInRadius(planetScriptableObject.radius);
        areas = new PlanetTerritory((long)(totalArea * planetData.highQualityLandFactor),
            (long)(totalArea * planetData.mediumQualityLandFactor),
            (long)(totalArea * planetData.lowQualityLandFactor));
        unclaimedTerritory = new PlanetFaction(this, null, new Population(), "This territory is open to claim.");
        planetMap = new PlanetMap(this, random, planetScriptableObject.radius);
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (Star star in battleManager.stars) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject + star.GetSize() + GetSize()) {
                return false;
            }
        }

        foreach (AsteroidField asteroidField in battleManager.asteroidFields) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <=
                minDistanceFromObject + asteroidField.GetSize() + GetSize()) {
                return false;
            }
        }

        foreach (Station station in battleManager.stations) {
            if (Vector2.Distance(position, station.GetPosition()) <=
                minDistanceFromObject + station.GetSize() + GetSize()) {
                return false;
            }
        }

        foreach (Planet planet in battleManager.planets) {
            if (Vector2.Distance(position, planet.GetPosition()) <=
                minDistanceFromObject + planet.GetSize() + GetSize()) {
                return false;
            }
        }

        return true;
    }

    /// <summary> Adds a planet faction to the planet with the faction, territory, force given </summary>
    public PlanetFaction AddFaction(Faction faction, Population population, string special) {
        // territory.highQualityArea =
        // math.min(territory.highQualityArea, GetUnclaimedFaction().territory.highQualityArea);
        // territory.mediumQualityArea =
        // math.min(territory.mediumQualityArea, GetUnclaimedFaction().territory.mediumQualityArea);
        // territory.lowQualityArea = math.min(territory.lowQualityArea, GetUnclaimedFaction().territory.lowQualityArea);
        // GetUnclaimedFaction().territory.SubtractFrom(territory);
        var planetFaction = new PlanetFaction(this, faction, population, special);
        planetFactions.Add(faction, planetFaction);
        faction.AddPlanet(this);
        return planetFaction;
    }

    public PlanetFaction AddFaction(Faction faction, long population, string special) {
        return AddFaction(faction, new Population().SetPlanetPopulation(population), special);
    }

    /// <summary>
    /// Divides the panet's remaining territories to the factions based on the input.
    /// Applies some randomness to the input based on the randomFactor.
    /// Any extra territory will be left as unclaimed.
    /// </summary>
    public void GenerateFactionTerritories(List<(PlanetFaction, float)> factionTerritories, float randomFactor,
        bool takeoverTerritories) {
        float initialSum = factionTerritories.Select(ft => ft.Item2).Sum();
        // Apply randomness on the territories based on randomFactor
        factionTerritories = factionTerritories.Select(ft =>
            (ft.Item1, ft.Item2 * random.NextFloat(1 - randomFactor, 1 + randomFactor))).ToList();
        // Now we need to normalize the percentage of territories of each planet faction
        float newSum = factionTerritories.Sum(ft => ft.Item2);
        factionTerritories = factionTerritories.Select(ft =>
            (ft.Item1, ft.Item2 / (1 + initialSum - newSum))).ToList();

        List<District> toTake = planetMap.districts.Where(d => d.owner == null || takeoverTerritories)
            .OrderByDescending(d => d.GetDistrictValue()).ToList();
        int totalDistrictValue = toTake.Sum(d => d.GetDistrictValue());

        float GetValueOfDistrict(District district, PlanetFaction planetFaction) {
            return district.GetDistrictValue() +
                planetMap.GetNeighboringDistricts(district).Count(d => d.owner == planetFaction) * 1.2f +
                -planetMap.GetNeighboringDistricts(district).Count(d => d.owner != planetFaction) * .3f;
        }

        while (toTake.Count > 0 && factionTerritories.Count > 0) {
            factionTerritories = factionTerritories.OrderByDescending(ft => ft.Item2).ToList();
            PlanetFaction planetFaction = factionTerritories.First().Item1;
            List<District> possibleDistricts = toTake.Where(d =>
                factionTerritories.First().Item2 - d.GetDistrictValue() / (float)totalDistrictValue > 0).ToList();
            if (possibleDistricts.Count == 0) {
                factionTerritories.RemoveAt(0);
                continue;
            }
            District district = possibleDistricts.Aggregate((max, current) =>
                GetValueOfDistrict(current, planetFaction) > GetValueOfDistrict(max, planetFaction) ? current : max
            );
            district.owner = planetFaction;
            district.SetRandomDistrictType(false);
            if (district.districtType != District.DistrictType.Empty ||
                district.districtType != District.DistrictType.Wildlife) {
                district.urbanPercent = .2f;
                district.agriculturePercent = .5f;
                district.industryPercent = .15f;
            }
            district.AddFaction(planetFaction, .7f, 1);
            float newControl =
                factionTerritories.First().Item2 - district.GetDistrictValue() / (float)totalDistrictValue;
            factionTerritories.Add((planetFaction, newControl));
            factionTerritories.RemoveAt(0);
            toTake.Remove(district);
        }
    }


    //
    // public void AddFaction(Faction faction, double highQualityAreaFactor, double mediumQualityAreaFactor,
    //     double lowQualityAreaFactor, Population population, string special) {
    //     PlanetTerritory territory = new PlanetTerritory(
    //         (long)(GetUnclaimedFaction().territory.highQualityArea * highQualityAreaFactor),
    //         (long)(GetUnclaimedFaction().territory.mediumQualityArea * mediumQualityAreaFactor),
    //         (long)(GetUnclaimedFaction().territory.lowQualityArea * lowQualityAreaFactor));
    //     AddFaction(faction, territory, population, special);
    // }
    //
    // public void AddFaction(Faction faction, double highQualityAreaFactor, double mediumQualityAreaFactor,
    //     double lowQualityAreaFactor, long population, double forceFraction, string special) {
    //     PlanetTerritory territory = new PlanetTerritory(
    //         (long)(GetUnclaimedFaction().territory.highQualityArea * highQualityAreaFactor),
    //         (long)(GetUnclaimedFaction().territory.mediumQualityArea * mediumQualityAreaFactor),
    //         (long)(GetUnclaimedFaction().territory.lowQualityArea * lowQualityAreaFactor));
    //     long force = (long)(population * forceFraction);
    //     population -= force;
    //     AddFaction(faction, territory,
    //         new Population((long)(population * (PopulationCenter.civilianRatio + PopulationCenter.marineRatio)),
    //             (long)(population * PopulationCenter.pilotRatio), (long)(population * PopulationCenter.engineerRatio),
    //             force), special);
    // }

    // public void AddFaction(Faction faction, double territoryFactor, long population, double forceFraction,
    //     string special) {
    //     AddFaction(faction, territoryFactor, territoryFactor, territoryFactor, population, forceFraction, special);
    // }

    public void AddColony(Faction faction, Population population, string special) {
        var district = planetMap.districts.Where(d => d.owner == null).Aggregate((mostValuable, next) => {
            return next.GetDistrictValue() > mostValuable.GetDistrictValue() ? next : mostValuable;
        });
        var newPlanetFaction = AddFaction(faction, population, special);
        district.owner = newPlanetFaction;
        district.districtFactions.Add(newPlanetFaction,
            new District.DistrictFaction(newPlanetFaction, population, .03f));
    }

    /// <param name="planetFaction">The resulting bigger planet faction</param>
    /// <param name="toMerge">The planet faction to merge into the other planet faction</param>
    public void MergePlanetFactions(PlanetFaction planetFaction, PlanetFaction toMerge) {
        foreach (District district in planetMap.districts) {
            if (!district.districtFactions.ContainsKey(toMerge)) continue;
            if (!district.districtFactions.ContainsKey(planetFaction)) {
                district.districtFactions.Add(planetFaction, district.districtFactions[toMerge]);
                district.districtFactions[planetFaction].planetFaction = planetFaction;
            } else {
                district.districtFactions[planetFaction].pop.AddPopulation(district.districtFactions[toMerge].pop);
                district.districtFactions[planetFaction].control += district.districtFactions[toMerge].control;
            }
            district.districtFactions.Remove(toMerge);
            if (district.owner == toMerge) district.owner = planetFaction;
        }
        planetFactions.Remove(toMerge.faction);
    }

    /// <param name="planetFaction">The resulting bigger planet faction</param>
    /// <param name="toMerge">The planet faction to merge into the other planet faction</param>
    public void MergePlanetFactions(Faction planetFaction, Faction toMerge) {
        MergePlanetFactions(planetFactions[planetFaction], planetFactions[toMerge]);
    }
    public void RemoveFaction(Faction faction) {
        planetFactions.Remove(faction);
        faction.RemovePlanet(this);
    }

    public PlanetFaction GetUnclaimedFaction() {
        return unclaimedTerritory;
    }

    public void UpdatePlanet(float deltaTime) {
        timeSinceStart += deltaTime;
        SetRotation(rotation + rotationSpeed * deltaTime);
        foreach (KeyValuePair<Faction, PlanetFaction> faction in planetFactions) {
            faction.Value.UpdateFaction(deltaTime);
        }
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = battleManager.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    public long GetPopulation() {
        return planetFactions.Sum(f => f.Value.population.TotalPopulation());
    }

    public override float GetSpriteSize() {
        return Calculator.GetSpriteSizeFromBounds(planetScriptableObject.spriteBounds, scale);
    }

    public override GameObject GetPrefab() {
        return planetScriptableObject.prefab;
    }

    public class PlanetTerritory {
        public long highQualityArea;
        public long lowQualityArea;
        public long mediumQualityArea;

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

        public void AddRandomTerritory(long value, ref Random random) {
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
}
