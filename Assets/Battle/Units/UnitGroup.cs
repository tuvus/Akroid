using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitGroup : ObjectGroup<Unit>{

    public UnitGroup(BattleManager battleManager, HashSet<Unit> objects, bool deleteGroupWhenEmpty, bool setupGroupPositionAndSize = true, bool changeSizeIndicatorPosition = false):
        base(battleManager, objects, deleteGroupWhenEmpty, setupGroupPositionAndSize, changeSizeIndicatorPosition){
    }

    public int GetTotalGroupHealth() {
        return CalculateTotalGroupHealth();
    }

    private int CalculateTotalGroupHealth() {
        return battleObjects.Sum(o => o.GetTotalHealth());
    }

    public void AddUnit(Unit unit) {
        unit.SetGroup(this);
    }

    public void RemoveUnit(Unit unit) {
        unit.SetGroup(null);
    }

    public override void RemoveBattleObject(BattleObject battleObject) {
        base.RemoveBattleObject(battleObject);
    }

    public virtual bool IsFleet() {
        return false;
    }

    public bool IsTargetable() {
        return battleObjects.Any(o => o.IsTargetable());
    }
}
