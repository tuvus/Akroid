using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerEventUI : MonoBehaviour {
    protected PlayerUI playerUI;
    protected EventManager eventManager;
    protected Tuple<EventCondition, Action> eventConditionTuple;
    protected EventCondition visualizedEvent;
    protected Transform worldSpaceTransform;
    [SerializeField] GameObject unitHighlight;


    public void SetupEventUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void UpdateEventUI() {
        bool newEvent = false;
        // Check if we aren't set up yet
        if (eventManager == null || worldSpaceTransform == null)
            return;
        if (!eventManager.ActiveEvents.Contains(eventConditionTuple)) {
            RemoveVisuals();
            eventConditionTuple = null;
            visualizedEvent = null;
        }

        if (visualizedEvent == null) {
            eventConditionTuple = eventManager.ActiveEvents.FirstOrDefault(e => e.Item1.visualize);
            if (eventConditionTuple != null) visualizedEvent = eventConditionTuple.Item1;
            newEvent = true;
        }

        if (visualizedEvent != null) {
            VisualizeEvent(newEvent);
        }
    }

    void VisualizeEvent(bool newEvent) {
        if (newEvent) {
            VisualizeObjects(new List<IObject>());
        }

        // switch (visualizedEvent.conditionType) {
        //     case EventCondition.ConditionType.SelectUnit:
        //         // // If the unit is docked at a station, we need to show the station instead
        //         // Unit unitToShow = visualizedEvent;
        //         // if (visualizedEvent.unitToSelect.IsShip() && ((Ship)visualizedEvent.unitToSelect).dockedStation != null) {
        //         //     unitToShow = ((Ship)visualizedEvent.unitToSelect).dockedStation;
        //         // }
        //         //
        //         // VisualizeObjects(new List<IObject> { unitToShow });
        //         break;
        //     case EventCondition.ConditionType.SelectUnits:
        //     case EventCondition.ConditionType.SelectUnitsAmount:
        //         HashSet<Unit> selectedUnits = GetSelectedUnits().GetAllUnits().ToHashSet();
        //         List<Unit> unitsToSelect = visualizedEvent.units.ToList();
        //         if (GetSelectedUnits().fleet != null) {
        //             VisualizeObjects(unitsToSelect.Cast<IObject>().ToList());
        //         } else {
        //             VisualizeObjects(unitsToSelect.Where((unit) => !selectedUnits.Contains(unit)).Cast<IObject>().ToList());
        //         }
        //
        //         break;
        //     case EventCondition.ConditionType.OpenObjectPanel:
        //         if (visualizedEvent.unitToSelect == null)
        //             break;
        //         VisualizeObjects(new List<IObject>() { visualizedEvent.unitToSelect });
        //         break;
        //     case EventCondition.ConditionType.SelectFleet:
        //         VisualizeObjects(new List<IObject>() { visualizedEvent.fleetToSelect });
        //         break;
        //     case EventCondition.ConditionType.FollowUnit:
        //         if (visualizedEvent.unitToSelect == null)
        //             break;
        //         VisualizeObjects(new List<IObject>() { visualizedEvent.unitToSelect });
        //         break;
        //     case EventCondition.ConditionType.ShipsDockedAtUnit:
        //         List<IObject> objectsToVisualize = new List<IObject> { visualizedEvent.unitToSelect };
        //         foreach (var ship in visualizedEvent.iObjects.Cast<Ship>().ToList()) {
        //             if (ship.dockedStation != visualizedEvent.unitToSelect
        //                 && !ship.shipAI.commands.Any((command) =>
        //                     command.commandType == Command.CommandType.Dock &&
        //                     command.destinationStation == visualizedEvent.unitToSelect)) {
        //                 if (ship.dockedStation != null) {
        //                     if (!objectsToVisualize.Contains(ship.dockedStation))
        //                         objectsToVisualize.Add(ship.dockedStation);
        //                 } else {
        //                     objectsToVisualize.Add(ship);
        //                 }
        //             }
        //         }
        //
        //         VisualizeObjects(objectsToVisualize);
        //         break;
        //     case EventCondition.ConditionType.MoveShipsToObject:
        //         // If the unit is docked at a station, we need to show the station instead
        //         Unit unitToShow2 = visualizedEvent.unitToSelect;
        //         HashSet<Unit> selectedUnits3 = GetSelectedUnits().GetAllUnits().ToHashSet();
        //         if (selectedUnits3.Contains(unitToShow2) && selectedUnits3.Count == 1) {
        //             VisualizeObjects(new List<IObject> { visualizedEvent.iObject });
        //         } else {
        //             if (((Ship)visualizedEvent.unitToSelect).dockedStation != null) {
        //                 unitToShow2 = ((Ship)visualizedEvent.unitToSelect).dockedStation;
        //             }
        //
        //             VisualizeObjects(new List<IObject> { unitToShow2, visualizedEvent.iObject });
        //         }
        //
        //         break;
        //     case EventCondition.ConditionType.CommandMoveShipToObjectSequence:
        //         HashSet<Unit> selectedUnits2 = GetSelectedUnits().GetAllUnits().ToHashSet();
        //         if (selectedUnits2.Count != 1 || !selectedUnits2.Contains(visualizedEvent.unitToSelect)) {
        //             VisualizeObjects(new List<IObject>() { visualizedEvent.unitToSelect });
        //         } else {
        //             ShipAI shipAI = ((Ship)visualizedEvent.unitToSelect).shipAI;
        //             int objectIndex = 0;
        //             foreach (var command in shipAI.commands) {
        //                 if (command.commandType == Command.CommandType.Move
        //                     && Vector2.Distance(command.targetPosition, visualizedEvent.iObjects[objectIndex].GetPosition()) <=
        //                     visualizedEvent.unitToSelect.GetSize() + visualizedEvent.iObjects[objectIndex].GetSize()) {
        //                     objectIndex++;
        //                     if (objectIndex == visualizedEvent.iObjects.Count) break;
        //                 }
        //             }
        //
        //             if (objectIndex < visualizedEvent.iObjects.Count) {
        //                 VisualizeObjects(new List<IObject>() { visualizedEvent.iObjects[objectIndex] });
        //             } else {
        //                 VisualizeObjects(new());
        //             }
        //         }
        //
        //         break;
        //
        //     case EventCondition.ConditionType.CommandDockShipToUnit:
        //         HashSet<Unit> selectedUnits4 = GetSelectedUnits().GetAllUnits().ToHashSet();
        //         if (selectedUnits4.Count != 1 || !selectedUnits4.Contains(visualizedEvent.iObjects.First())) {
        //             VisualizeObjects(new List<IObject>() { visualizedEvent.iObjects.First() });
        //         } else {
        //             ShipAI shipAI = ((Ship)visualizedEvent.iObjects.First()).shipAI;
        //             if (!shipAI.commands.Any((c) =>
        //                     c.commandType == Command.CommandType.Dock &&
        //                     c.destinationStation == (Station)visualizedEvent.iObjects.Last())) {
        //                 VisualizeObjects(new List<IObject>() { visualizedEvent.iObjects.Last() });
        //             }
        //         }
        //
        //         break;
        //     case EventCondition.ConditionType.CommandShipToCollectGas:
        //         HashSet<Unit> selectedUnits5 = GetSelectedUnits().GetAllUnits().ToHashSet();
        //         if (selectedUnits5.Count != 1 || !selectedUnits5.Contains(visualizedEvent.iObjects.First())) {
        //             VisualizeObjects(new List<IObject>() { visualizedEvent.iObjects.First() });
        //         }
        //
        //         break;
        //     case EventCondition.ConditionType.LateCondition:
        //         if (visualizedEvent.eventCondition != null && visualizedEvent.eventCondition.visualize) {
        //             visualizedEvent = visualizedEvent.eventCondition;
        //             VisualizeEvent(newEvent);
        //         }
        //
        //         break;
        // }
    }

    void RemoveVisuals() {
        for (int i = worldSpaceTransform.childCount - 1; i >= 0; i--) {
            GameObject.Destroy(worldSpaceTransform.GetChild(i).gameObject);
        }
    }

    void VisualizeObjects(List<IObject> objectsTovisualize) {
        for (int i = 0; i < objectsTovisualize.Count; i++) {
            IObject obj = objectsTovisualize[i];
            if (worldSpaceTransform.childCount <= i)
                Instantiate(unitHighlight, worldSpaceTransform);
            Transform visualEffect = worldSpaceTransform.GetChild(i);
            visualEffect.GetComponent<SpriteRenderer>().enabled = true;
            visualEffect.position = obj.GetPosition();
            float objectSizeDivisor = 3;
            if (obj.IsGroup())
                objectSizeDivisor = 4;
            float objectSize = Math.Max(obj.GetSize() / objectSizeDivisor,
                LocalPlayer.Instance.GetLocalPlayerInput().GetCamera().orthographicSize / 100);
            visualEffect.localScale = new Vector2(objectSize, objectSize);
        }

        for (int i = objectsTovisualize.Count; i < worldSpaceTransform.childCount; i++) {
            worldSpaceTransform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    public void SetEventManager(EventManager eventManager) {
        this.eventManager = eventManager;
    }

    public void SetWorldSpaceTransform(Transform worldSpaceTransform) {
        this.worldSpaceTransform = worldSpaceTransform;
    }

    private SelectionGroup GetSelectedUnits() {
        // return EventManager.playerGameInput.GetSelectedUnits();
        return LocalPlayer.Instance.GetLocalPlayerGameInput().GetSelectedUnits();
    }
}
