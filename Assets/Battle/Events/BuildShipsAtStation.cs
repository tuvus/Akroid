using System.Collections.Generic;

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
            station.stationAI.OnBuildShip += s => shipBlueprintsToBuild
                .RemoveAll(b => b.shipScriptableObject == s.shipScriptableObject && b.faction == s.faction);
        }

        return shipBlueprintsToBuild.Count == 0;
    }
}
