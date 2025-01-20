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
    private Transform highlightTransform;
    private Transform arrowTransform;
    [SerializeField] GameObject unitHighlight;
    [SerializeField] GameObject unitArrow;
    private List<ObjectUI> objectsToVisualize;


    public void SetupEventUI(UIManager uIManager, UIEventManager uIEventManager, LocalPlayer localPlayer, PlayerUI playerUI) {
        worldSpaceTransform = uIManager.GetEventVisulationTransform();
        highlightTransform = worldSpaceTransform.GetChild(0);
        arrowTransform = worldSpaceTransform.GetChild(1);
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

        if (visualizedEvent == null || !visualizedEvent.visualize) {
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
    }

    void RemoveVisuals() {
        for (int i = highlightTransform.childCount - 1; i >= 0; i--) {
            GameObject.Destroy(highlightTransform.GetChild(i).gameObject);
        }
        for (int i = arrowTransform.childCount - 1; i >= 0; i--) {
            GameObject.Destroy(arrowTransform.GetChild(i).gameObject);
        }
    }

    void VisualizeObjects(List<ObjectUI> objectsTovisualize) {
        int arrowCount = 0;
        Camera camera = localPlayer.GetLocalPlayerInput().mainCamera;
        for (int i = 0; i < objectsTovisualize.Count; i++) {
            ObjectUI obj = objectsTovisualize[i];
            if (highlightTransform.childCount <= i) Instantiate(unitHighlight, highlightTransform);
            Transform visualEffect = highlightTransform.GetChild(i);
            visualEffect.GetComponent<SpriteRenderer>().enabled = true;
            visualEffect.position = obj.transform.position;
            float objectSizeDivisor = 3;
            if (obj.iObject.IsGroup())
                objectSizeDivisor = 4;
            float objectSize = Math.Max(obj.iObject.GetSize() / objectSizeDivisor, camera.orthographicSize / 100);
            visualEffect.localScale = new Vector2(objectSize, objectSize);

            if (!localPlayer.GetLocalPlayerInput().IsObjectInViewingField(obj)) {
                if (arrowTransform.childCount <= arrowCount) Instantiate(unitArrow, arrowTransform);
                Transform arrow = arrowTransform.GetChild(arrowCount);
                arrow.GetComponent<SpriteRenderer>().enabled = true;
                arrow.position = Vector2.MoveTowards(camera.transform.position, obj.iObject.GetPosition(), camera.orthographicSize / 1);
                arrow.eulerAngles = new Vector3(0, 0, Calculator.GetAngleOutOfTwoPositions(arrow.position, obj.iObject.GetPosition()));
                arrow.localScale = Vector2.one * camera.orthographicSize / 50;
                arrowCount++;
            }
        }

        for (int i = objectsTovisualize.Count; i < highlightTransform.childCount; i++) {
            highlightTransform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
        }
        for (int i = arrowCount; i < arrowTransform.childCount; i++) {
            arrowTransform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
        }
    }
}
