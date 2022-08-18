using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationFactionAI : FactionAI {
    public Shipyard fleetCommand { get; private set; }

    public override void SetupFactionAI(Faction faction) {
        base.SetupFactionAI(faction);
        SetFleetCommand();
    }

    void SetFleetCommand() {
        for (int i = 0; i < faction.stations.Count; i++) {
            if (faction.stations[i].stationType == Station.StationType.Shipyard) {
                fleetCommand = (Shipyard)faction.stations[i];
                return;
            }
        }
    }

    public override void UpdateFactionAI() {
        base.UpdateFactionAI();
        if (fleetCommand != null) {
            ManageShipBuilding();
            ManageStationBuilding();
        }
    }

    void ManageShipBuilding() {
        if (fleetCommand.GetConstructionBay().HasOpenBays()) {
            float randomNumber = 0;
            if (faction.HasEnemy()) {
                randomNumber = Random.Range(0, 100);
            }
            if (randomNumber < 20) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(
new Ship.ShipBlueprint(Ship.ShipClass.Zarrack, "Science Ship", 21000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 5000 }));
            } else if (randomNumber < 50) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(
    new Ship.ShipBlueprint(Ship.ShipClass.Aria, "Aria", 2300,
    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 800 }));
            } else if (randomNumber < 80) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(
    new Ship.ShipBlueprint(Ship.ShipClass.Lancer, "Lancer", 5000,
    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 1800 }));
            } else {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(
new Ship.ShipBlueprint(Ship.ShipClass.Aterna, "Aterna", 30000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 9000 }));
            }
        }
    }

    void ManageStationBuilding() {
        int transportQueueCount = fleetCommand.GetConstructionBay().GetNumberOfShipsOfClass(Ship.ShipClass.Transport);
        int stationBuilderQueueCount = fleetCommand.GetConstructionBay().GetNumberOfShipsOfClass(Ship.ShipClass.StationBuilder);
        bool wantTransport = faction.GetTotalWantedTransports() > faction.GetShipsOfType(Ship.ShipType.Transport) + transportQueueCount;
        bool wantNewStationBuilder = fleetCommand.faction.GetAvailableAsteroidFieldsCount() > faction.GetShipsOfType(Ship.ShipType.Construction) + stationBuilderQueueCount;

        if ((fleetCommand.GetConstructionBay().HasOpenBays() && !faction.HasEnemy()) ||
            transportQueueCount == 0 && stationBuilderQueueCount == 0) {
            if (wantTransport) {
                fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(
new Ship.ShipBlueprint(Ship.ShipClass.Transport, "Transport", 1000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 1400 }));
            } else if (wantNewStationBuilder) {
                fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(
new Ship.ShipBlueprint(Ship.ShipClass.StationBuilder, "StationBuilder", 3000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 4000 }));
            }
        }
    }
}
