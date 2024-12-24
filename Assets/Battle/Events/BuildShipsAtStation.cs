using System.Collections.Generic;
using System.Linq;

public class BuildShipsAtStation : EventCondition {
    public List<Ship.ShipBlueprint> shipBlueprintsToBuild { get; private set; }
    public Faction faction { get; private set; }
    public Station station { get; private set; }
    public bool subscribed { get; private set; }

    public BuildShipsAtStation(List<Ship.ShipBlueprint> shipBlueprintsToBuild, Faction faction, Station station, bool visualize = false) :
        base(ConditionType.BuildShipsAtStation, visualize) {
        this.shipBlueprintsToBuild = shipBlueprintsToBuild;
        this.faction = faction;
        this.station = station;
        subscribed = false;
    }

    public BuildShipsAtStation(Ship.ShipBlueprint shipBlueprintToBuild, Faction faction, Station station, bool visualize = false) :
        this(new List<Ship.ShipBlueprint>() { shipBlueprintToBuild }, faction, station, visualize) { }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        if (!subscribed) {
            station.stationAI.OnBuildShip += OnBuildShip;
            subscribed = true;
        }

        return shipBlueprintsToBuild.Count == 0;
    }

    private void OnBuildShip(Ship ship) {
        shipBlueprintsToBuild.Remove(
            shipBlueprintsToBuild.FirstOrDefault(b => b.shipScriptableObject == ship.shipScriptableObject && b.faction == ship.faction));
        if (shipBlueprintsToBuild.Count == 0) station.stationAI.OnBuildShip -= OnBuildShip;
    }
}
