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

    public EventCondition CreateWaitEvent(float timeToWait) {
        return new WaitCondition(timeToWait);
    }

    public EventCondition CreateWaitUntilShipsIdle(List<Ship> shipsToIdle, bool visualize = false) {
        return new IdleShipsCondition(shipsToIdle, visualize);
    }

    public static Tuple<EventCondition, Action> ConditionalWait() {
        WaitTriggerCondition triggerCondition = new WaitTriggerCondition();
        return new Tuple<EventCondition, Action>(triggerCondition, triggerCondition.completer);
    }

    public EventCondition CreateDockShipAtUnit(Ship shipToDock, Station unitToDockAt, bool visualize = false) {
        return new DockShipsAtUnitCondition(shipToDock, unitToDockAt, visualize);
    }

    public EventCondition CreateDockShipsAtUnit(List<Ship> shipsToDock, Station unitToDockAt, bool visualize = false) {
        return new DockShipsAtUnitCondition(shipsToDock, unitToDockAt, visualize);
    }

    public EventCondition CreateCommandMoveShipToObject(Ship shipToMove, IObject objectToCommandMoveTo, bool visualize = false) {
        return new ShipCommandMoveToObjectsCondition(shipToMove, objectToCommandMoveTo, visualize);
    }

    public EventCondition CreateCommandMoveShipToObjects(Ship shipToMove, List<IObject> objectsToCommandToMoveTo,
        bool visualize = false) {
        return new ShipCommandMoveToObjectsCondition(shipToMove, objectsToCommandToMoveTo, visualize);
    }

    public EventCondition CreateCommandMoveShipsToObject(List<Ship> shipsToMove, IObject objectToCommandMoveTo, bool visualize = false) {
        return new ShipCommandMoveToObjectsCondition(shipsToMove, objectToCommandMoveTo, visualize);
    }

    public EventCondition CreateCommandMoveShipsToObjects(List<Ship> shipsToMove, List<IObject> objectsToCommandToMoveTo,
        bool visualize = false) {
        return new ShipCommandMoveToObjectsCondition(shipsToMove, objectsToCommandToMoveTo, visualize);
    }

    public EventCondition CreateCommandDockShipToUnit(Ship shipToMove, Station unitToDockAt, bool visualize = false) {
        return new ShipsCommandCondition(shipToMove, Command.CreateDockCommand(unitToDockAt), visualize);
    }

    public EventCondition CreateCommandShipToCollectGas(Ship shipToMove, GasCloud gasCloud = null, Station returnStation = null,
        bool visualize = false) {
        return new ShipsCommandCondition(shipToMove, Command.CreateCollectGasCommand(gasCloud, returnStation), visualize);
    }

    public EventCondition CreateBuildShipAtStation(Ship.ShipBlueprint shipBlueprint, Faction faction, Station station = null,
        bool visualize = false) {
        return new BuildShipsAtStation(shipBlueprint, faction, station, visualize);
    }

    public EventCondition CreateBuildShipsAtStation(List<Ship.ShipBlueprint> shipBlueprints, Faction faction, Station station,
        bool visualize = false) {
        return new BuildShipsAtStation(shipBlueprints, faction, station, visualize);
    }

    public EventCondition CreateSelectUnitEvent(Unit unitToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitToSelect, visualize });
    }

    public EventCondition CreateSelectUnitsEvent(HashSet<Unit> unitsToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitsToSelect, visualize });
    }

    public EventCondition CreateSelectUnitsAmountEvent(HashSet<Unit> unitsToSelect, int amount, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitsToSelect, amount, visualize });
    }

    public EventCondition CreateUnselectUnitsEvent(HashSet<Unit> unitsToUnselect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitsToUnselect, visualize });
    }

    public EventCondition CreateSelectFleetEvent(Fleet fleetToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { fleetToSelect, visualize });
    }

    public EventCondition CreateOpenObjectPanelEvent(BattleObject objectToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { objectToSelect, visualize });
    }

    public EventCondition CreateOpenFactionPanelEvent(Faction factionToSelect, bool visualize = false) {
        return new PlaceholderCondition(new object[] { factionToSelect, visualize });
    }

    public EventCondition CreateFollowUnitEvent(Unit unitToFollow, bool visualize = false) {
        return new PlaceholderCondition(new object[] { unitToFollow, visualize });
    }

    public EventCondition CreatePanEvent(float distanceToPan) {
        return new PlaceholderCondition(new object[] { distanceToPan });
    }

    public EventCondition CreateZoomEvent(float scrollTo) {
        return new PlaceholderCondition(new object[] { scrollTo });
    }

    public EventCondition CreatePredicateEvent(Predicate<EventManager> predicate) {
        return new PredicateCondition(predicate);
    }

    public EventCondition CreateLateConditionEvent(Func<EventCondition> eventConditionFunction) {
        return new LateCondition(eventConditionFunction);
    }

    public virtual void SetPlayerZoom(float zoom) { }

    public virtual void CenterPlayerCamera() { }

    public virtual void StartFollowingUnit(Unit unit) { }

}
