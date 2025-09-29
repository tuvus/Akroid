using UnityEngine;

public class District {
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

    // The location of the district in axial hex coordinates
    public Vector2Int location;
    public TerrainType terrainType;

    public District(Vector2Int loc) {
        this.location = loc;
    }
}
