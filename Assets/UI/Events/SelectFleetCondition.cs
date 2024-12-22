using System.Collections.Generic;
using System.Linq;

public class SelectFleetsCondition : UIEventCondition {
    private Fleet fleetToSelect;

    public SelectFleetsCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, Fleet fleet, bool visualize = false) :
        base(localPlayer, unitSpriteManager, ConditionType.SelectFleet, visualize) {
        fleetToSelect = fleet;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        SelectionGroup selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits();
        return selectedUnits.fleet == unitSpriteManager.fleetUIs[fleetToSelect];
    }

    public override List<ObjectUI> GetVisualizedObjects() {
        return fleetToSelect.ships.Select(s => unitSpriteManager.units[s]).Cast<ObjectUI>().ToList();
    }
}
