using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static District.DistrictType;
using static District.TerrainType;
using Random = UnityEngine.Random;


public class District {
    public class DistrictFaction {
        public District district;
        public PlanetFaction planetFaction;
        public Population pop;
        public float control;
        private double populationGainFraction;
        private double forceGainFraction;

        public DistrictFaction(District district, PlanetFaction planetFaction, Population pop, float control) {
            this.district = district;
            this.planetFaction = planetFaction;
            this.pop = pop;
            this.control = control;
        }

        public void Update(float deltaTime) {
            // Increase Population
            double popCapRatio = (double)pop.TotalPopulation() / district.GetPopulationCapacity(planetFaction);
            double populationGained =
                pop.TotalPopulationWithoutMarines() * terrainModifiers[district.terrainType].popGrowth *
                (1 - math.sqrt(popCapRatio)) * deltaTime * .0001 + populationGainFraction;
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

            // Expand control
            var uncontrolledArea = 1 - district.districtFactions.Select(d => d.Value.control).Sum();
            if (uncontrolledArea > 0) {
                var areaToControl = math.min(uncontrolledArea, pop.civilians * deltaTime / district.area);
                control += areaToControl;
            }
        }
    }

    public class DistrictModifier {
        public float population;
        public float popGrowth;
        public float lightIndustry;
        public float heavyIndustry;
        public float research;
        public float agriculture;
        public float excavation;

        public DistrictModifier(float population = 1, float popGrowth = 1, float lightIndustry = 1,
            float heavyIndustry = 1, float research = 1, float agriculture = 1, float excavation = 1) {
            this.population = population;
            this.popGrowth = popGrowth;
            this.lightIndustry = lightIndustry;
            this.heavyIndustry = heavyIndustry;
            this.research = research;
            this.agriculture = agriculture;
            this.excavation = excavation;
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
            Lakes, new List<DistrictType> { Empty, Wildlife, Agricultural, Research, Colony, LightIndustry }
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

    /// <summary>
    /// Modifications to the districts capacity and growth.
    /// </summary>
    public static readonly Dictionary<TerrainType, DistrictModifier> terrainModifiers = new() {
        { Ocean, new DistrictModifier() },
        { Lakes, new DistrictModifier(1f, 1.2f, 1.2f, .8f, 1.1f, 1f, .7f) },
        { Plains, new DistrictModifier(.8f, .8f, 1f, .9f, .8f, 1.2f, .9f) },
        { Forest, new DistrictModifier(1f, 1.1f, 1.1f, .1f, 1f, .8f, 1f) },
        { Desert, new DistrictModifier(0.7f, 0.6f, 1f, 1.1f, .8f, .5f, 1.2f) },
        { Mountains, new DistrictModifier(0.05f, .8f, .2f, 0.4f, 1.3f, .2f, 1.5f) },
        { Hills, new DistrictModifier(.8f, 1.1f, 1.3f, 1f, 1f, 1f, 1.2f) },
        { Crater, new DistrictModifier(0.03f, .6f, 1f, 1f, 1.2f, .7f, 1.2f) },
        { Barren, new DistrictModifier(0.002f, .1f, 1f, 1f, .6f, .2f, .4f) },
        { Tundra, new DistrictModifier(0.03f, .5f, 1f, .8f, 1, .1f, .3f) },
        { Arctic, new DistrictModifier(0.0001f, .01f, 1f, .7f, 2f, 0.0001f, .05f) },
        { Islands, new DistrictModifier(1.2f, 1.5f, 1.3f, .4f, 1.3f, 1.4f) },
        { Gas, new DistrictModifier(0, 0, 0, 0, 10, 0, 2) },
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
        control = (1 - districtFactions.Values.Select(f => f.control).Sum()) * control;
        districtFactions.Add(planetFaction, new DistrictFaction(this, planetFaction,
            new Population().SetPlanetPopulation((long)(GetPopulationCapacity() * control * populationPercent)),
            control));
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
        return (long)(area * landPercent * terrainModifiers[terrainType].population *
            (40000 * urbanPercent * 50 + agriculturePercent * .01f));
    }

    public long GetPopulationCapacity(PlanetFaction planetFaction) {
        float control = 0;
        if (districtFactions.ContainsKey(planetFaction))
            control = districtFactions[planetFaction].control;
        return (long)(GetPopulationCapacity() * control);
    }

    public long GetTotalPopulation() {
        return districtFactions.Values.Select(f => f.pop.TotalPopulation()).Sum();
    }

    public void UpdateDistrict(float deltaTime) {
        districtFactions.Values.ToList().ForEach(d => d.Update(deltaTime));
    }
}
