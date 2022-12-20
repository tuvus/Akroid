using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class MiningStationAI : StationAI {

    public List<Ship> transportShips;
    [SerializeField]int wantedTransports;

    public override void SetupStationAI(Station station) {
        base.SetupStationAI(station);
        transportShips = new List<Ship>(10);
        if (station.faction.GetFleetCommand() != null) {
            SetupWantedTrasports(station.faction.GetFleetCommand().GetPosition());
        } else {
            SetupWantedTrasports(station.faction.factionPosition);
        }
    }

    public void SetupWantedTrasports(Vector2 targetPosition) {
        float distance = Vector2.Distance(station.GetPosition(), targetPosition) * 2;
        float miningAmount = GetMiningStation().GetMiningAmmount() / GetMiningStation().GetMiningSpeed();
        float cargoPerTransport = 4800;
        float transportSpeed = 20;
        wantedTransports = Mathf.RoundToInt(miningAmount / (transportSpeed * cargoPerTransport / distance));
    }

    public override void UpdateAI(float deltaTime) {
        base.UpdateAI(deltaTime);
        UpdateMinningStation();
    }

    private void UpdateMinningStation() {
        if (GetMiningStation().activelyMinning) {
            if (cargoTime <= 0) {
                ManageMinningStationCargo();
            }
        } else if (!GetMiningStation().activelyMinning && transportShips.Count > 0) {
            for (int i = transportShips.Count - 1; i >= 0; i--) {
                transportShips[i].shipAI.AddUnitAICommand(new Command(Command.CommandType.Idle), ShipAI.CommandAction.Replace);
                transportShips.RemoveAt(i);
            }
        }
    }

    void ManageMinningStationCargo() {
        for (int i = 0; i < station.GetHanger().GetShips().Count; i++) {
            Ship ship = station.GetHanger().GetShips()[i];
            if (ship.GetShipType() == Ship.ShipType.Transport) {
                ship.GetCargoBay().LoadCargoFromBay(station.GetCargoBay(), CargoBay.CargoTypes.Metal, cargoAmmount);
                cargoTime += cargoSpeed;
                break;
            }
        }
    }

    public void AddTransportShip(Ship ship) {
        if (!GetMiningStation().activelyMinning)
            Debug.LogError("Trying to add to an inactive station");
        for (int i = 0; i < transportShips.Count; i++) {
            if (transportShips[i] == null) {
                transportShips.RemoveAt(i);
                i--;
            }
        }
        if (!transportShips.Contains(ship)) {
            transportShips.Add(ship);
            ship.shipAI.AddUnitAICommand(new Command(Command.CommandType.Transport, station, station.faction.GetFleetCommand()), ShipAI.CommandAction.Replace);
        }
    }

    public int? GetWantedTransportShips() {
        for (int i = 0; i < transportShips.Count; i++) {
            if (transportShips[i] == null) {
                transportShips.RemoveAt(i);
                i--;
            }
        }
        if (!station.IsBuilt() || !GetMiningStation().activelyMinning)
            return null;
        return wantedTransports - transportShips.Count;
    }

    public MiningStation GetMiningStation() {
        return (MiningStation)station;
    }
}