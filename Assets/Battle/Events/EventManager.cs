using System;
using System.Collections.Generic;
using System.Linq;

public class EventManager {
    public HashSet<Tuple<EventCondition, Action>> ActiveEvents { get; private set; }

    public EventManager() {
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

    public virtual EventCondition CreateWaitEvent(float timeToWait) {
        return new WaitCondition(timeToWait);
    }

    public virtual EventCondition CreateWaitUntilShipsIdle(List<Ship> shipsToIdle, bool visualize = false) {
        return new IdleShipsCondition(shipsToIdle, visualize);
    }

    public static Tuple<EventCondition, Action> ConditionalWait() {
        WaitTriggerCondition triggerCondition = new WaitTriggerCondition();
        return new Tuple<EventCondition, Action>(triggerCondition, triggerCondition.completer);
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

    public virtual EventCondition CreateCommandMoveShipsToObject(List<Ship> shipsToMove, IObject objectToCommandMoveTo, bool visualize = false) {
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
        return new BuildShipsAtStation(shipBlueprint, faction, station, visualize);
    }

    public virtual EventCondition CreateBuildShipsAtStation(List<Ship.ShipBlueprint> shipBlueprints, Faction faction, Station station,
        bool visualize = false) {
        return new BuildShipsAtStation(shipBlueprints, faction, station, visualize);
    }

    public virtual EventCondition CreateSelectUnitEvent(Unit unitToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitToSelect, visualize });
    }

    public virtual EventCondition CreateSelectUnitsEvent(List<Unit> unitsToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitsToSelect, visualize });
    }

    public virtual EventCondition CreateSelectUnitsAmountEvent(List<Unit> unitsToSelect, int amount, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitsToSelect, amount, visualize });
    }

    public virtual EventCondition CreateUnselectUnitsEvent(List<Unit> unitsToUnselect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitsToUnselect, visualize });
    }

    public virtual EventCondition CreateSelectFleetEvent(Fleet fleetToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { fleetToSelect, visualize });
    }

    public virtual EventCondition CreateOpenObjectPanelEvent(BattleObject objectToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { objectToSelect, visualize });
    }

    public virtual EventCondition CreateOpenFactionPanelEvent(Faction factionToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { factionToSelect, visualize });
    }

    public virtual EventCondition CreateFollowUnitEvent(Unit unitToFollow, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitToFollow, visualize });
    }

    public virtual EventCondition CreatePanEvent(float distanceToPan) {
        return new PlaceholderCondition(new object[] { distanceToPan });
    }

    public virtual EventCondition CreateZoomEvent(float zoomTo) {
        return new PlaceholderCondition(new object[] { zoomTo });
    }

    public virtual EventCondition CreatePredicateEvent(Predicate<EventManager> predicate) {
        return new PredicateCondition(predicate);
    }

    public virtual EventCondition CreateLateConditionEvent(Func<EventCondition> eventConditionFunction) {
        return new LateCondition(eventConditionFunction);
    }

    public virtual void SetPlayerZoom(float zoom) { }

    public virtual void CenterPlayerCamera() { }

    public virtual void StartFollowingUnit(Unit unit) { }

}
