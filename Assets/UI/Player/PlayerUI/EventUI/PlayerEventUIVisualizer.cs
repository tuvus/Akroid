using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerEventUIVisualizer : MonoBehaviour {
    private LocalPlayer localPlayer;
    private PlayerUI playerUI;
    private UIEventManager uIEventManager;
    private Tuple<EventCondition, Action> eventConditionTuple;
    private UIEventCondition visualizedEvent;
    private Transform worldSpaceTransform;
    [SerializeField] GameObject unitHighlight;
    private List<ObjectUI> objectsToVisualize;


    public void SetupEventUI(UIManager uIManager, UIEventManager uIEventManager, LocalPlayer localPlayer, PlayerUI playerUI) {
        worldSpaceTransform = uIManager.GetEventVisulationTransform();
        this.localPlayer = localPlayer;
        this.playerUI = playerUI;
        this.uIEventManager = uIEventManager;
        objectsToVisualize = new List<ObjectUI>();
    }

    public void UpdateEventUI() {
        bool newEvent = false;
        // Check if we aren't set up yet
        if (uIEventManager == null || worldSpaceTransform == null)
            return;
        if (!uIEventManager.ActiveEvents.Contains(eventConditionTuple)) {
            RemoveVisuals();
            eventConditionTuple = null;
            visualizedEvent = null;
        }

        if (visualizedEvent == null) {
            eventConditionTuple = uIEventManager.ActiveEvents.FirstOrDefault(e => e.Item1.visualize && e.Item1 is UIEventCondition);
            if (eventConditionTuple != null) visualizedEvent = (UIEventCondition)eventConditionTuple.Item1;
            newEvent = true;
        }

        if (visualizedEvent != null) {
            VisualizeEvent(newEvent);
        }
    }

    void VisualizeEvent(bool newEvent) {
        objectsToVisualize.Clear();
        visualizedEvent.GetVisualizedObjects(objectsToVisualize);
        VisualizeObjects(objectsToVisualize);
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

    void VisualizeObjects(List<ObjectUI> objectsTovisualize) {
        for (int i = 0; i < objectsTovisualize.Count; i++) {
            ObjectUI obj = objectsTovisualize[i];
            if (worldSpaceTransform.childCount <= i)
                Instantiate(unitHighlight, worldSpaceTransform);
            Transform visualEffect = worldSpaceTransform.GetChild(i);
            visualEffect.GetComponent<SpriteRenderer>().enabled = true;
            visualEffect.position = obj.transform.position;
            float objectSizeDivisor = 3;
            if (obj.iObject.IsGroup())
                objectSizeDivisor = 4;
            float objectSize = Math.Max(obj.iObject.GetSize() / objectSizeDivisor,
                LocalPlayer.Instance.GetLocalPlayerInput().GetCamera().orthographicSize / 100);
            visualEffect.localScale = new Vector2(objectSize, objectSize);
        }

        for (int i = objectsTovisualize.Count; i < worldSpaceTransform.childCount; i++) {
            worldSpaceTransform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    public void SetWorldSpaceTransform(Transform worldSpaceTransform) {
        this.worldSpaceTransform = worldSpaceTransform;
    }

    private SelectionGroup GetSelectedUnits() {
        return LocalPlayer.Instance.GetLocalPlayerGameInput().GetSelectedUnits();
    }
}
