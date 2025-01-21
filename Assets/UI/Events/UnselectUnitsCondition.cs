using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class UnSelectUnitsCondition : UIEventCondition {
    private List<Unit> unitsToUnselect;

    public UnSelectUnitsCondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, List<Unit> units, bool visualize = false) :
        base(localPlayer, uiBattleManager, ConditionType.UnSelectUnits, visualize) {
        unitsToUnselect = units;
    }


    public override bool CheckUICondition(EventManager eventManager) {
        SelectionGroup selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits();
        return !unitsToUnselect.Select(u => uiBattleManager.units[u])
            .Any(unitUI => selectedUnits.ContainsObject(unitUI));
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();
        objectsToVisualize.AddRange(unitsToUnselect.Select(u => uiBattleManager.units[u])
            .Where(u => selectedUnits.Contains(u)).Cast<ObjectUI>().ToList());
    }
}
