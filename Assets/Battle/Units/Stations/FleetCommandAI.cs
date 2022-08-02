using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleetCommandAI : StationAI {
    public override void UpdateAI() {
        base.UpdateAI();
        UpdateFleetCommand();
    }

    private void UpdateFleetCommand() {
        if (waitTime <= 0) {
            waitTime = 1;
            ManageStationBuilding();
            ManageShipBuilding();
            Ship combatShip = station.GetHanger().GetCombatShip();
            int count = 1;
            while (combatShip != null) {
                if (!combatShip.IsDammaged()) {
                    Station enemyStation = station.faction.GetClosestEnemyStation(station.GetPosition());
                    if (enemyStation != null) {
                        combatShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.AttackMove, enemyStation.GetPosition() + new Vector2(Random.Range(-100, 100), Random.Range(-100, 100))), ShipAI.CommandAction.AddToEnd);
                        combatShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station), ShipAI.CommandAction.AddToEnd);
                    }
                }
                combatShip = station.GetHanger().GetCombatShip(count);
                count++;
            }
            Ship scienceShip = station.GetHanger().GetResearchShip();
            if (scienceShip != null && !scienceShip.IsDammaged()) {
                station.faction.AddScience(scienceShip.GetResearchEquiptment().DownloadData());
                scienceShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Reserch, station.faction.GetClosestStar(station.GetPosition())), ShipAI.CommandAction.AddToEnd);
                scienceShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station), ShipAI.CommandAction.AddToEnd);
            }
        }
        if (cargoTime <= 0) {
            foreach (var ship in station.GetHanger().GetShips()) {
                if (ship.GetShipClass() == Ship.ShipClass.Transport && !ship.GetCargoBay().IsCargoEmptyOfType(CargoBay.CargoTypes.Metal)) {
                    station.GetCargoBay().LoadCargoFromBay(ship.GetCargoBay(), CargoBay.CargoTypes.Metal, 300);
                    if (ship.GetCargoBay().IsCargoEmptyOfType(CargoBay.CargoTypes.Metal) && !ship.IsDammaged())
                        ship.shipAI.NextCommand();
                    cargoTime = 1;
                    break;
                }
            }
        }

    }

    void ManageShipBuilding() {
        if (GetFleetCommand().GetConstructionBay().buildQueue.Count < GetFleetCommand().GetConstructionBay().constructionBays && station.faction.HasEnemy()) {
            int randomNumber = Random.Range(0, 100);
            if (randomNumber < 30) {
                GetFleetCommand().GetConstructionBay().AddConstructionToQueue(
    new Ship.ShipBlueprint(Ship.ShipClass.Aria, "Aria", 2300,
    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 800 }));
            } else if (randomNumber < 75) {
                GetFleetCommand().GetConstructionBay().AddConstructionToQueue(
    new Ship.ShipBlueprint(Ship.ShipClass.Lancer, "Lancer", 5000,
    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 1800 }));
            } else if (randomNumber < 90) {
                GetFleetCommand().GetConstructionBay().AddConstructionToQueue(
new Ship.ShipBlueprint(Ship.ShipClass.Aterna, "Aterna", 30000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 9000 }));
            } else {
                GetFleetCommand().GetConstructionBay().AddConstructionToQueue(
new Ship.ShipBlueprint(Ship.ShipClass.Zarrack, "Science Ship", 21000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 5000 }));
            }
        }
    }

    void ManageStationBuilding() {
        if (station.faction.GetAvailableAsteroidFieldsCount() > 0 && GetFleetCommand().GetConstructionBay().GetNumberOfShipsOfClass(Ship.ShipClass.StationBuilder) < 1) {
            GetFleetCommand().GetConstructionBay().AddConstructionToQueue(
new Ship.ShipBlueprint(Ship.ShipClass.StationBuilder, "StationBuilder", 3000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 5000 }));
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
            ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, newMinningStation), ShipAI.CommandAction.AddToEnd);
            ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station), ShipAI.CommandAction.AddToEnd);
            station.faction.GetFleetCommand().GetConstructionBay().AddConstructionToBeginningQueue(station.faction.GetTransportBlueprint());
            newMinningStation.GetMiningStationAI().AddTransportShipToBuildQueue();
            return;
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
