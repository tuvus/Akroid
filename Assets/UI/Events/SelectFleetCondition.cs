using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class SelectFleetsCondition : UIEventCondition {
    private Fleet fleetToSelect;

    public SelectFleetsCondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, Fleet fleet, bool visualize = false) :
        base(localPlayer, uiBattleManager, ConditionType.SelectFleet, visualize) {
        fleetToSelect = fleet;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        SelectionGroup selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits();
        return selectedUnits.fleet == uiBattleManager.fleetUIs[fleetToSelect];
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        AddShipsToSelect(fleetToSelect.ships.Select(s => (ShipUI)uiBattleManager.units[s]).ToList(), objectsToVisualize, buttonsToVisualize);
    }
}
