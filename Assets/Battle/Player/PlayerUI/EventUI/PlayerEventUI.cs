using System;
using System.Collections.Generic;
using System.Linq;
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
            RemoveVisuals();
            switch (VisualizedEvent.conditionType) {
                case EventCondition.ConditionType.Wait:
                    break;
                case EventCondition.ConditionType.SelectUnit:
                    Instantiate(unitHighlight, worldSpaceTransform);
                    break;
                case EventCondition.ConditionType.SelectFleet:
                    Instantiate(unitHighlight, worldSpaceTransform);
                    break;
                case EventCondition.ConditionType.SelectUnits:
                case EventCondition.ConditionType.SelectUnitsAmount:
                    foreach (var _ in VisualizedEvent.unitsToSelect) {
                        Instantiate(unitHighlight, worldSpaceTransform);
                    }
                    break;
                case EventCondition.ConditionType.OpenObjectPanel:
                    Instantiate(unitHighlight, worldSpaceTransform);
                    break;
                case EventCondition.ConditionType.FollowUnit:
                    Instantiate(unitHighlight, worldSpaceTransform);
                    break;
                case EventCondition.ConditionType.Predicate:
                    break;
            }
        }
        switch (VisualizedEvent.conditionType) {
            case EventCondition.ConditionType.Wait:
                break;
            case EventCondition.ConditionType.SelectUnit:
                // If the unit is docked at a station, we need to show the station instead
                Unit unitToShow = VisualizedEvent.unitToSelect;
                if (VisualizedEvent.unitToSelect.IsShip() && ((Ship)VisualizedEvent.unitToSelect).dockedStation != null) {
                    unitToShow = ((Ship)VisualizedEvent.unitToSelect).dockedStation;
                }
                worldSpaceTransform.GetChild(0).position = unitToShow.GetPosition();
                float unitSize = Math.Max(unitToShow.GetSize() / 3, LocalPlayer.Instance.GetLocalPlayerInput().GetCamera().orthographicSize / 100);
                worldSpaceTransform.GetChild(0).localScale = new Vector2(unitSize, unitSize);
                break;
            case EventCondition.ConditionType.SelectUnits:
            case EventCondition.ConditionType.SelectUnitsAmount:
                HashSet<Unit> selectedUnits = EventManager.playerGameInput.GetSelectedUnits().GetAllUnits().ToHashSet();
                List<Unit> unitsToSelect = VisualizedEvent.unitsToSelect.ToList();
                for (int i = 0; i < unitsToSelect.Count; i++) {
                    Unit unitToShow2 = unitsToSelect[i];
                    if (selectedUnits.Contains(unitToShow2)) {
                        worldSpaceTransform.GetChild(i).GetComponent<SpriteRenderer>().enabled = false;
                    } else {
                        worldSpaceTransform.GetChild(i).GetComponent<SpriteRenderer>().enabled = true;
                        worldSpaceTransform.GetChild(i).position = unitToShow2.GetPosition();
                        float unitSize2 = Math.Max(unitToShow2.GetSize() / 3, LocalPlayer.Instance.GetLocalPlayerInput().GetCamera().orthographicSize / 100);
                        worldSpaceTransform.GetChild(i).localScale = new Vector2(unitSize2, unitSize2);
                    }
                }
                break;
            case EventCondition.ConditionType.OpenObjectPanel:
                if (VisualizedEvent.unitToSelect == null)
                    break;
                Unit unitPanelToOpen = VisualizedEvent.unitToSelect;
                worldSpaceTransform.GetChild(0).position = unitPanelToOpen.GetPosition();
                float unitSize3 = Math.Max(unitPanelToOpen.GetSize() / 3, LocalPlayer.Instance.GetLocalPlayerInput().GetCamera().orthographicSize / 100);
                worldSpaceTransform.GetChild(0).localScale = new Vector2(unitSize3, unitSize3);
                break;
            case EventCondition.ConditionType.SelectFleet:
                worldSpaceTransform.GetChild(0).position = VisualizedEvent.fleetToSelect.GetPosition();
                float fleetSize = Math.Max(VisualizedEvent.fleetToSelect.GetSize() / 4, LocalPlayer.Instance.GetLocalPlayerInput().GetCamera().orthographicSize / 100);
                worldSpaceTransform.GetChild(0).localScale = new Vector2(fleetSize, fleetSize);
                break;
            case EventCondition.ConditionType.FollowUnit:
                if (VisualizedEvent.unitToSelect == null)
                    break;
                Unit unitToFollow = VisualizedEvent.unitToSelect;
                worldSpaceTransform.GetChild(0).position = unitToFollow.GetPosition();
                float unitSize4 = Math.Max(unitToFollow.GetSize() / 3, LocalPlayer.Instance.GetLocalPlayerInput().GetCamera().orthographicSize / 100);
                worldSpaceTransform.GetChild(0).localScale = new Vector2(unitSize4, unitSize4);
                break;
            case EventCondition.ConditionType.Predicate:
                break;
        }
    }

    void RemoveVisuals() {
        for (int i = worldSpaceTransform.childCount - 1; i >= 0; i--) {
            GameObject.Destroy(worldSpaceTransform.GetChild(i).gameObject);
        }
    }

    public void SetEventManager(EventManager eventManager) {
        this.EventManager = eventManager;
    }

    public void SetWorldSpaceTransform(Transform worldSpaceTransform) {
        this.worldSpaceTransform = worldSpaceTransform;
    }
}
