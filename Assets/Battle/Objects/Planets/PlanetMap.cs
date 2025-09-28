using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class PlanetMap {
    public List<District> districts;
    public Dictionary<Vector2Int, District> locToDistrict;
    public int radius;

    private static List<Vector2Int> cubeDir = new List<Vector2Int>() {
        new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1),
        new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1)
    };

    public PlanetMap(int radius) {
        this.radius = radius;
        districts = new List<District>();
        locToDistrict = new Dictionary<Vector2Int, District>();
        CreateGridOfSize(radius);
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

    public static Vector2 GetPositionOfDistrict(District district) {
        return new Vector2(math.sqrt(3) * district.location.x + (math.sqrt(3) / 2) * district.location.y,
            (3f / 2) * -district.location.y);
    }
}
