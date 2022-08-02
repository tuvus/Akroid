using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionShip : Ship {

    public override void UpdateUnit() {
        base.UpdateUnit();
        Station targetStation = GetClosestUnbuiltStationInRange(GetSize() * 2);
        if (targetStation != null && targetStation.BuildStation()) {
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
}
