public abstract class UIEventCondition : EventCondition {
    protected LocalPlayer localPlayer;
    protected UnitSpriteManager unitSpriteManager;

    public UIEventCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, ConditionType conditionType,
        bool visualize = false) : base(conditionType, visualize) {
        this.localPlayer = localPlayer;
        this.unitSpriteManager = unitSpriteManager;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        // UIEventConditions check during the UI frame and not the simulation frame
        return false;
    }

    /// <summary>
    /// Checks the UICondition during the UI frame.
    /// </summary>
    /// <returns>True if the condition is fullfilled and the event should be removed, false otherwise.</returns>
    public abstract bool CheckUICondition(EventManager eventManager);
}
