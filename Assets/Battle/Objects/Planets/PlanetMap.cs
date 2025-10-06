using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class PlanetMap {
    private Planet planet;
    private Random random;

    public List<District> districts;
    public Dictionary<Vector2Int, District> locToDistrict;
    public int radius;

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
        CreateGridOfSize(radius);
        GenerateTerrain();
    }


    void CreateGridOfSize(int radius) {
        Vector2Int loc = Vector2Int.zero;
        districts.Add(new District(loc));
        locToDistrict.Add(loc, districts.Last());
        for (int r = 1; r < radius; r++) {
            loc = cubeDir[4] * r;
            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < r; j++) {
                    districts.Add(new District(loc));
                    locToDistrict.Add(loc, districts.Last());
                    loc += cubeDir[i];
                }
            }
        }
    }

    public void GenerateTerrain() {
        switch (planet.planetScriptableObject.planetType) {
            case Planet.PlanetType.Terran:
                foreach (District district in districts) {
                    bool water = random.NextInt(0, 10) < 3;
                    if (water) {
                        int value = random.NextInt(0, 10);
                        if (value < 8) {
                            district.terrainType = District.TerrainType.Ocean;
                        } else {
                            district.terrainType = District.TerrainType.Islands;
                        }
                    } else {
                        int value = random.NextInt(0, 11);
                        if (value < 3) {
                            district.terrainType = District.TerrainType.Plains;
                        } else if (value < 5) {
                            district.terrainType = District.TerrainType.Forest;
                        } else if (value < 7) {
                            district.terrainType = District.TerrainType.Hills;
                        } else if (value < 8) {
                            district.terrainType = District.TerrainType.Mountains;
                        } else if (value < 9) {
                            district.terrainType = District.TerrainType.Tundra;
                        } else if (value < 10) {
                            district.terrainType = District.TerrainType.Lakes;
                        } else {
                            district.terrainType = District.TerrainType.Desert;
                        }
                    }
                }
                break;
            case Planet.PlanetType.Moon:
                foreach (District district in districts) {
                    if (random.NextInt(0, 5) < 4) {
                        district.terrainType = District.TerrainType.Barren;
                    } else {
                        district.terrainType = District.TerrainType.Crater;
                    }
                }
                break;
            case Planet.PlanetType.GasGiant:
                foreach (District district in districts) {
                    district.terrainType = District.TerrainType.Gas;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static Vector2 GetPositionOfDistrict(District district) {
        return new Vector2(math.sqrt(3) * district.location.x + (math.sqrt(3) / 2) * district.location.y,
            (3f / 2) * -district.location.y);
    }
}
