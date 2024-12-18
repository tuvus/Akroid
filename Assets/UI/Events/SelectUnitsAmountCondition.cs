using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

public class SelectUnitsAmountCondition : UIEventCondition {
    private List<Unit> unitsToSelect;
    private int amountToSelect;

    public SelectUnitsAmountCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, ConditionType conditionType, Unit unit,
        bool visualize = false) : base(localPlayer, unitSpriteManager, conditionType, visualize) {
        unitsToSelect = new List<Unit>() { unit };
        amountToSelect = 1;
    }

    public SelectUnitsAmountCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, ConditionType conditionType,
        List<Unit> units, bool visualize = false) : base(localPlayer, unitSpriteManager, conditionType, visualize) {
        unitsToSelect = units;
        amountToSelect = units.Count;
    }


    public SelectUnitsAmountCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, ConditionType conditionType,
        List<Unit> units, int count, bool visualize = false) : base(localPlayer, unitSpriteManager, conditionType, visualize) {
        unitsToSelect = units;
        amountToSelect = math.max(0, math.min(units.Count, count));
    }


    public override bool CheckUICondition(EventManager eventManager) {
        SelectionGroup selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits();
        return unitsToSelect.Select(u => unitSpriteManager.units[u])
            .Count(unitUI => selectedUnits.ContainsObject(unitUI)) >= amountToSelect;
    }
}
