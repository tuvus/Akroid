using System.Linq;
using UnityEngine;

public class Shipyard : Station {
    public Shipyard(BattleObjectData battleObjectData, BattleManager battleManager,
        StationScriptableObject stationScriptableObject, bool built) :
        base(battleObjectData, battleManager, stationScriptableObject, built) { }

    public override void UpdateUnit(float deltaTime) {
        base.UpdateUnit(deltaTime);
        if (built) moduleSystem.Get<ConstructionBay>().ForEach(c => c.UpdateConstructionBay(deltaTime));
    }

    public ConstructionBay GetConstructionBay() {
        return moduleSystem.Get<ConstructionBay>().FirstOrDefault();
    }
}
