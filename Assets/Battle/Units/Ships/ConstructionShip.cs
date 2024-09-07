using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionShip : Ship {
    public Station targetStationBlueprint;

    public ConstructionShip(BattleObjectData battleObjectData, BattleManager battleManager, ShipScriptableObject shipScriptableObject): 
        base(battleObjectData, battleManager, shipScriptableObject) {
        // this.targetStationBlueprint = targetStationBlueprint;
    }

    public Station CreateStation(Vector2 position) {
        if (targetStationBlueprint != null) {
            targetStationBlueprint.Explode();
        }
        //TODO: Add stationscriptableobject here!
        targetStationBlueprint = (MiningStation)BattleManager.Instance.CreateNewStation(new BattleObjectData("MiningStation", new BattleManager.PositionGiver(position), Random.Range(0, 360), faction),  BattleManager.Instance.GetStationBlueprint(Station.StationType.MiningStation).stationScriptableObject, true);
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
        foreach (var targetStation in faction.stations) {
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
