public class OpenObjectPanelCondition : UIEventCondition {
    private BattleObject objectToSelect;

    public OpenObjectPanelCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, ConditionType conditionType,
        BattleObject objectToSelect,
        bool visualize = false) : base(localPlayer, unitSpriteManager, conditionType, visualize) {
        this.objectToSelect = objectToSelect;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        if (objectToSelect == null) return localPlayer.GetLocalPlayerGameInput().rightClickedBattleObject == null;
        return localPlayer.GetLocalPlayerGameInput().rightClickedBattleObject == unitSpriteManager.objects[objectToSelect];
    }
}
