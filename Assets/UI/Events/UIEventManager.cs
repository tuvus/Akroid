using System;
using System.Collections.Generic;
using System.Linq;

public class UIEventManager : EventManager {
    private LocalPlayer localPlayer;
    private PlayerUI playerUI;
    private LocalPlayerGameInput playerGameInput;
    private UnitSpriteManager unitSpriteManager;
    private List<Action> uIEventList;

    public UIEventManager(BattleManager battleManager, LocalPlayer localPlayer, LocalPlayerGameInput playerGameInput,
        UnitSpriteManager unitSpriteManager) : base(battleManager) {
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

    public override EventCondition CreateSelectUnitCondition(Unit unitToSelect, bool visualize = false) {
        return new SelectUnitsAmountCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.SelectUnit, unitToSelect,
            visualize);
    }

    public override EventCondition CreateSelectUnitsCondition(List<Unit> unitsToSelect, bool visualize = false) {
        return new SelectUnitsAmountCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.SelectUnit, unitsToSelect,
            visualize);
    }

    public override EventCondition CreateSelectUnitsAmountCondition(List<Unit> unitsToSelect, int amount, bool visualize = false) {
        return new SelectUnitsAmountCondition(localPlayer, unitSpriteManager, EventCondition.ConditionType.SelectUnit, unitsToSelect,
            amount, visualize);
    }

    public override EventCondition CreateUnselectUnitsCondition(List<Unit> unitsToUnselect, bool visualize = false) {
        return new UnSelectUnitsCondition(localPlayer, unitSpriteManager, unitsToUnselect,
            visualize);
    }

    public override EventCondition CreateSelectFleetCondition(Fleet fleetToSelect, bool visualize = false) {
        return new SelectFleetsCondition(localPlayer, unitSpriteManager, fleetToSelect, visualize);
    }

    public override EventCondition CreateOpenObjectPanelCondition(BattleObject objectToSelect, bool visualize = false) {
        return new OpenObjectPanelCondition(localPlayer, unitSpriteManager, objectToSelect, visualize);
    }

    public override EventCondition CreateOpenFactionPanelCondition(Faction factionToSelect, bool visualize = false) {
        return new OpenFactionPanelCondition(localPlayer, unitSpriteManager, factionToSelect,
            visualize);
    }

    public override EventCondition CreateFollowUnitCondition(Unit unitToFollow, bool visualize = false) {
        return new FollowUnitCondition(localPlayer, unitSpriteManager, unitToFollow, visualize);
    }

    public override EventCondition CreatePanCondition(float distanceToPan) {
        return new PanCondtion(localPlayer, unitSpriteManager, distanceToPan);
    }

    public override EventCondition CreateZoomCondition(float zoomTo) {
        return new ZoomCondtion(localPlayer, unitSpriteManager, zoomTo);
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
