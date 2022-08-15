using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shipyard : Station {
    private ConstructionBay constructionBay;

    public override void SetupUnit(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, bool built) {
        base.SetupUnit(name, faction, positionGiver, rotation, built);
        constructionBay = GetComponentInChildren<ConstructionBay>();
        constructionBay.SetupConstructionBay(this);
    }

    public override void UpdateUnit() {
        base.UpdateUnit();
        if (constructionBay != null && built)
            constructionBay.UpdateConstructionBay(Time.fixedDeltaTime * BattleManager.Instance.timeScale);
    }

    public ConstructionBay GetConstructionBay() {
        return constructionBay;
    }
}
