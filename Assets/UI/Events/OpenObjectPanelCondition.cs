public class OpenObjectPanelCondition : UIEventCondition {
    private BattleObject objectToSelect;

    public OpenObjectPanelCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, BattleObject objectToSelect,
        bool visualize = false) : base(localPlayer, unitSpriteManager, ConditionType.OpenObjectPanel, visualize) {
        this.objectToSelect = objectToSelect;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        if (objectToSelect == null) return localPlayer.GetLocalPlayerGameInput().rightClickedBattleObject == null;
        return localPlayer.GetLocalPlayerGameInput().rightClickedBattleObject == unitSpriteManager.objects[objectToSelect];
    }
}
