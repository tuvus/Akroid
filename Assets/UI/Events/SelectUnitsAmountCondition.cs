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

    public override List<ObjectUI> GetVisualizedObjects() {
        if (conditionType == ConditionType.SelectUnit) {
            // If the unit is docked at a station, we need to show the station instead
            if (unitsToSelect.First().IsShip() && ((Ship)unitsToSelect.First()).dockedStation != null)
                return new List<ObjectUI>() { unitSpriteManager.objects[((Ship)unitsToSelect.First()).dockedStation] };

            return new List<ObjectUI>() { unitSpriteManager.objects[unitsToSelect.First()] };
        }

        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();
        bool isFleet = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().fleet != null;
        return unitsToSelect.Select(u => unitSpriteManager.units[u]).Where(u => !selectedUnits.Contains(u) || isFleet).Cast<ObjectUI>().ToList();
    }
}
