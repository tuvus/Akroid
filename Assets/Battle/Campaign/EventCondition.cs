using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventCondition {
    public enum ConditionType {
        Wait,
        SelectUnit,
        SelectFleet,
        Predicate,
    }

    public ConditionType conditionType { get; protected set; }
    public Unit unitToSelect { get; protected set; }
    public Fleet fleetToSelect { get; protected set; }
    public float timeToWait { get; protected set; }
    public Predicate<EventManager> predicate { get; protected set; }

    public EventCondition(float timeToWait) {
        this.conditionType = ConditionType.Wait;
        this.timeToWait = timeToWait;
    }

    public EventCondition(Unit unitToSelect) {
        this.conditionType = ConditionType.SelectUnit;
        this.unitToSelect = unitToSelect;
    }

    public EventCondition(Fleet fleetToSelect) {
        this.conditionType = ConditionType.SelectFleet;
        this.fleetToSelect = fleetToSelect;
    }

    public EventCondition(Predicate<EventManager> predicate) {
        this.conditionType = ConditionType.Predicate;
        this.predicate = predicate;
    }

    public bool CheckCondition(EventManager eventManager, float deltaTime) {
        switch (conditionType) {
            case ConditionType.Wait:
                timeToWait -= deltaTime;
                if (timeToWait <= 0)
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
            case ConditionType.Predicate:
                if (predicate(eventManager))
                    return true;
                break;
        }
        return false;
    }
}
