using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIEventManager : EventManager {
    private LocalPlayer localPlayer;
    private PlayerUI playerUI;
    private LocalPlayerGameInput playerGameInput;
    private UnitSpriteManager unitSpriteManager;
    private List<Action> uIEventList;

    public UIEventManager(LocalPlayer localPlayer, LocalPlayerGameInput playerGameInput, UnitSpriteManager unitSpriteManager) {
        this.localPlayer = localPlayer;
        playerUI = localPlayer.GetPlayerUI();
        this.playerGameInput = playerGameInput;
        this.unitSpriteManager = unitSpriteManager;
        uIEventList = new List<Action>();
        playerUI.playerEventUI.SetEventManager(this);
    }


    /// <summary>
    /// Applies UI actions during the UI update.
    /// </summary>
    public void UpdateUIEvents() {
        uIEventList.ForEach(a => a.Invoke());
        uIEventList.Clear();
        foreach (var activeEvent in ActiveEvents.ToList()) {
            if (activeEvent.Item1 is UIEventCondition uIActiveEvent) {
                if (uIActiveEvent.CheckUICondition(this)) {
                    ActiveEvents.Remove(activeEvent);
                    activeEvent.Item2();
                }
            }
        }
    }

    public override EventCondition CreateSelectUnitEvent(Unit unitToSelect, bool visualize = false) {
        return new SelectUnitsAmountCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.SelectUnit, unitToSelect,
            visualize);
    }

    public override EventCondition CreateSelectUnitsEvent(List<Unit> unitsToSelect, bool visualize = false) {
        return new SelectUnitsAmountCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.SelectUnit, unitsToSelect,
            visualize);
    }

    public override EventCondition CreateSelectUnitsAmountEvent(List<Unit> unitsToSelect, int amount, bool visualize = false) {
        return new SelectUnitsAmountCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.SelectUnit, unitsToSelect,
            amount, visualize);
    }

    public override EventCondition CreateUnselectUnitsEvent(List<Unit> unitsToUnselect, bool visualize = false) {
        return new UnSelectUnitsCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.SelectUnit, unitsToUnselect,
            visualize);
    }

    public override EventCondition CreateSelectFleetEvent(Fleet fleetToSelect, bool visualize = false) {
        return new SelectFleetsCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.SelectUnit, fleetToSelect, visualize);
    }

    public override EventCondition CreateOpenObjectPanelEvent(BattleObject objectToSelect, bool visualize = false) {
        return new OpenObjectPanelCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.OpenObjectPanel, objectToSelect,
            visualize);
    }

    public override EventCondition CreateOpenFactionPanelEvent(Faction factionToSelect, bool visualize = false) {
        return new OpenFactionPanelCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.OpenFactionPanel, factionToSelect,
            visualize);
    }

    public override EventCondition CreateFollowUnitEvent(Unit unitToFollow, bool visualize = false) {
        return new FollowUnitCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.FollowUnit, unitToFollow, visualize);
    }

    public override EventCondition CreatePanEvent(float distanceToPan) {
        return new PanCondtion(localPlayer, unitSpriteManager, EventCondition.ConditionType.Pan, distanceToPan);
    }

    public override EventCondition CreateZoomEvent(float zoomTo) {
        return new ZoomCondtion(localPlayer, unitSpriteManager, EventCondition.ConditionType.Zoom, zoomTo);
    }

    public override void SetPlayerZoom(float zoom) {
        uIEventList.Add(() => playerGameInput.SetZoom(zoom));
    }

    public override void CenterPlayerCamera() {
        uIEventList.Add(() => playerGameInput.CenterCamera());
    }

    public override void StartFollowingUnit(Unit unit) {
        uIEventList.Add(() => playerGameInput.StartFollowingUnit(unitSpriteManager.units[unit]));
    }
}
