using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShipGroup : UnitGroup {
    protected List<Ship> ships;
    public List<Fleet> sentFleets;

    public override void SetupObjectGroup(List<Unit> objects, bool deleteWhenEmpty, bool setupGroupPositionAndSize = true, bool changeSizeIndicatorPosition = false) {
        ships = new List<Ship>();
        sentFleets = new List<Fleet>();
        base.SetupObjectGroup(objects, deleteWhenEmpty, setupGroupPositionAndSize, changeSizeIndicatorPosition);
    }

    public void SetupTargetGroup(List<Unit> ships, bool deleteWhenEmpty) {
        SetupObjectGroup(ships, deleteWhenEmpty);
        UpdateObjectGroup();
    }

    public virtual void AddShip(Ship ship) {
        base.AddUnit(ship);
        UpdateObjectGroup(); 
    }

    public virtual void RemoveShip(Ship ship) {
        base.RemoveUnit(ship);
    } 

    public override void AddBattleObject(BattleObject battleObject) {
        base.AddBattleObject(battleObject);
        ships.Add((Ship)battleObject);
    }

    public override void RemoveBattleObject(BattleObject battleObject) {
        base.RemoveBattleObject(battleObject);
        ships.Remove((Ship)battleObject);
    }

    public bool IsSentFleetsStronger() {
        int totalHealth = 0;
        for (int i = 0; i < sentFleets.Count; i++) {
            sentFleets[i].GetTotalFleetHealth();
        }
        return totalHealth >= GetTotalGroupHealth();
    }

    public List<Ship> GetShips() {
        return ships;
    }
}
