using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

// x = r, y = q, z = s
public class PlanetMap {
    public List<Vector2Int> districts;

    private static List<Vector2Int> cubeDir = new List<Vector2Int>() {
        new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1),
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1)
    };

    public PlanetMap(int radius) {
        districts = new List<Vector2Int>();
        CreateGridOfSize(radius);
    }
    void CreateGridOfSize(int radius) {
        Vector2Int loc = Vector2Int.zero;
        districts.Add(loc);
        for (int r = 1; r < radius; r++) {
            loc = cubeDir[4] * r;
            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < r; j++) {
                    districts.Add(loc);
                    loc += cubeDir[i];
                }
            }
        }
    }

    public static Vector2 GetPositionOfDistrict(Vector2Int district) {
        return new Vector2(math.sqrt(3) * district.x + (math.sqrt(3) / 2) * district.y,
            (3f / 2) * -district.y);
    }
}
