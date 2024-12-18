public class SelectFleetsCondition : UIEventCondition {
    private Fleet fleetToSelect;

    public SelectFleetsCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, ConditionType conditionType, Fleet fleet,
        bool visualize = false) : base(localPlayer, unitSpriteManager, conditionType, visualize) {
        fleetToSelect = fleet;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        SelectionGroup selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits();
        return selectedUnits.fleet == unitSpriteManager.fleetUIs[fleetToSelect];
    }
}
