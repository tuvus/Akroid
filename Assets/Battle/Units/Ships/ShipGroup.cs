using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipGroup : UnitGroup<Ship> {
    public List<Fleet> sentFleets;


    public void SetupTargetGroup(List<Ship> ships) {
        SetupObjectGroup(ships);
        UpdateObjectGroup();
    }

    public void AddShip(Ship ship) {
        AddBattleObject(ship);
        UpdateObjectGroup();
    }

    public bool IsSentFleetsStronger() {
        int totalHealth = 0;
        for (int i = 0; i < sentFleets.Count; i++) {
            sentFleets[i].GetTotalFleetHealth();
        }
        return totalHealth >= GetTotalGroupHealth();
    }

    public List<Ship> GetShips() {
        return GetBattleObjects();
    }
}
