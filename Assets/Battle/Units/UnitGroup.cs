using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitGroup : ObjectGroup<Unit>{
    private int totalGroupHealth;
    private bool hasChanged;
    public override void SetupObjectGroup(BattleManager battleManager, List<Unit> objects, bool deleteGroupWhenEmpty, bool setupGroupPositionAndSize = true, bool changeSizeIndicatorPosition = false) {
        base.SetupObjectGroup(battleManager, objects, deleteGroupWhenEmpty, setupGroupPositionAndSize, changeSizeIndicatorPosition);
        totalGroupHealth = CalculateTotalGroupHealth();
        hasChanged = false;
    }

    public int GetTotalGroupHealth() {
        return CalculateTotalGroupHealth();
    }

    private int CalculateTotalGroupHealth() {
        int newTotalGroupHealth = 0;
        for (int i = 0; i < GetBattleObjects().Count; i++) {
            newTotalGroupHealth += GetBattleObjects()[i].GetTotalHealth();
        }
        return newTotalGroupHealth;
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
        if (GetBattleObjects().Count == 0)
            return false;
        for (int i = 0; i < GetBattleObjects().Count; i++) {
            if (GetBattleObjects()[i].IsTargetable())
                return true;
        }
        return false;
    }

    public List<Unit> GetUnits() {
        return GetBattleObjects();
    }
}