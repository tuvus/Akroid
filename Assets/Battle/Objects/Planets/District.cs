using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static District.DistrictType;
using static District.TerrainType;
using Random = UnityEngine.Random;


public class District {
    public class DistrictFaction {
        public PlanetFaction planetFaction;
        public Population pop;
        public float control;

        public DistrictFaction(PlanetFaction planetFaction, Population pop, float control) {
            this.planetFaction = planetFaction;
            this.pop = pop;
            this.control = control;
        }
    }

    public enum TerrainType {
        Ocean,
        Lakes,
        Plains,
        Forest,
        Desert,
        Mountains,
        Hills,
        Crater,
        Barren,
        Tundra,
        Arctic,
        Islands,
        Gas,
    }

    public enum DistrictType {
        Empty,
        Agricultural,
        Urban,
        Wildlife,
        LightIndustry,
        HeavyIndustry,
        Colony,
        Excavation,
        Research,
    }

    public static readonly Dictionary<TerrainType, List<DistrictType>> districtsAvailableOnTerrain = new() {
        {
            Ocean, new List<DistrictType> { Empty, Wildlife }
        }, {
            Lakes, new List<DistrictType> { Empty, Wildlife }
        }, {
            Plains, new List<DistrictType> {
                Empty, Agricultural, LightIndustry, Urban, Research, Wildlife, HeavyIndustry, Colony
            }
        }, {
            Forest, new List<DistrictType> {
                Empty, LightIndustry, Urban, Research, Wildlife, HeavyIndustry, Excavation, Colony
            }
        }, {
            Desert, new List<DistrictType> {
                Empty, LightIndustry, Urban, Research, HeavyIndustry, Excavation, Colony
            }
        }, {
            Mountains, new List<DistrictType> {
                Empty, Wildlife, Research, Excavation, Colony
            }
        }, {
            Hills, new List<DistrictType> {
                Empty, Urban, LightIndustry, HeavyIndustry, Wildlife, Research, Excavation, Colony
            }
        }, {
            Crater, new List<DistrictType> {
                Empty, Urban, LightIndustry, HeavyIndustry, Research, Excavation, Colony
            }
        }, {
            Barren, new List<DistrictType> {
                Empty, Research, Colony
            }
        }, {
            Tundra, new List<DistrictType> {
                Empty, Urban, LightIndustry, Research, Colony, Excavation
            }
        }, {
            Arctic, new List<DistrictType> {
                Empty, Research, Colony, Excavation
            }
        }, {
            Islands, new List<DistrictType> {
                Empty, Research, Colony, LightIndustry, Urban, Wildlife
            }
        }, {
            Gas, new List<DistrictType> {
                Empty
            }
        }
    };

    // The location of the district in axial hex coordinates
    public Vector2Int location;
    public long area;
    public TerrainType terrainType;
    public DistrictType districtType;
    public PlanetFaction owner;
    public Dictionary<PlanetFaction, DistrictFaction> districtFactions;
    public float industryPercent;
    public float urbanPercent;
    public float agriculturePercent;
    public float landPercent;

    public District(Vector2Int loc, long area) {
        this.location = loc;
        this.area = area;
        districtFactions = new Dictionary<PlanetFaction, DistrictFaction>();
        districtType = Empty;
        landPercent = 1;
    }

    public void SetTerrainType(TerrainType terrainType) {
        this.terrainType = terrainType;
        switch (terrainType) {
            case Ocean:
            case Gas:
                landPercent = 0;
                break;
            case Lakes:
                landPercent = .7f;
                break;
            case Plains:
            case Forest:
            case Hills:
                landPercent = .95f;
                break;
            case Desert:
            case Mountains:
            case Crater:
            case Barren:
            case Tundra:
            case Arctic:
                landPercent = 1;
                break;
            case Islands:
                landPercent = .1f;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(terrainType), terrainType, null);
        }
    }

    public void SetRandomDistrictType(bool includeWildlife) {
        var types = districtsAvailableOnTerrain[terrainType].Where(d => d != Empty &&
            d != Colony && (includeWildlife || d != Wildlife)).ToList();
        if (!types.Any()) return;
        districtType = types[Random.Range(0, types.Count)];
    }

    public void AddFaction(PlanetFaction planetFaction, float populationPercent, float control) {
        districtFactions.Add(planetFaction,
            new DistrictFaction(planetFaction,
                new Population().SetPlanetPopulation((long)((GetPopulationCapacity() - GetTotalPopulation()) *
                    populationPercent)), (1 - districtFactions.Values.Select(f => f.control).Sum()) * control));
    }

    public int GetDistrictValue() {
        switch (terrainType) {
            case Ocean:
            case Arctic:
            case Barren:
            case Gas:
                return 1;
            case Desert:
            case Mountains:
            case Crater:
            case Tundra:
            case Islands:
                return 2;
            case Forest:
            case Plains:
            case Lakes:
            case Hills:
                return 3;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public long GetPopulationCapacity() {
        return (long)(area * landPercent * urbanPercent * 50 + area * landPercent * agriculturePercent * .01f);
    }

    public long GetTotalPopulation() {
        return districtFactions.Values.Select(f => f.pop.TotalPopulation()).Sum();
    }
}
