using System.Collections.Generic;
using System.Linq;

public class UnSelectUnitsCondition : UIEventCondition {
    private List<Unit> unitsToUnselect;

    public UnSelectUnitsCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, List<Unit> units, bool visualize = false) :
        base(localPlayer, unitSpriteManager, ConditionType.UnSelectUnits, visualize) {
        unitsToUnselect = units;
    }


    public override bool CheckUICondition(EventManager eventManager) {
        SelectionGroup selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits();
        return !unitsToUnselect.Select(u => unitSpriteManager.units[u])
            .Any(unitUI => selectedUnits.ContainsObject(unitUI));
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize) {
        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();
        objectsToVisualize.AddRange(unitsToUnselect.Select(u => unitSpriteManager.units[u])
            .Where(u => selectedUnits.Contains(u)).Cast<ObjectUI>().ToList());
    }
}
