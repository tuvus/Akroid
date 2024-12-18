public abstract class EventCondition {
    public enum ConditionType {
        Wait,
        WaitUntilShipsIdle,
        WaitTrigger,
        SelectUnit,
        SelectUnits,
        SelectUnitsAmount,
        UnSelectUnits,
        SelectFleet,
        OpenObjectPanel,
        OpenFactionPanel,
        FollowUnit,
        MoveShipsToObject,
        CommandShip,
        CommandMoveShipToObjectSequence,
        CommandDockShipToUnit,
        CommandShipToCollectGas,
        BuildShipsAtStation,
        ShipsDockedAtUnit,
        Pan,
        Zoom,
        Predicate,
        LateCondition,
        Placeholder,
    }

    public ConditionType conditionType { get; protected set; }
    public bool visualize;

    /// <summary> No extenal instantiation allowed </summary>
    public EventCondition(ConditionType conditionType, bool visualize = false) {
        this.conditionType = conditionType;
        this.visualize = visualize;
    }

    /// <summary>
    /// Checks the condition during the simulation frame.
    /// </summary>
    /// <returns>True if the condition is fullfilled and the event should be removed, false otherwise.</returns>
    public abstract bool CheckCondition(EventManager eventManager, float deltaTime);
}
