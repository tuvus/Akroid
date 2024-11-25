using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public abstract bool CheckCondition(EventManager eventManager, float deltaTime);
}
