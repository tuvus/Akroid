using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

public class PlayerEventUI : MonoBehaviour {
    protected PlayerUI playerUI;
    protected EventManager EventManager;
    protected Tuple<EventCondition, Action> EventConditionTuple;
    protected EventCondition VisualizedEvent;
    protected Transform worldSpaceTransform;
    [SerializeField] GameObject unitHighlight;


    public void SetupEventUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void UpdateEventUI() {
        bool newEvent = false;
        // Check if we aren't set up yet
        if (EventManager == null || worldSpaceTransform == null)
            return;
        if (!EventManager.ActiveEvents.Contains(EventConditionTuple)) {
            RemoveVisuals();
            EventConditionTuple = null;
            VisualizedEvent = null;
        }
        if (VisualizedEvent == null) {
            EventConditionTuple = EventManager.ActiveEvents.FirstOrDefault(e => e.Item1.visualize);
            if (EventConditionTuple != null) VisualizedEvent = EventConditionTuple.Item1;
            newEvent = true;
        }
        if (VisualizedEvent != null) {
            VisualizeEvent(newEvent);
        }
    }

    void VisualizeEvent(bool newEvent) {
        if (newEvent) {
            VisualizeObjects(new List<IObject>());
        }
        switch (VisualizedEvent.conditionType) {
            case EventCondition.ConditionType.SelectUnit:
                // If the unit is docked at a station, we need to show the station instead
                Unit unitToShow = VisualizedEvent.unitToSelect;
                if (VisualizedEvent.unitToSelect.IsShip() && ((Ship)VisualizedEvent.unitToSelect).dockedStation != null) {
                    unitToShow = ((Ship)VisualizedEvent.unitToSelect).dockedStation;
                }
                VisualizeObjects(new List<IObject> { unitToShow });
                break;
            case EventCondition.ConditionType.SelectUnits:
            case EventCondition.ConditionType.SelectUnitsAmount:
                HashSet<Unit> selectedUnits = EventManager.playerGameInput.GetSelectedUnits().GetAllUnits().ToHashSet();
                List<Unit> unitsToSelect = VisualizedEvent.unitsToSelect.ToList();
                VisualizeObjects(unitsToSelect.Select((unit) => selectedUnits.Contains(unit)).ToList().Cast<IObject>().ToList());
                break;
            case EventCondition.ConditionType.OpenObjectPanel:
                if (VisualizedEvent.unitToSelect == null)
                    break;
                VisualizeObjects(new List<IObject>() { VisualizedEvent.unitToSelect });
                break;
            case EventCondition.ConditionType.SelectFleet:
                VisualizeObjects(new List<IObject>() { VisualizedEvent.fleetToSelect });
                break;
            case EventCondition.ConditionType.FollowUnit:
                if (VisualizedEvent.unitToSelect == null)
                    break;
                VisualizeObjects(new List<IObject>() { VisualizedEvent.unitToSelect });
                break;
            case EventCondition.ConditionType.ShipsDockedAtUnit:
                List<IObject> objectsToVisualize = new List<IObject> { VisualizedEvent.unitToSelect };
                foreach (var ship in VisualizedEvent.iObjects.Cast<Ship>().ToList()) {
                    if (ship.dockedStation != VisualizedEvent.unitToSelect
                        && (ship.shipAI.commands.All((command) => !(command.commandType == Command.CommandType.Dock && command.targetUnit == VisualizedEvent.unitToSelect)))) {
                        objectsToVisualize.Add(ship);
                    }
                }
                break;
            case EventCondition.ConditionType.MoveShipToObject:
                // If the unit is docked at a station, we need to show the station instead
                Unit unitToShow2 = VisualizedEvent.unitToSelect;
                HashSet<Unit> selectedUnits3 = EventManager.playerGameInput.GetSelectedUnits().GetAllUnits().ToHashSet();
                if (selectedUnits3.Contains(unitToShow2) && selectedUnits3.Count == 1) {
                    VisualizeObjects(new List<IObject> { VisualizedEvent.iObject });
                } else {
                    if (((Ship)VisualizedEvent.unitToSelect).dockedStation != null) {
                        unitToShow2 = ((Ship)VisualizedEvent.unitToSelect).dockedStation;
                    }
                    VisualizeObjects(new List<IObject> { unitToShow2, VisualizedEvent.iObject });
                }
                break;
            case EventCondition.ConditionType.CommandMoveShipToObjectSequence:
                HashSet<Unit> selectedUnits2 = EventManager.playerGameInput.GetSelectedUnits().GetAllUnits().ToHashSet();
                if (selectedUnits2.Count != 1 || !selectedUnits2.Contains(VisualizedEvent.unitToSelect)) {
                    VisualizeObjects(new List<IObject>() { VisualizedEvent.unitToSelect });
                } else {
                    ShipAI shipAI = ((Ship)VisualizedEvent.unitToSelect).shipAI;
                    IObject objectToVisualize = VisualizedEvent.iObjects.First();
                    for (int i = 0; i < VisualizedEvent.iObjects.Count - 1; i++) {
                        if (shipAI.commands.Count <= i || shipAI.commands[i].commandType != Command.CommandType.Move
                            || Vector2.Distance(shipAI.commands[i].targetPosition, VisualizedEvent.iObjects[i].GetPosition()) > VisualizedEvent.unitToSelect.GetSize() + VisualizedEvent.iObjects[i].GetSize()) {
                            break;
                        }
                        objectToVisualize = VisualizedEvent.iObjects[i + 1];
                    }
                    VisualizeObjects(new List<IObject>() { objectToVisualize });
                }
                break;
        }
    }

    void RemoveVisuals() {
        for (int i = worldSpaceTransform.childCount - 1; i >= 0; i--) {
            GameObject.Destroy(worldSpaceTransform.GetChild(i).gameObject);
        }
    }

    void VisualizeObjects(List<IObject> objectsToVisualise) {
        for (int i = 0; i < objectsToVisualise.Count; i++) {
            IObject obj = objectsToVisualise[i];
            if (worldSpaceTransform.childCount <= i)
                Instantiate(unitHighlight, worldSpaceTransform);
            Transform visualEffect = worldSpaceTransform.GetChild(i);
            visualEffect.GetComponent<SpriteRenderer>().enabled = true;
            visualEffect.position = obj.GetPosition();
            float objectSizeDivisor = 3;
            if (obj.IsGroup())
                objectSizeDivisor = 4;
            float objectSize = Math.Max(obj.GetSize() / objectSizeDivisor, LocalPlayer.Instance.GetLocalPlayerInput().GetCamera().orthographicSize / 100);
            visualEffect.localScale = new Vector2(objectSize, objectSize);
        }
        for (int i = objectsToVisualise.Count; i < worldSpaceTransform.childCount; i++) {
            worldSpaceTransform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    public void SetEventManager(EventManager eventManager) {
        this.EventManager = eventManager;
    }

    public void SetWorldSpaceTransform(Transform worldSpaceTransform) {
        this.worldSpaceTransform = worldSpaceTransform;
    }
}
