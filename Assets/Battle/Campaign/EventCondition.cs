using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventCondition {
    public enum ConditionType {
        Wait,
        WaitUntilShipsIdle,
        SelectUnit,
        SelectUnits,
        SelectUnitsAmount,
        UnSelectUnits,
        SelectFleet,
        OpenObjectPanel,
        FollowUnit,
        MoveShipToObject,
        CommandMoveShipToObjectSequence,
        ShipsDockedAtUnit,
        Pan,
        Zoom,
        Predicate,
    }

    public ConditionType conditionType { get; protected set; }
    public BattleObject objectToSelect { get; protected set; }
    public Unit unitToSelect { get; protected set; }
    public HashSet<Unit> unitsToSelect { get; protected set; }
    public IObject iObject { get; protected set; }
    public List<IObject> iObjects { get; protected set; }
    public Fleet fleetToSelect { get; protected set; }
    public int intValue { get; protected set; }
    public float floatValue { get; protected set; }
    public float floatValue2 { get; protected set; }
    public Vector2 postionValue { get; protected set; }
    public Predicate<EventManager> predicate { get; protected set; }
    public bool visualize;

    private EventCondition() {
        // No extenal instantiation allowed
    }

    public static EventCondition WaitEvent(float timeToWait) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.Wait;
        condition.floatValue = timeToWait;
        return condition;
    }

    public static EventCondition WaitUntilShipsIdle(List<Ship> shipsToIdle, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.WaitUntilShipsIdle;
        condition.iObjects = shipsToIdle.Cast<IObject>().ToList();
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition SelectUnitEvent(Unit unitToSelect, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.SelectUnit;
        condition.unitToSelect = unitToSelect;
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition SelectUnitsEvent(HashSet<Unit> unitsToSelect, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.SelectUnits;
        condition.unitsToSelect = unitsToSelect;
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition SelectUnitsAmountEvent(HashSet<Unit> unitsToSelect, int amount, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.SelectUnitsAmount;
        condition.unitsToSelect = unitsToSelect;
        condition.intValue = amount;
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition UnselectUnitsEvent(HashSet<Unit> unitsToUnselect, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.UnSelectUnits;
        condition.unitsToSelect = unitsToUnselect;
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition SelectFleetEvent(Fleet fleetToSelect, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.SelectFleet;
        condition.fleetToSelect = fleetToSelect;
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition OpenObjectPanelEvent(BattleObject objectToSelect, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.OpenObjectPanel;
        condition.objectToSelect = objectToSelect;
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition FollowUnitEvent(Unit unitToFollow, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.FollowUnit;
        condition.unitToSelect = unitToFollow;
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition DockShipsAtUnit(List<Ship> shipsToDock, Unit unitToDockAt, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.ShipsDockedAtUnit;
        condition.iObjects = shipsToDock.Cast<IObject>().ToList();
        condition.unitToSelect = unitToDockAt;
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition MoveShipToObject(Unit shipToMove, IObject objectToMoveTo, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.MoveShipToObject;
        condition.unitToSelect = shipToMove;
        condition.iObject = objectToMoveTo;
        condition.visualize = visualise;
        return condition;
    }

    public static EventCondition CommandMoveShipToObjectSequence(Unit shipToMove, List<IObject> objectToCommandToMoveTo, bool visualise = false) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.CommandMoveShipToObjectSequence;
        condition.unitToSelect = shipToMove;
        condition.iObjects = objectToCommandToMoveTo;
        condition.visualize = visualise;
        return condition;
    }
    public static EventCondition PanEvent(float distanceToPan) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.Pan;
        condition.floatValue = distanceToPan;
        condition.floatValue2 = 0;
        return condition;
    }

    public static EventCondition ZoomEvent(float scrollTo) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.Zoom;
        condition.floatValue = scrollTo;
        condition.floatValue2 = LocalPlayer.Instance.GetInputManager().GetCamera().orthographicSize;
        return condition;
    }

    public static EventCondition PredicateEvent(Predicate<EventManager> predicate) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.Predicate;
        condition.predicate = predicate;
        return condition;
    }

    public bool CheckCondition(EventManager eventManager, float deltaTime) {
        switch (conditionType) {
            case ConditionType.Wait:
                floatValue -= deltaTime;
                if (floatValue <= 0)
                    return true;
                break;
            case ConditionType.WaitUntilShipsIdle:
                if (iObjects.Cast<Ship>().All((ship) => ship.IsIdle())) {
                    return true;
                }
                return false;
            case ConditionType.SelectUnit:
                if (eventManager.playerGameInput.GetSelectedUnits().ContainsObject(unitToSelect)
                    && eventManager.playerGameInput.GetSelectedUnits().objects.Count == 1) {
                    return true;
                }
                break;
            case ConditionType.SelectUnits:
                if (eventManager.playerGameInput.GetDisplayedFleet() != null)
                    return false;
                HashSet<Unit> selectedUnits = eventManager.playerGameInput.GetSelectedUnits().GetAllUnits().ToHashSet();
                if (unitsToSelect.All((unit) => selectedUnits.Contains(unit))) {
                    return true;
                }
                break;
            case ConditionType.SelectUnitsAmount:
                if (eventManager.playerGameInput.GetDisplayedFleet() != null)
                    return false;
                HashSet<Unit> selectedUnitsAmount = eventManager.playerGameInput.GetSelectedUnits().GetAllUnits().ToHashSet();
                if (unitsToSelect.Count((unit) => selectedUnitsAmount.Contains(unit)) >= intValue) {
                    return true;
                }
                break;
            case ConditionType.UnSelectUnits:
                HashSet<Unit> unselectUnits = eventManager.playerGameInput.GetSelectedUnits().GetAllUnits().ToHashSet();
                if (unitsToSelect.All((unit) => !unselectUnits.Contains(unit))) {
                    return true;
                }
                break;
            case ConditionType.SelectFleet:
                if (eventManager.playerGameInput.GetSelectedUnits().fleet == fleetToSelect
                    && eventManager.playerGameInput.GetSelectedUnits().groupType == SelectionGroup.GroupType.Fleet) {
                    return true;
                }
                break;
            case ConditionType.OpenObjectPanel:
                if (objectToSelect == null) {
                    if (!LocalPlayer.Instance.playerUI.IsAnObjectMenuShown())
                        return true;
                } else if (eventManager.playerGameInput.rightClickedBattleObject == objectToSelect) {
                    return true;
                }
                break;
            case ConditionType.FollowUnit:
                if (eventManager.playerGameInput.followUnit == unitToSelect) {
                    return true;
                }
                break;
            case ConditionType.MoveShipToObject:
                if (Vector2.Distance(unitToSelect.GetPosition(), iObject.GetPosition()) <= unitToSelect.GetSize() + iObject.GetSize()) {
                    return true;
                }
                break;
            case ConditionType.CommandMoveShipToObjectSequence:
                ShipAI shipAI = ((Ship)unitToSelect).shipAI;
                if (shipAI.commands.Count < iObjects.Count) return false;
                for (int i = 0; i < iObjects.Count; i++) {
                    if (shipAI.commands[i].commandType != Command.CommandType.Move
                        || Vector2.Distance(shipAI.commands[i].targetPosition, iObjects[i].GetPosition()) > unitToSelect.GetSize() + iObjects[i].GetSize()) {
                        return false;
                    }
                }
                return true;
            case ConditionType.ShipsDockedAtUnit:
                foreach (var ship in iObjects.Cast<Ship>()) {
                    if (ship.dockedStation != unitToSelect) {
                        return false;
                    }
                }
                return true;
            case ConditionType.Pan:
                // We can't just take the position of the camera here because the player might be following a ship
                // Resulting in non player camera movement
                floatValue2 += eventManager.panDelta;
                if (floatValue2 >= floatValue) {
                    return true;
                }
                break;
            case ConditionType.Zoom:
                float currentSize = LocalPlayer.Instance.GetInputManager().GetCamera().orthographicSize;
                if (floatValue2 > floatValue) {
                    // Zooming in
                    if (currentSize <= floatValue) return true;
                } else {
                    // Zooming out
                    if (currentSize >= floatValue) return true;
                }
                break;
            case ConditionType.Predicate:
                if (predicate(eventManager))
                    return true;
                break;
        }
        return false;
    }
}
