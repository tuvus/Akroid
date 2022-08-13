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
        if (waitTime <= 0) {
            waitTime = 1;
            ManageStationBuilding();
            ManageShipBuilding();
            if (station.faction.HasEnemy()) {
                if (station.enemyUnitsInRange.Count > 0) {
                    List<Ship> combatShips = station.GetHanger().GetAllCombatShips();
                    Vector2 position = station.enemyUnitsInRange[0].GetPosition();
                    for (int i = 0; i < combatShips.Count; i++) {
                        combatShips[i].shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.AttackMove, position), ShipAI.CommandAction.AddToEnd);
                        combatShips[i].shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station), ShipAI.CommandAction.AddToEnd);
                        waitTime += 1;
                    }
                } else {
                    List<Ship> combatShips = station.GetHanger().GetAllUndamagedCombatShips();
                    if (combatShips.Count > 0) {

                        int totalHealth = 0;
                        for (int i = 0; i < combatShips.Count; i++) {
                            totalHealth += combatShips[i].GetTotalHealth();
                        }
                        if (totalHealth > 3000 && combatShips.Count > 4) {
                            Station enemyStation = station.faction.GetClosestEnemyStation(station.GetPosition());
                            if (enemyStation != null) {
                                for (int i = 0; i < combatShips.Count; i++) {
                                    combatShips[i].shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.AttackMove, enemyStation.GetPosition() + new Vector2(Random.Range(-100, 100), Random.Range(-100, 100))), ShipAI.CommandAction.AddToEnd);
                                    combatShips[i].shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station), ShipAI.CommandAction.AddToEnd);
                                    waitTime += 1;
                                }
                            }
                        }
                    }
                }
            }
            Ship scienceShip = station.GetHanger().GetResearchShip();
            if (scienceShip != null && !scienceShip.IsDammaged()) {
                station.faction.AddScience(scienceShip.GetResearchEquiptment().DownloadData());
            }
        }
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

    void ManageShipBuilding() {
        if (GetFleetCommand().GetConstructionBay().HasOpenBays()) {
            float randomNumber = 0;
            if (station.faction.HasEnemy()) {
                randomNumber = Random.Range(0, 100);
            }
            if (randomNumber < 20) {
                GetFleetCommand().GetConstructionBay().AddConstructionToQueue(
new Ship.ShipBlueprint(Ship.ShipClass.Zarrack, "Science Ship", 21000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 5000 }));
            } else if (randomNumber < 50) {
                GetFleetCommand().GetConstructionBay().AddConstructionToQueue(
    new Ship.ShipBlueprint(Ship.ShipClass.Aria, "Aria", 2300,
    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 800 }));
            } else if (randomNumber < 80) {
                GetFleetCommand().GetConstructionBay().AddConstructionToQueue(
    new Ship.ShipBlueprint(Ship.ShipClass.Lancer, "Lancer", 5000,
    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 1800 }));
            } else {
                GetFleetCommand().GetConstructionBay().AddConstructionToQueue(
new Ship.ShipBlueprint(Ship.ShipClass.Aterna, "Aterna", 30000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 9000 }));
            }
        }
    }

    void ManageStationBuilding() {
        int transportQueueCount = GetFleetCommand().GetConstructionBay().GetNumberOfShipsOfClass(Ship.ShipClass.Transport);
        int stationBuilderQueueCount = GetFleetCommand().GetConstructionBay().GetNumberOfShipsOfClass(Ship.ShipClass.StationBuilder);
        bool wantTransport = station.faction.GetTotalWantedTransports() > station.faction.GetShipsOfType(Ship.ShipType.Transport) + transportQueueCount;
        bool wantNewStationBuilder = station.faction.GetAvailableAsteroidFieldsCount() > station.faction.GetShipsOfType(Ship.ShipType.Construction) + stationBuilderQueueCount;

        if ((GetFleetCommand().GetConstructionBay().HasOpenBays() && !station.faction.HasEnemy()) ||
            transportQueueCount == 0 && stationBuilderQueueCount == 0) {
            if (wantTransport) {
                GetFleetCommand().GetConstructionBay().AddConstructionToBeginningQueue(
new Ship.ShipBlueprint(Ship.ShipClass.Transport, "Transport", 1000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 1400 }));
            } else if (wantNewStationBuilder) {
                GetFleetCommand().GetConstructionBay().AddConstructionToBeginningQueue(
new Ship.ShipBlueprint(Ship.ShipClass.StationBuilder, "StationBuilder", 3000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 4000 }));
            }
        }
    }

    public void OnShipBuilt(Ship ship) {
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
        int repairAmmount = GetFleetCommand().repairAmmount;
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

    public FleetCommand GetFleetCommand() {
        return (FleetCommand)station;
    }
}
