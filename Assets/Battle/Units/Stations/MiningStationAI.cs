using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class MiningStationAI : StationAI {
    public List<Ship> transportShips;
    [SerializeField] private int wantedTransports;

    public MiningStationAI(Station station) : base(station) {
        transportShips = new List<Ship>(10);
        wantedTransports = 0;
    }

    public void SetupMiningStation() {
        if (station.faction.GetFleetCommand() != null) {
            SetupWantedTrasports(station.faction.GetFleetCommand().GetPosition());
        } else {
            SetupWantedTrasports(station.faction.GetPosition());
        }
    }

    public void SetupWantedTrasports(Vector2 targetPosition) {
        float distance = Vector2.Distance(station.GetPosition(), targetPosition) * 2;
        float miningAmount = GetMiningStation().GetMiningAmount() / GetMiningStation().GetMiningSpeed();
        float cargoPerTransport = 4800;
        float transportSpeed = 20;
        wantedTransports = Mathf.CeilToInt(miningAmount / (transportSpeed * cargoPerTransport / distance));
    }

    public override void UpdateAI(float deltaTime) {
        base.UpdateAI(deltaTime);
        UpdateMiningStation();
    }

    private void UpdateMiningStation() {
        Profiler.BeginSample("UpdateMiningStationAI");
        if (!GetMiningStation().activelyMining && !GetMiningStation().activelyMining && transportShips.Count > 0) {
            for (int i = transportShips.Count - 1; i >= 0; i--) {
                transportShips[i].shipAI.AddUnitAICommand(Command.CreateIdleCommand(), Command.CommandAction.Replace);
                transportShips.RemoveAt(i);
            }
        }

        Profiler.EndSample();
    }

    public void AddTransportShip(Ship ship) {
        if (!GetMiningStation().activelyMining)
            Debug.LogError("Trying to add to an inactive station");
        for (int i = 0; i < transportShips.Count; i++) {
            if (transportShips[i] == null) {
                transportShips.RemoveAt(i);
                i--;
            }
        }

        if (!transportShips.Contains(ship)) {
            transportShips.Add(ship);
        }

        ship.shipAI.AddUnitAICommand(Command.CreateTransportCommand(station, station.faction.GetFleetCommand(), CargoBay.CargoTypes.Metal),
            Command.CommandAction.Replace);
    }

    public int? GetWantedTransportShips() {
        for (int i = transportShips.Count - 1; i >= 0; i--) {
            if (transportShips[i] == null) {
                transportShips.RemoveAt(i);
            }
        }

        if (!station.IsBuilt() || !GetMiningStation().activelyMining)
            return null;
        return wantedTransports - transportShips.Count;
    }

    public MiningStation GetMiningStation() {
        return (MiningStation)station;
    }
}
