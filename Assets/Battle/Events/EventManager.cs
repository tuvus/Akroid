using System;
using System.Collections.Generic;
using System.Linq;

public class EventManager {
    protected BattleManager battleManager;
    public HashSet<Tuple<EventCondition, Action>> ActiveEvents { get; private set; }

    public EventManager(BattleManager battleManager) {
        this.battleManager = battleManager;
        ActiveEvents = new HashSet<Tuple<EventCondition, Action>>();
    }

    /// <summary>
    /// Checks the conditions of every event during the regular game update.
    /// </summary>
    public virtual void UpdateEvents(float deltaTime) {
        foreach (var activeEvent in ActiveEvents.ToList()) {
            if (activeEvent.Item1.CheckCondition(this, deltaTime)) {
                ActiveEvents.Remove(activeEvent);
                activeEvent.Item2();
            }
        }
    }

    public void AddEvent(EventCondition condition, Action action) {
        ActiveEvents.Add(new Tuple<EventCondition, Action>(condition, action));
    }

    public virtual EventCondition CreateWaitCondition(float timeToWait) {
        return new WaitCondition(timeToWait);
    }

    public virtual EventCondition CreateWaitUntilShipsIdle(List<Ship> shipsToIdle, bool visualize = false) {
        return new IdleShipsCondition(shipsToIdle, visualize);
    }

    public static Tuple<EventCondition, Action> CreateConditionalWait() {
        WaitTriggerCondition triggerCondition = new WaitTriggerCondition();
        return new Tuple<EventCondition, Action>(triggerCondition, triggerCondition.completer);
    }

    public virtual EventCondition CreateMoveShipToObject(Ship shipToMove, IObject objectToMoveTo, float distance = 0f,
        bool visualize = false) {
        return new MoveShipsToObject(shipToMove, objectToMoveTo, distance, visualize);
    }

    public virtual EventCondition CreateMoveShipsToObject(List<Ship> shipsToMove, IObject objectToMoveTo, float distance = 0f,
        bool visualize = false) {
        return new MoveShipsToObject(shipsToMove, objectToMoveTo, distance, visualize);
    }

    public virtual EventCondition CreateDockShipAtUnit(Ship shipToDock, Station unitToDockAt, bool visualize = false) {
        return new DockShipsAtUnitCondition(shipToDock, unitToDockAt, visualize);
    }

    public virtual EventCondition CreateDockShipsAtUnit(List<Ship> shipsToDock, Station unitToDockAt, bool visualize = false) {
        return new DockShipsAtUnitCondition(shipsToDock, unitToDockAt, visualize);
    }

    public virtual EventCondition CreateCommandMoveShipToObject(Ship shipToMove, IObject objectToCommandMoveTo, bool visualize = false) {
        return new ShipCommandMoveToObjectsCondition(shipToMove, objectToCommandMoveTo, visualize);
    }

    public virtual EventCondition CreateCommandMoveShipToObjects(Ship shipToMove, List<IObject> objectsToCommandToMoveTo,
        bool visualize = false) {
        return new ShipCommandMoveToObjectsCondition(shipToMove, objectsToCommandToMoveTo, visualize);
    }

    public virtual EventCondition CreateCommandMoveShipsToObject(List<Ship> shipsToMove, IObject objectToCommandMoveTo,
        bool visualize = false) {
        return new ShipCommandMoveToObjectsCondition(shipsToMove, objectToCommandMoveTo, visualize);
    }

    public virtual EventCondition CreateCommandMoveShipsToObjects(List<Ship> shipsToMove, List<IObject> objectsToCommandToMoveTo,
        bool visualize = false) {
        return new ShipCommandMoveToObjectsCondition(shipsToMove, objectsToCommandToMoveTo, visualize);
    }

    public virtual EventCondition CreateCommandDockShipToUnit(Ship shipToMove, Station unitToDockAt, bool visualize = false) {
        return new ShipsCommandCondition(shipToMove, Command.CreateDockCommand(unitToDockAt), visualize);
    }

    public virtual EventCondition CreateCommandShipToCollectGas(Ship shipToMove, GasCloud gasCloud = null, Station returnStation = null,
        bool visualize = false) {
        return new ShipsCommandCondition(shipToMove, Command.CreateCollectGasCommand(gasCloud, returnStation), visualize);
    }

    public virtual EventCondition CreateBuildShipAtStation(Ship.ShipBlueprint shipBlueprint, Faction faction, Station station = null,
        bool visualize = false) {
        return new BuildShipsAtStation(new Ship.ShipBlueprint(shipBlueprint, faction), faction, station, visualize);
    }

    public virtual EventCondition CreateBuildShipsAtStation(List<Ship.ShipBlueprint> shipBlueprints, Faction faction, Station station,
        bool visualize = false) {
        return new BuildShipsAtStation(shipBlueprints.Select(b => new Ship.ShipBlueprint(b, faction)).ToList(), faction, station,
            visualize);
    }

    public virtual EventCondition CreateSelectUnitCondition(Unit unitToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitToSelect, visualize });
    }

    public virtual EventCondition CreateSelectUnitsCondition(List<Unit> unitsToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitsToSelect, visualize });
    }

    public virtual EventCondition CreateSelectUnitsAmountCondition(List<Unit> unitsToSelect, int amount, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitsToSelect, amount, visualize });
    }

    public virtual EventCondition CreateUnselectUnitsCondition(List<Unit> unitsToUnselect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitsToUnselect, visualize });
    }

    public virtual EventCondition CreateSelectFleetCondition(Fleet fleetToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { fleetToSelect, visualize });
    }

    public virtual EventCondition CreateOpenObjectPanelCondition(BattleObject objectToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { objectToSelect, visualize });
    }

    public virtual EventCondition CreateOpenFactionPanelCondition(Faction factionToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { factionToSelect, visualize });
    }

    public virtual EventCondition CreateFollowUnitCondition(Unit unitToFollow, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitToFollow, visualize });
    }

    public virtual EventCondition CreatePanCondition(float distanceToPan) {
        return new PlaceholderCondition(new object[] { distanceToPan });
    }

    public virtual EventCondition CreateZoomCondition(float zoomTo) {
        return new PlaceholderCondition(new object[] { zoomTo });
    }

    public virtual EventCondition CreatePredicateCondition(Predicate<EventManager> predicate) {
        return new PredicateCondition(predicate);
    }

    public virtual EventCondition CreateLateCondition(Func<EventCondition> eventConditionFunction) {
        return new LateCondition(eventConditionFunction);
    }

    public virtual EventCondition CreateVictoryCondition() {
        return new VictoryCondition(battleManager);
    }

    public virtual void SetPlayerZoom(float zoom) { }

    public virtual void CenterPlayerCamera() { }

    public virtual void StartFollowingUnit(Unit unit) { }
}
