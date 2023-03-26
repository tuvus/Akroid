using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitGroup<T> : ObjectGroup<T>, IUnitGroup where T : Unit {
    private int totalGroupHealth;
    private bool hasChanged;

    public override void SetupObjectGroup(List<T> objects, bool setupGroupPositionAndSize = true, bool changeSizeIndicatorPosition = false) {
        base.SetupObjectGroup(objects, setupGroupPositionAndSize, changeSizeIndicatorPosition);
        totalGroupHealth = CalculateTotalGroupHealth();
        hasChanged = false;
    }

    public int GetTotalGroupHealth() {
        return totalGroupHealth;
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

    public override void AddBattleObject(BattleObject battleObject) {
        ((T)battleObject).SetGroup(this);
        base.AddBattleObject(battleObject);
    }

    public override void RemoveBattleObject(T battleObject) {
        battleObject.SetGroup(null);
        base.RemoveBattleObject(battleObject);
    }

    public void AddUnit(Unit unit) {
        AddBattleObject(unit);
    }

    public void RemoveUnit(Unit unit) {
        RemoveBattleObject(unit);
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

    public List<T> GetUnits() {
        return GetBattleObjects();
    }
}