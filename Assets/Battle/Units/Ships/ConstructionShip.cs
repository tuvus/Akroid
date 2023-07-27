using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionShip : Ship {
    public Station targetStationBlueprint;

    public Station CreateStation(Vector2 position) {
        if (targetStationBlueprint != null) {
            targetStationBlueprint.Explode();
        }
        //TODO: Add stationscriptableobject here!
        targetStationBlueprint = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(faction.factionIndex, null, "MiningStation", position, Random.Range(0, 360), false));
        return targetStationBlueprint;
    }

    public override void UpdateUnit(float deltaTime) {
        base.UpdateUnit(deltaTime);
        if (targetStationBlueprint != null && Vector2.Distance(GetPosition(), targetStationBlueprint.GetPosition()) < targetStationBlueprint.GetSize() + GetSize() + 100 && targetStationBlueprint.BuildStation()) {
            targetStationBlueprint = null;
            Explode();
        }
    }

    public Station GetClosestUnbuiltStationInRange(float range) {
        Station station = null;
        float distance = 0;
        for (int i = 0; i < faction.stations.Count; i++) {
            Station targetStation = faction.stations[i];
            if (targetStation == null || targetStation.IsSpawned() || targetStation.IsBuilt())
                continue;
            float targetDistance = Vector2.Distance(GetPosition(), targetStation.GetPosition());
            if (targetDistance <= range + targetStation.GetSize() && (station == null || (targetDistance < distance))) {
                station = targetStation;
                distance = targetDistance;
            }
        }
        return station;
    }

    public override void Explode() {
        if (targetStationBlueprint != null) {
            targetStationBlueprint.Explode();
        }
        base.Explode();
    }
}
