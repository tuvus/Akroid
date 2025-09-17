using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIEventManager : EventManager {
    private readonly LocalPlayer localPlayer;
    private readonly LocalPlayerGameInput playerGameInput;
    private PlayerUI playerUI;
    private readonly UIBattleManager uiBattleManager;
    private readonly List<Action> uIEventList;

    public UIEventManager(BattleManager battleManager, LocalPlayer localPlayer, LocalPlayerGameInput playerGameInput,
        UIBattleManager uiBattleManager) : base(battleManager) {
        this.localPlayer = localPlayer;
        playerUI = localPlayer.GetPlayerUI();
        this.playerGameInput = playerGameInput;
        this.uiBattleManager = uiBattleManager;
        uIEventList = new List<Action>();
    }


    /// <summary>
    ///     Applies UI actions during the UI update.
    /// </summary>
    public void UpdateUIEvents() {
        uIEventList.ForEach(a => a.Invoke());
        uIEventList.Clear();
        foreach (var activeEvent in ActiveEvents.ToList()) {
            if (activeEvent.Key is UICondition uIActiveCondition) {
                if (uIActiveCondition.CheckUICondition(this)) {
                    ActiveEvents.Remove(activeEvent.Key);
                    activeEvent.Value();
                }
            } else if (activeEvent.Key is UIEvent uIActiveEvent) {
                if (uIActiveEvent.CheckUICondition(this)) {
                    ActiveEvents.Remove(activeEvent.Key);
                    activeEvent.Value();
                } else {
                    uIActiveEvent.UpdateUI(this);
                }
            }
        }
    }

    public override EventCondition CreateMoveShipToObject(Ship shipToMove, IObject objectToCommandMoveTo,
        float distance = 0,
        bool visualize = false) {
        return new MoveShipsToObjectUICondition(
            (MoveShipsToObject)base.CreateMoveShipToObject(shipToMove, objectToCommandMoveTo, distance, visualize),
            localPlayer,
            uiBattleManager, visualize);
    }

    public override EventCondition CreateMoveShipsToObject(List<Ship> shipsToMove, IObject objectToCommandMoveTo,
        float distance = 0,
        bool visualize = false) {
        return new MoveShipsToObjectUICondition(
            (MoveShipsToObject)base.CreateMoveShipsToObject(shipsToMove, objectToCommandMoveTo, distance, visualize),
            localPlayer,
            uiBattleManager, visualize);
    }

    public override EventCondition CreateDockShipAtUnit(Ship shipToDock, Station unitToDockAt, bool visualize = false) {
        return new DockShipsAtUnitUICondition(
            (DockShipsAtUnitCondition)base.CreateDockShipAtUnit(shipToDock, unitToDockAt, visualize),
            localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateDockShipsAtUnit(List<Ship> shipsToDock, Station unitToDockAt,
        bool visualize = false) {
        return new DockShipsAtUnitUICondition(
            (DockShipsAtUnitCondition)base.CreateDockShipsAtUnit(shipsToDock, unitToDockAt, visualize),
            localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateCommandMoveShipToObject(Ship shipToMove, IObject objectToCommandMoveTo,
        bool visualize = false) {
        return new ShipCommandMoveToObjectsUICondition(
            (ShipCommandMoveToObjectsCondition)base.CreateCommandMoveShipToObject(shipToMove, objectToCommandMoveTo,
                visualize),
            localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateCommandMoveShipsToObject(List<Ship> shipsToMove, IObject objectToCommandMoveTo,
        bool visualize = false) {
        return new ShipCommandMoveToObjectsUICondition(
            (ShipCommandMoveToObjectsCondition)base.CreateCommandMoveShipsToObject(shipsToMove, objectToCommandMoveTo,
                visualize),
            localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateCommandMoveShipToObjects(Ship shipToMove,
        List<IObject> objectsToCommandToMoveTo,
        bool visualize = false) {
        return new ShipCommandMoveToObjectsUICondition(
            (ShipCommandMoveToObjectsCondition)base.CreateCommandMoveShipToObjects(shipToMove, objectsToCommandToMoveTo,
                visualize),
            localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateCommandMoveShipsToObjects(List<Ship> shipsToMove,
        List<IObject> objectsToCommandToMoveTo,
        bool visualize = false) {
        return new ShipCommandMoveToObjectsUICondition(
            (ShipCommandMoveToObjectsCondition)base.CreateCommandMoveShipsToObjects(shipsToMove,
                objectsToCommandToMoveTo, visualize),
            localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateCommandDockShipToUnit(Ship shipToMove, Station unitToDockAt,
        bool visualize = false) {
        return new ShipsCommandUICondition(
            (ShipsCommandCondition)base.CreateCommandDockShipToUnit(shipToMove, unitToDockAt, visualize),
            localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateCommandShipToCollectGas(Ship shipToMove, GasCloud gasCloud = null,
        Station returnStation = null,
        bool visualize = false) {
        return new ShipsCommandUICondition((ShipsCommandCondition)base.CreateCommandShipToCollectGas(shipToMove,
            gasCloud, returnStation,
            visualize), localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateBuildShipAtStation(Ship.ShipBlueprint shipBlueprint, Faction faction,
        Station station = null,
        bool visualize = false) {
        return new BuildShipsAtStationUICondition((BuildShipsAtStation)base.CreateBuildShipAtStation(shipBlueprint,
            faction, station,
            visualize), localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateBuildShipsAtStation(List<Ship.ShipBlueprint> shipBlueprints, Faction faction,
        Station station,
        bool visualize = false) {
        return new BuildShipsAtStationUICondition((BuildShipsAtStation)base.CreateBuildShipsAtStation(shipBlueprints,
            faction, station,
            visualize), localPlayer, uiBattleManager, visualize);
    }

    public override EventCondition CreateSelectUnitCondition(Unit unitToSelect, bool visualize = false) {
        return new SelectUnitsAmountCondition(localPlayer, uiBattleManager, EventCondition.ConditionType.SelectUnit,
            unitToSelect,
            visualize);
    }

    public override EventCondition CreateSelectUnitsCondition(List<Unit> unitsToSelect, bool visualize = false) {
        return new SelectUnitsAmountCondition(localPlayer, uiBattleManager, EventCondition.ConditionType.SelectUnits,
            unitsToSelect,
            visualize);
    }

    public override EventCondition CreateSelectUnitsAmountCondition(List<Unit> unitsToSelect, int amount,
        bool visualize = false) {
        return new SelectUnitsAmountCondition(localPlayer, uiBattleManager,
            EventCondition.ConditionType.SelectUnitsAmount, unitsToSelect,
            amount, visualize);
    }

    public override EventCondition CreateUnselectUnitsCondition(List<Unit> unitsToUnselect, bool visualize = false) {
        return new UnSelectUnitsCondition(localPlayer, uiBattleManager, unitsToUnselect,
            visualize);
    }

    public override EventCondition CreateSelectFleetCondition(Fleet fleetToSelect, bool visualize = false) {
        return new SelectFleetsCondition(localPlayer, uiBattleManager, fleetToSelect, visualize);
    }

    public override EventCondition CreateOpenObjectPanelCondition(BattleObject objectToSelect, bool visualize = false) {
        return new OpenObjectPanelCondition(localPlayer, uiBattleManager, objectToSelect, visualize);
    }

    public override EventCondition CreateOpenFactionPanelCondition(Faction factionToSelect, bool visualize = false) {
        return new OpenFactionPanelCondition(localPlayer, uiBattleManager, factionToSelect,
            visualize);
    }

    public override EventCondition CreateFollowUnitCondition(Unit unitToFollow, bool visualize = false) {
        return new FollowUnitCondition(localPlayer, uiBattleManager, unitToFollow, visualize);
    }

    public override EventCondition CreatePanCondition(float distanceToPan) {
        return new PanCondition(localPlayer, uiBattleManager, distanceToPan);
    }

    public override EventCondition CreateZoomCondition(float zoomTo) {
        return new ZoomCondition(localPlayer, uiBattleManager, zoomTo);
    }

    public override EventCondition CreateLateCondition(Func<EventCondition> eventConditionFunction) {
        return new LateUICondition((LateCondition)base.CreateLateCondition(eventConditionFunction), localPlayer,
            uiBattleManager);
    }

    public override EventCondition CreateMoveCameraCondition(Vector2 startPos, Vector2 endPos, float startZoom,
        float endZoom, float duration) {
        return new MoveCamera(localPlayer, uiBattleManager, startPos, endPos, startZoom, endZoom, duration);
    }

    public override EventCondition CreateMoveCameraToIObjectCondition(Vector2 startPos, IObject iObject,
        float startZoom, float endZoom, float duration) {
        return new MoveCameraToIObject(localPlayer, uiBattleManager, startPos, iObject, startZoom, endZoom, duration);
    }

    public override EventCondition CreateMakeDiscoveryCondition(Faction factionToResearch, bool visualize = false) {
        return new MakeDiscoveryCondition(localPlayer, uiBattleManager, factionToResearch, visualize);
    }

    public override void SetPlayerZoom(float zoom) {
        uIEventList.Add(() => playerGameInput.SetZoom(zoom));
    }

    public override void CenterPlayerCamera() {
        uIEventList.Add(() => playerGameInput.CenterCamera());
    }

    public override void StartFollowingUnit(Unit unit) {
        uIEventList.Add(() => playerGameInput.StartFollowingUnit(uiBattleManager.units[unit]));
    }
}
