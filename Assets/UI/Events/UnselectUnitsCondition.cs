using System.Collections.Generic;
using System.Linq;

public class UnSelectUnitsCondition : UIEventCondition {
    private List<Unit> unitsToUnselect;

    public UnSelectUnitsCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, ConditionType conditionType,
        List<Unit> units, bool visualize = false) : base(localPlayer, unitSpriteManager, conditionType, visualize) {
        unitsToUnselect = units;
    }


    public override bool CheckUICondition(EventManager eventManager) {
        SelectionGroup selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits();
        return !unitsToUnselect.Select(u => unitSpriteManager.units[u])
            .Any(unitUI => selectedUnits.ContainsObject(unitUI));
    }
}
