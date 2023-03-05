using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipGroup : ObjectGroup<Ship> {
    public List<Fleet> sentFleets;

    public void SetupTargetGroup(List<Ship> ships) {
        SetupObjectGroup(ships);
        UpdateObjectGroup();
    }

    public void AddShip(Ship ship) {
        AddBattleObject(ship);
        UpdateObjectGroup();
    }

    public int GetTotalShipsHealth() {
        int totalHealth = 0;
        for (int i = 0; i < GetBattleObjects().Count; i++) {
            totalHealth += GetBattleObjects()[i].GetTotalHealth();
        }
        return totalHealth;
    }

    public bool IsSentFleetsStronger() {
        int totalHealth = 0;
        for (int i = 0; i < sentFleets.Count; i++) {
            sentFleets[i].GetTotalFleetHealth();
        }
        return totalHealth >= GetTotalShipsHealth();
    }

    public List<Ship> GetTargetShips() {
        return GetBattleObjects();
    }
}
