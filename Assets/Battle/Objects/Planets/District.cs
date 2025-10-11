using System;
using System.Collections.Generic;
using UnityEngine;

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

    // The location of the district in axial hex coordinates
    public Vector2Int location;
    public TerrainType terrainType;
    public PlanetFaction owner;
    public Dictionary<PlanetFaction, DistrictFaction> districtFactions;
    public long poulationCapacity;

    public District(Vector2Int loc) {
        this.location = loc;
        districtFactions = new Dictionary<PlanetFaction, DistrictFaction>();
    }

    public int GetDistrictValue() {
        switch (terrainType) {
            case TerrainType.Ocean:
            case TerrainType.Arctic:
            case TerrainType.Barren:
            case TerrainType.Gas:
                return 1;
            case TerrainType.Desert:
            case TerrainType.Mountains:
            case TerrainType.Crater:
            case TerrainType.Tundra:
            case TerrainType.Islands:
                return 2;
            case TerrainType.Forest:
            case TerrainType.Plains:
            case TerrainType.Lakes:
            case TerrainType.Hills:
                return 3;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
