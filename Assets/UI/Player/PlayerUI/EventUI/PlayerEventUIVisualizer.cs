using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEventUIVisualizer : MonoBehaviour {
    [SerializeField] private GameObject unitHighlight;
    [SerializeField] private GameObject unitArrow;
    [SerializeField] private GameObject buttonHighlight;
    private Transform arrowTransform;
    private List<Button> buttonsToVisualize;
    private Transform buttonTransform;
    private Transform highlightTransform;
    private LocalPlayer localPlayer;
    private List<ObjectUI> objectsToVisualize;
    private PlayerUI playerUI;
    private UIEventManager uIEventManager;
    private Transform worldSpaceTransform;
    private float currentScale;
    private bool scaleDirection;


    public void SetupEventUI(UIManager uIManager, UIEventManager uIEventManager, LocalPlayer localPlayer,
        PlayerUI playerUI) {
        worldSpaceTransform = uIManager.GetEventVisulationTransform();
        highlightTransform = worldSpaceTransform.GetChild(0);
        arrowTransform = worldSpaceTransform.GetChild(1);
        buttonTransform = transform.GetChild(0);
        this.localPlayer = localPlayer;
        this.playerUI = playerUI;
        this.uIEventManager = uIEventManager;
        objectsToVisualize = new List<ObjectUI>();
        buttonsToVisualize = new List<Button>();
    }

    public void UpdateEventUI() {
        bool newEvent = false;
        // Check if we aren't set up yet
        if (uIEventManager == null || worldSpaceTransform == null)
            return;

        List<UICondition> visualConditions = new List<UICondition>();
        visualConditions.AddRange(uIEventManager.ActiveEvents.Where(e => e.Key.visualize && e.Key is UICondition).Select(e => (UICondition)e.Key));

        if (scaleDirection) {
            currentScale = math.min(0.1f, currentScale + .05f * Time.deltaTime);
            if (currentScale >= 0.1f) scaleDirection = false;
        } else {
            currentScale = math.max(0, currentScale - .05f * Time.deltaTime);
            if (currentScale <= 0) scaleDirection = true;
        }
        VisualizeEvent(visualConditions);
    }

    private void VisualizeEvent(List<UICondition> visualConditions) {
        objectsToVisualize.Clear();
        buttonsToVisualize.Clear();
        visualConditions.ForEach(ec => ec.GetVisualizedObjects(objectsToVisualize, buttonsToVisualize));
        VisualizeObjects(objectsToVisualize);
        VisualizeButtons(buttonsToVisualize);
    }

    private void RemoveVisuals() {
        for (int i = highlightTransform.childCount - 1; i >= 0; i--) {
            Destroy(highlightTransform.GetChild(i).gameObject);
        }

        for (int i = arrowTransform.childCount - 1; i >= 0; i--) {
            Destroy(arrowTransform.GetChild(i).gameObject);
        }
    }

    private void VisualizeObjects(List<ObjectUI> objectsToVisualize) {
        int arrowCount = 0;
        Camera camera = localPlayer.GetLocalPlayerInput().mainCamera;

        for (int i = 0; i < objectsToVisualize.Count; i++) {
            ObjectUI obj = objectsToVisualize[i];
            if (highlightTransform.childCount <= i) Instantiate(unitHighlight, highlightTransform);
            Transform visualEffect = highlightTransform.GetChild(i);
            visualEffect.GetComponent<SpriteRenderer>().enabled = true;
            visualEffect.position = obj.transform.position;
            float objectSizeDivisor = 3;
            if (obj.iObject.IsGroup())
                objectSizeDivisor = 4;
            float objectSize = obj.iObject.GetSize();
            if (obj is UnitUI unitUI) objectSize *= math.max(1, unitUI.unitIconUI.GetSize());
            float highlightSize = Math.Max(objectSize / objectSizeDivisor, camera.orthographicSize / 100);
            visualEffect.localScale = new Vector2(highlightSize, highlightSize);

            if (!localPlayer.GetLocalPlayerInput().IsObjectInViewingField(obj)) {
                if (arrowTransform.childCount <= arrowCount) Instantiate(unitArrow, arrowTransform);
                Transform arrow = arrowTransform.GetChild(arrowCount);
                arrow.GetComponent<SpriteRenderer>().enabled = true;
                arrow.position = Vector2.MoveTowards(camera.transform.position, obj.iObject.GetPosition(),
                    camera.orthographicSize / 1);
                arrow.eulerAngles = new Vector3(0, 0,
                    Calculator.GetAngleOutOfTwoPositions(arrow.position, obj.iObject.GetPosition()));
                arrow.localScale = Vector2.one * camera.orthographicSize / 50 * (1 + currentScale);
                arrowCount++;
            }
        }

        for (int i = objectsToVisualize.Count; i < highlightTransform.childCount; i++) {
            highlightTransform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
        }

        for (int i = arrowCount; i < arrowTransform.childCount; i++) {
            arrowTransform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void VisualizeButtons(List<Button> buttonsToVisualize) {
        for (int i = 0; i < buttonsToVisualize.Count; i++) {
            if (buttonTransform.childCount <= i) Instantiate(buttonHighlight, buttonTransform);
            RectTransform visualizationTransform = buttonTransform.GetChild(i).GetComponent<RectTransform>();
            visualizationTransform.position = buttonsToVisualize[i].GetComponent<RectTransform>().position;
            visualizationTransform.sizeDelta =
                buttonsToVisualize[i].GetComponent<RectTransform>().sizeDelta + new Vector2(2, 2);
            visualizationTransform.gameObject.SetActive(true);
            visualizationTransform.localScale = new Vector3(1 + currentScale, 1 + currentScale * 2, 1);
        }

        for (int i = buttonsToVisualize.Count; i < buttonTransform.childCount; i++) {
            buttonTransform.GetChild(i).gameObject.SetActive(false);
        }
    }
}
