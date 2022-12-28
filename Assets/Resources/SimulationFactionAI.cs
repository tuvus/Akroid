using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class SimulationFactionAI : FactionAI {
    public Shipyard fleetCommand { get; private set; }
    float updateTime;

    public override void SetupFactionAI(Faction faction) {
        base.SetupFactionAI(faction);
    }

    public override void GenerateFactionAI() {
    }

    public override void OnStationBuilt(Station station) {
        if (station.stationType == Station.StationType.FleetCommand) {
            fleetCommand = (Shipyard)station;
        }
    }

    public override void UpdateFactionAI(float deltaTime) {
        Profiler.BeginSample("FactionAI");
        base.UpdateFactionAI(deltaTime);
        if (fleetCommand != null) {
            updateTime -= deltaTime;
            if (updateTime <= 0) {
                ManageFleets();
                ManageIdleShips();
                ManageDockedShips();
                ManageStationBuilding();
                ManageShipBuilding();
                updateTime += .2f;
            }
        }
        Profiler.EndSample();
    }

    void ManageFleets() {
        foreach (FleetAI fleet in faction.fleets) {
            if (fleet.IsFleetIdle()) {
                if (fleet.GetAllShips().Count <= 2 || fleet.GetTotalFleetHealth() <= 1000) {
                    foreach (Ship ship in fleet.GetAllShips()) {
                        ship.shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand));
                    }
                    fleet.DisbandFleet();
                } else {
                    Vector2 fleetPosition = fleet.GetFleetCenter();
                    Station targetStation = faction.GetClosestEnemyStation(fleetPosition);
                    if (targetStation != null) {
                        fleet.SetFormation(fleetPosition, Calculator.GetAngleOutOfTwoPositions(fleetPosition, targetStation.GetPosition()), Command.CommandAction.Replace);
                        fleet.AddUnitAICommand(Command.CreateAttackMoveCommand(targetStation));
                    } else {
                        fleet.SetFormation(fleetPosition, Calculator.GetAngleOutOfTwoPositions(fleetPosition, fleetCommand.GetPosition()), Command.CommandAction.Replace);
                        fleet.AddUnitAICommand(Command.CreateDockCommand(fleetCommand));
                    }
                }
            }
        }
    }

    void ManageIdleShips() {
        for (int i = 0; i < idleShips.Count; i++) {
            if (idleShips[i].IsIdle()) {
                if (idleShips[i].IsCombatShip()) {
                    if (idleShips[i].dockedStation == null) {
                        idleShips[i].shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand), Command.CommandAction.Replace);
                    }
                } else if (idleShips[i].IsTransportShip()) {
                    Station minningStation = faction.GetClosestMinningStationWantingTransport(idleShips[i].GetPosition());
                    if (minningStation != null) {
                        ((MiningStationAI)minningStation.stationAI).AddTransportShip(idleShips[i]);
                    } else if (idleShips[i].dockedStation != fleetCommand) {
                        idleShips[i].shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand), Command.CommandAction.Replace);
                    }
                } else if (idleShips[i].IsScienceShip()) {
                    idleShips[i].shipAI.AddUnitAICommand(Command.CreateResearchCommand(faction.GetClosestStar(idleShips[i].GetPosition()), fleetCommand), Command.CommandAction.Replace);
                } else if (idleShips[i].IsConstructionShip()) {
                    MiningStation newMinningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(faction.factionIndex, Station.StationType.MiningStation, "MiningStation", faction.factionPosition, Random.Range(0, 360), false));
                    ((ConstructionShip)idleShips[i]).targetStationBlueprint = newMinningStation;
                    idleShips[i].shipAI.AddUnitAICommand(Command.CreateMoveCommand(newMinningStation.GetPosition()), Command.CommandAction.AddToEnd);
                    return;
                }
            }
        }
    }

    void ManageDockedShips() {
        if (faction.HasEnemy()) {
            if (fleetCommand.enemyUnitsInRange.Count > 0) {
                List<Ship> combatShips = fleetCommand.GetHanger().GetAllCombatShips();
                Vector2 position = fleetCommand.enemyUnitsInRange[0].GetPosition();
                for (int i = 0; i < combatShips.Count; i++) {
                    combatShips[i].shipAI.AddUnitAICommand(Command.CreateAttackMoveCommand(position), Command.CommandAction.AddToEnd);
                    combatShips[i].shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand), Command.CommandAction.AddToEnd);
                }
            } else {
                List<Ship> combatShips = fleetCommand.GetHanger().GetAllUndamagedCombatShips();
                if (combatShips.Count > 4) {
                    int totalHealth = 0;
                    for (int i = 0; i < combatShips.Count; i++) {
                        totalHealth += combatShips[i].GetTotalHealth();
                    }
                    if (totalHealth > 3000) {
                        Station enemyStation = faction.GetClosestEnemyStation(fleetCommand.GetPosition());
                        if (enemyStation != null) {
                            FleetAI fleet = faction.CreateNewFleet("AttackFleet", combatShips);
                            if (fleet != null) {
                                fleet.SetFormation(Vector2.MoveTowards(fleetCommand.GetPosition(), enemyStation.GetPosition(), fleetCommand.GetSize() * 3),Calculator.GetAngleOutOfTwoPositions(fleetCommand.GetPosition(),enemyStation.GetPosition()));
                                fleet.AddUnitAICommand(Command.CreateAttackMoveCommand(enemyStation), Command.CommandAction.AddToEnd);
                            }
                        }
                    }
                }
            }
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
new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Zarrack, "Science Ship", 21000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 5000 }));
            } else if (randomNumber < 50) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(
    new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Aria, "Aria", 2300,
    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 800 }));
            } else if (randomNumber < 80) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(
    new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Lancer, "Lancer", 5000,
    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 1800 }));
            } else {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(
new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Aterna, "Aterna", 30000,
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
new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Transport, "Transport", 1000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 1400 }));
            } else if (wantNewStationBuilder) {
                fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(
new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.StationBuilder, "StationBuilder", 3000,
new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 4000 }));
            }
        }
    }
}
