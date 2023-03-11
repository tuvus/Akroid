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

    protected override void AddBattleObject(T battleObject) {
        battleObject.SetGroup(this);
        base.AddBattleObject(battleObject);
    }

    protected override void RemoveBattleObject(T battleObject) {
        battleObject.SetGroup(null);
        base.RemoveBattleObject(battleObject);
    }


    public List<T> GetUnits() {
        return GetBattleObjects();
    }
}