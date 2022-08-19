using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class FleetCommandAI : StationAI {
    public override void UpdateAI() {
        base.UpdateAI();
        UpdateFleetCommand();
    }

    private void UpdateFleetCommand() {
        Profiler.BeginSample("FleetCommandAI");
        if (cargoTime <= 0) {
            foreach (var ship in station.GetHanger().GetShips()) {
                if (ship.GetShipClass() == Ship.ShipClass.Transport && !ship.GetCargoBay().IsCargoEmptyOfType(CargoBay.CargoTypes.Metal)) {
                    station.GetCargoBay().LoadCargoFromBay(ship.GetCargoBay(), CargoBay.CargoTypes.Metal, 300);
                    cargoTime = 1;
                    break;
                }
            }
        }
        Profiler.EndSample();
    }

    public override void OnShipBuilt(Ship ship) {
        if (!ship.IsSpawned())
            return;
        if (ship.GetShipType() == Ship.ShipType.Transport) {
            foreach (var station in station.faction.GetAllFactionStations()) {
                if (station != null && station != this && station.IsSpawned() && station.stationType == Station.StationType.MiningStation) {
                    if (((MiningStation)station).GetMiningStationAI().transportShips.Count < 1) {
                        ((MiningStation)station).GetMiningStationAI().AddTransportShip(ship);
                        ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station), ShipAI.CommandAction.AddToEnd);
                        return;
                    }
                }
            }
            return;
        }
        if (ship.GetShipType() == Ship.ShipType.Construction) {
            MiningStation newMinningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(station.faction.factionIndex, Station.StationType.MiningStation, "MiningStation", station.GetPosition(), Random.Range(0, 360), false));
            ((ConstructionShip)ship).targetStationBlueprint = newMinningStation;
            ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, newMinningStation), ShipAI.CommandAction.AddToEnd);
            ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station), ShipAI.CommandAction.AddToEnd);
            station.faction.GetFleetCommand().GetConstructionBay().AddConstructionToBeginningQueue(station.faction.GetTransportBlueprint());
            newMinningStation.GetMiningStationAI().AddTransportShipToBuildQueue();
            return;
        }
        if (ship.GetShipType() == Ship.ShipType.Research) {
            ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Research, station.faction.GetClosestStar(station.GetPosition()), station),ShipAI.CommandAction.Replace);
        }
    }

    protected override void ManageStationRepair() {
        int repairAmmount = GetShipyard().GetRepairAmmount();
        if (station.GetHealth() < station.GetMaxHealth() / 2)
            repairAmmount = station.Repair(repairAmmount);
        for (int i = 0; i < station.GetHanger().GetShips().Count; i++) {
            Ship targetShip = station.GetHanger().GetShips()[i];
            if (targetShip.IsDammaged()) {
                repairAmmount = station.RepairUnit(targetShip, repairAmmount);
            }
        }
        if (station.IsDammaged())
            station.RepairUnit(station, repairAmmount);
        station.repairTime += station.repairSpeed;
    }

    public Shipyard GetShipyard() {
        return (Shipyard)station;
    }
}
