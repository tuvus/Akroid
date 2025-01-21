using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine.UI;

public class SelectUnitsAmountCondition : UIEventCondition {
    private List<Unit> unitsToSelect;
    private int amountToSelect;

    public SelectUnitsAmountCondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, ConditionType conditionType, Unit unit,
        bool visualize = false) : base(localPlayer, uiBattleManager, conditionType, visualize) {
        unitsToSelect = new List<Unit>() { unit };
        amountToSelect = 1;
    }

    public SelectUnitsAmountCondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, ConditionType conditionType,
        List<Unit> units, bool visualize = false) : base(localPlayer, uiBattleManager, conditionType, visualize) {
        unitsToSelect = units;
        amountToSelect = units.Count;
    }

    public SelectUnitsAmountCondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, ConditionType conditionType,
        List<Unit> units, int count, bool visualize = false) : base(localPlayer, uiBattleManager, conditionType, visualize) {
        unitsToSelect = units;
        amountToSelect = math.max(0, math.min(units.Count, count));
    }


    public override bool CheckUICondition(EventManager eventManager) {
        SelectionGroup selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits();
        return unitsToSelect.Select(u => uiBattleManager.units[u])
            .Count(unitUI => selectedUnits.ContainsObject(unitUI)) >= amountToSelect;
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        AddShipsToSelect(unitsToSelect.Where(u => u.IsShip()).Select(s => (ShipUI)uiBattleManager.units[s]).ToList(), objectsToVisualize,
            buttonsToVisualize);

        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();
        bool isFleet = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().fleet != null;
        objectsToVisualize.AddRange(unitsToSelect.Where(u => !u.IsShip()).Select(u => uiBattleManager.units[u])
            .Where(u => !selectedUnits.Contains(u) || isFleet).Cast<ObjectUI>().ToList());
    }
}
