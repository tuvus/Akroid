public abstract class UIEvent : EventCondition {
    protected LocalPlayer localPlayer;
    protected UIBattleManager uiBattleManager;

    public UIEvent(LocalPlayer localPlayer, UIBattleManager uiBattleManager, ConditionType conditionType,
        bool visualize = false) : base(conditionType, visualize) {
        this.localPlayer = localPlayer;
        this.uiBattleManager = uiBattleManager;
    }

    /// <summary>
    ///     Updates the event during the UI update phase.
    /// </summary>
    public abstract void UpdateUI(EventManager eventManager);

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        // Most UIEvents will check their condition during the UI update and not the battle update
        return false;
    }

    /// <summary>
    ///     Checks the UICondition during the UI frame.
    /// </summary>
    /// <returns>True if the condition is fulfilled and the event should be removed, false otherwise.</returns>
    public abstract bool CheckUICondition(EventManager eventManager);
}
