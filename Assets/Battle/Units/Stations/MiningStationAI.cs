using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningStationAI : StationAI {

    public List<Ship> transportShips;

    public override void SetupStationAI(Station station) {
        base.SetupStationAI(station);
        transportShips = new List<Ship>(10);
    }

    public override void UpdateAI() {
        base.UpdateAI();
        UpdateMinningStation();
    }

    private void UpdateMinningStation() {
        if (GetMiningStation().activelyMinning) {
            if (waitTime <= 0) {
                ManageMinningStationTransports();
                waitTime += 4;
            }
            if (cargoTime <= 0) {
                ManageMinningStationCargo();
            }
        } else if (!GetMiningStation().activelyMinning && transportShips.Count > 0) {
            MiningStation closestMinningStation = GetMiningStation().faction.GetClosestMinningStationWantingTransport(GetMiningStation().GetPosition());
            if (closestMinningStation != null) {
                for (int i = transportShips.Count - 1; i >= 0; i--) {
                    transportShips[i].shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, closestMinningStation), ShipAI.CommandAction.AddToEnd);
                    closestMinningStation.GetMiningStationAI().AddTransportShip(transportShips[i]);
                    transportShips.RemoveAt(i);
                }
            }
        }
    }
    void ManageMinningStationCargo() {
        for (int i = 0; i < transportShips.Count; i++) {
            if (transportShips[i] == null) {
                transportShips.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < station.GetHanger().GetShips().Count; i++) {
            Ship ship = station.GetHanger().GetShips()[i];
            if (ship.GetShipType() == Ship.ShipType.Transport) {
                ship.GetCargoBay().LoadCargoFromBay(station.GetCargoBay(), CargoBay.CargoTypes.Metal, 600);
                if (ship.GetCargoBay().IsCargoFullOfType(CargoBay.CargoTypes.Metal) && station.faction.GetFleetCommand() != null) {
                    //ship.UndockShip(station.faction.GetFleetCommand().GetPosition());
                    ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station.faction.GetFleetCommand()), ShipAI.CommandAction.AddToEnd);
                    ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Idle), ShipAI.CommandAction.AddToEnd);
                    ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station), ShipAI.CommandAction.AddToEnd);
                    if (!transportShips.Contains(ship))
                        transportShips.Add(ship);
                    i--;
                }
                cargoTime = 1;
                break;
            }
        }
    }

    void ManageMinningStationTransports() {
        //if (transportShips.Count < GetWantedTransportShips()) {
        //    AddTransportShipToBuildQueue();
        //}
    }

    public void AddTransportShip(Ship ship) {
        transportShips.Add(ship);
    }

    public int GetWantedTransportShips() {
        if (!station.IsBuilt() || !GetMiningStation().activelyMinning)
            return 0;
        return 2 - transportShips.Count;
    }

    public MiningStation GetMiningStation() {
        return (MiningStation)station;
    }

    public void AddTransportShipToBuildQueue() {
        station.faction.GetFleetCommand().GetConstructionBay().AddConstructionToBeginningQueue(station.faction.GetTransportBlueprint());
    }
}
