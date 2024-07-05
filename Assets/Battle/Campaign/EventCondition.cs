using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventCondition {
    public enum ConditionType {
        Wait,
        SelectUnit,
        SelectFleet,
        Pan,
        Zoom,
        Predicate,
    }

    public ConditionType conditionType { get; protected set; }
    public Unit unitToSelect { get; protected set; }
    public Fleet fleetToSelect { get; protected set; }
    public float floatValue { get; protected set; }
    public float floatValue2 { get; protected set; }
    public Vector2 postionValue { get; protected set; }
    public Predicate<EventManager> predicate { get; protected set; }

    private EventCondition() {
        // No extenal instantiation allowed
    }

    public static EventCondition WaitEvent(float timeToWait) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.Wait;
        condition.floatValue = timeToWait;
        return condition;
    }

    public static EventCondition SelectUnitEvent(Unit unitToSelect) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.SelectUnit;
        condition.unitToSelect = unitToSelect;
        return condition;
    }

    public static EventCondition SelectFleetEvent(Fleet fleetToSelect) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.SelectFleet;
        condition.fleetToSelect = fleetToSelect;
        return condition;
    }

    public static EventCondition PanEvent(float distanceToPan) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.Pan;
        condition.floatValue = distanceToPan;
        condition.floatValue2 = 0;
        return condition;
    }

    public static EventCondition ScrollEvent(float scrollTo) {
        EventCondition condition = new EventCondition();
        condition.conditionType = ConditionType.Pan;
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
            case ConditionType.SelectUnit:
                if (eventManager.playerGameInput.GetSelectedUnits().ContainsObject(unitToSelect)
                    && eventManager.playerGameInput.GetSelectedUnits().objects.Count == 1) {
                    return true;
                }
                break;
            case ConditionType.SelectFleet:
                if (eventManager.playerGameInput.GetSelectedUnits().fleet == fleetToSelect
                    && eventManager.playerGameInput.GetSelectedUnits().groupType == SelectionGroup.GroupType.Fleet) {
                    return true;
                }
                break;
            case ConditionType.Pan:
                Vector2 newPosition = postionValue = LocalPlayer.Instance.GetInputManager().GetCamera().transform.position;
                if (postionValue != null) {
                    floatValue += Vector2.Distance(newPosition, postionValue);
                    if (floatValue >= floatValue2) {
                        return true;
                    }
                }
                postionValue = newPosition;
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
