using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

/// <summary>
/// Helpful webpage https://www.redblobgames.com/grids/hexagons/
/// </summary>
public class PlanetMap {
    private Planet planet;
    private Random random;

    public List<District> districts;
    // Stores a map of the Axial coordinates to districts
    // The map is displayed so
    //   (0,-1)(1,-1)
    // (-1,0)(0,0)(1,0)
    //   (-1,1)(0,1)
    // When traversing any neighbors we start by going to the left
    // and then proceed in a clockwise manner.
    private Dictionary<Vector2Int, District> locToDistrict;
    // Radius of 0 means 1 tile, radius of 1 means 7 tiles.
    public int radius;
    // Holds a list of the center locations of all adjacent wrap around grids
    // This is used for translating locations off of the center map back to the center map
    // The first one is to the bottom left of the center
    private List<Vector2Int> wrapMapCenters;

    private static List<Vector2Int> cubeDir = new List<Vector2Int>() {
        new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1),
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1)
    };

    public PlanetMap(Planet planet, Random random, int radius) {
        this.planet = planet;
        this.random = random;
        this.radius = radius;
        districts = new List<District>();
        locToDistrict = new Dictionary<Vector2Int, District>();
        wrapMapCenters = GetAllHexRotations(new Vector2Int(-radius, -radius + 1));
        CreateGridOfSize(radius);
        GenerateTerrain();
    }

    public static List<Vector2Int> GetGridCoordinatesOfSize(int radius) {
        Vector2Int loc = Vector2Int.zero;
        List<Vector2Int> coordinates = new List<Vector2Int>();
        coordinates.Add(loc);
        for (int r = 1; r < radius; r++) {
            loc = cubeDir[4] * r;
            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < r; j++) {
                    coordinates.Add(loc);
                    loc += cubeDir[i];
                }
            }
        }
        return coordinates;
    }

    void CreateGridOfSize(int radius) {
        GetGridCoordinatesOfSize(radius).ForEach(loc => {
            districts.Add(new District(districts.Count, loc, planet.districtArea));
            locToDistrict.Add(loc, districts.Last());
        });
    }

    public void GenerateTerrain() {
        switch (planet.planetScriptableObject.planetType) {
            case Planet.PlanetType.Terran:
                foreach (District district in districts) {
                    bool water = random.NextInt(0, 10) < 3;
                    if (water) {
                        int value = random.NextInt(0, 10);
                        if (value < 8) {
                            district.SetTerrainType(District.TerrainType.Ocean);
                        } else {
                            district.SetTerrainType(District.TerrainType.Islands);
                        }
                    } else {
                        int value = random.NextInt(0, 11);
                        if (value < 3) {
                            district.SetTerrainType(District.TerrainType.Plains);
                        } else if (value < 5) {
                            district.SetTerrainType(District.TerrainType.Forest);
                        } else if (value < 7) {
                            district.SetTerrainType(District.TerrainType.Hills);
                        } else if (value < 8) {
                            district.SetTerrainType(District.TerrainType.Mountains);
                        } else if (value < 9) {
                            district.SetTerrainType(District.TerrainType.Tundra);
                        } else if (value < 10) {
                            district.SetTerrainType(District.TerrainType.Lakes);
                        } else {
                            district.SetTerrainType(District.TerrainType.Desert);
                        }
                    }
                }
                break;
            case Planet.PlanetType.Moon:
                foreach (District district in districts) {
                    if (random.NextInt(0, 5) < 4) {
                        district.SetTerrainType(District.TerrainType.Barren);
                    } else {
                        district.SetTerrainType(District.TerrainType.Crater);
                    }
                }
                break;
            case Planet.PlanetType.GasGiant:
                foreach (District district in districts) {
                    district.SetTerrainType(District.TerrainType.Gas);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static Vector2 GetPositionFromLocation(Vector2Int location) {
        return new Vector2(math.sqrt(3) * location.x + (math.sqrt(3) / 2) * location.y,
            (3f / 2) * -location.y);
    }

    public static int GetDistrictCountInRadius(int radius) {
        return 3 * radius * (radius + 1) + 1;
    }

    public Vector2Int WrapLocation(Vector2Int loc) {
        while (GetDistanceBetweenHexes(loc, Vector2Int.zero) >= radius) {
            var closestCenter = wrapMapCenters.Select(c => (c, GetDistanceBetweenHexes(loc, c)))
                .Aggregate((a, b) => b.Item2 < a.Item2 ? b : a);
            loc -= closestCenter.c;
        }
        return loc;
    }

    public District GetDistrict(Vector2Int loc) {
        return locToDistrict[WrapLocation(loc)];
    }

    public static int GetDistanceBetweenHexes(Vector2Int loc1, Vector2Int loc2) {
        var diff = loc1 - loc2;
        return math.max(math.max(math.abs(diff.x), math.abs(diff.y)), math.abs(-diff.x - diff.y));
    }

    public static List<Vector2Int> GetAllHexRotations(Vector2Int loc) {
        List<Vector2Int> locs = new List<Vector2Int>() { loc };
        for (int i = 0; i < 5; i++) {
            loc = GetClockwiseHexRotation(loc);
            locs.Add(loc);
        }
        return locs;
    }

    public static Vector2Int GetClockwiseHexRotation(Vector2Int loc) {
        return new Vector2Int(-loc.y, loc.y + loc.x);
    }

    public static Vector2Int GetCounterClockwiseHexRotation(Vector2Int loc) {
        return new Vector2Int(loc.y + loc.x, -loc.x);
    }

    public List<District> GetNeighboringDistricts(District district) {
        List<District> neighbors = new List<District>();
        foreach (Vector2Int translation in cubeDir) {
            Vector2Int loc = district.location + translation;
            neighbors.Add(GetDistrict(loc));
        }
        return neighbors;
    }
}
