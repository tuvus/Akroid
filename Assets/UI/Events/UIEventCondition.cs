using System.Collections.Generic;

public abstract class UIEventCondition : EventCondition {
    protected LocalPlayer localPlayer;
    protected UnitSpriteManager unitSpriteManager;

    public UIEventCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, ConditionType conditionType,
        bool visualize = false) : base(conditionType, visualize) {
        this.localPlayer = localPlayer;
        this.unitSpriteManager = unitSpriteManager;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        // Most UIEventConditions will check their condition during the UI update and not the battle update
        return false;
    }

    /// <summary>
    /// Checks the UICondition during the UI frame.
    /// </summary>
    /// <returns>True if the condition is fullfilled and the event should be removed, false otherwise.</returns>
    public abstract bool CheckUICondition(EventManager eventManager);

    /// <summary>
    /// Decideds wich objects should be visualised by this event
    /// </summary>
    public abstract List<ObjectUI> GetVisualizedObjects();
}
