using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shipyard : Station {
    private ConstructionBay constructionBay;

    public override void SetupUnit(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, bool built, float timeScale, UnitScriptableObject unitScriptableObject) {
        base.SetupUnit(name, faction, positionGiver, rotation, built, timeScale, unitScriptableObject);
        constructionBay = GetComponentInChildren<ConstructionBay>();
        constructionBay.SetupConstructionBay(this);
    }

    public override void UpdateUnit(float deltaTime) {
        base.UpdateUnit(deltaTime);
        if (constructionBay != null && built)
            constructionBay.UpdateConstructionBay(deltaTime);
    }

    public ConstructionBay GetConstructionBay() {
        return constructionBay;
    }
}
