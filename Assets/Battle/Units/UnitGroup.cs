using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitGroup : ObjectGroup<Unit>{
    private int totalGroupHealth;
    private bool hasChanged;
    public override void SetupObjectGroup(BattleManager battleManager, HashSet<Unit> objects, bool deleteGroupWhenEmpty, bool setupGroupPositionAndSize = true, bool changeSizeIndicatorPosition = false) {
        base.SetupObjectGroup(battleManager, objects, deleteGroupWhenEmpty, setupGroupPositionAndSize, changeSizeIndicatorPosition);
        totalGroupHealth = CalculateTotalGroupHealth();
        hasChanged = false;
    }

    public int GetTotalGroupHealth() {
        return CalculateTotalGroupHealth();
    }

    private int CalculateTotalGroupHealth() {
        return battleObjects.Sum(o => o.GetTotalHealth());
    }

    public void UnitUpdated() {
        hasChanged = true;
    }

    public void ChangeGroupTotalHealth(int health) {
        totalGroupHealth += health;
    }

    public void AddUnit(Unit unit) {
        unit.SetGroup(this);
    }

    public void RemoveUnit(Unit unit) {
        unit.SetGroup(null);
    }

    public override void RemoveBattleObject(BattleObject battleObject) {
        base.RemoveBattleObject(battleObject);
        totalGroupHealth -= ((Unit)battleObject).GetTotalHealth();
    }

    public virtual bool IsFleet() {
        return false;
    }

    public bool IsTargetable() {
        return battleObjects.Any(o => o.IsTargetable());
    }
}