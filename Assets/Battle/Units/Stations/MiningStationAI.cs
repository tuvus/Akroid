using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MiningStationAI : StationAI {
    [SerializeField] private int wantedTransports;

    public MiningStationAI(Station station) : base(station) {
        wantedTransports = 0;
    }

    public void SetupMiningStation() {
        if (station.faction.GetFleetCommand() != null) {
            SetupWantedTransports(station.faction.GetFleetCommand().GetPosition());
        } else if (station.faction.stations.Any(s =>
            s.GetStationType() == Station.StationType.Shipyard ||
            s.GetStationType() == Station.StationType.FleetCommand)) {
            SetupWantedTransports(station.faction.stations
                .First(s => s.GetStationType() == Station.StationType.Shipyard ||
                    s.GetStationType() == Station.StationType.FleetCommand)
                .GetPosition());
        } else {
            SetupWantedTransports(station.faction.GetPosition());
        }
    }

    public void SetupWantedTransports(Vector2 targetPosition) {
        float distance = Vector2.Distance(station.GetPosition(), targetPosition) * 2;
        float miningAmount = GetMiningStation().GetMiningAmount() / GetMiningStation().GetMiningSpeed();
        float cargoPerTransport = 4800;
        float transportSpeed = 17.25f;
        wantedTransports = Mathf.CeilToInt(miningAmount / (transportSpeed * cargoPerTransport / distance));
    }

    public int? GetWantedTransportShips() {
        if (!station.IsBuilt() || !GetMiningStation().activelyMining)
            return null;
        return wantedTransports;
    }

    public MiningStation GetMiningStation() {
        return (MiningStation)station;
    }
}
