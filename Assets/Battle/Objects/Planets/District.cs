using UnityEngine;

public class District {
    // The location of the district in axial hex coordinates
    public Vector2Int location;

    public District(Vector2Int loc) {
        this.location = loc;
    }
}
