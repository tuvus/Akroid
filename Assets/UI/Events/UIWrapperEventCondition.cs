public abstract class UIWrapperEventCondition<T> : UIEventCondition where T : EventCondition {
    protected T conditionLogic;

    public UIWrapperEventCondition(T conditionLogic, LocalPlayer localPlayer, UIBattleManager uiBattleManager, bool visualize = false) :
        base(localPlayer, uiBattleManager, conditionLogic.conditionType, visualize) {
        this.conditionLogic = conditionLogic;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return conditionLogic.CheckCondition(eventManager, deltaTime);
    }

    public override bool CheckUICondition(EventManager eventManager) {
        // Regular EventConditions don't check their condition during the UI update
        return false;
    }
}
