using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class SimulationFactionAI : FactionAI {
    public Shipyard fleetCommand { get; private set; }
    float updateTime;
    public bool autoBuildShips;

    public override void SetupFactionAI(Faction faction) {
        base.SetupFactionAI(faction);
        autoBuildShips = true;
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
                if (autoBuildShips) {
                    ManageStationBuilding();
                    ManageShipBuilding();
                }
                updateTime += .2f;
            }
        }
        Profiler.EndSample();
    }

    void ManageFleets() {
        for (int i = 0; i < faction.fleets.Count; i++) {
            Fleet fleet = faction.fleets[i];
            if (fleet.IsFleetIdle() || !fleet.HasNearbyEnemyCombatShip()) {
                bool merged = false;
                for (int f = i + 1; f < faction.fleets.Count; f++) {
                    if ((faction.fleets[f].IsFleetIdle() || !faction.fleets[f].HasNearbyEnemyCombatShip()) && Vector2.Distance(fleet.GetPosition(), faction.fleets[f].GetPosition()) < 1000) {
                        fleet.MergeIntoFleet(faction.fleets[f]);
                        merged = true;
                        break;
                    }
                }
                if (merged)
                    continue;
            } 
            if (fleet.IsFleetIdle() && (faction.HasEnemy() || !fleet.IsDockedWithStation(fleetCommand))) {
                if (fleet.GetAllShips().Count <= 2 || fleet.GetTotalFleetHealth() <= 1000) {
                    List<Ship> shipsInFleet = new List<Ship>(fleet.GetAllShips());
                    fleet.DisbandFleet();
                    foreach (Ship ship in shipsInFleet) {
                        ship.shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand));
                    }
                    i--;
                } else {
                    Vector2 fleetPosition = fleet.GetPosition();
                    Station targetStation = faction.GetClosestEnemyStation(fleetPosition);
                    if (targetStation != null) {
                        fleet.FleetAI.AddFormationCommand(fleetPosition, Calculator.GetAngleOutOfTwoPositions(fleetPosition, targetStation.GetPosition()), Command.CommandAction.Replace);
                        fleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(targetStation));
                    } else {
                        Unit targetUnit = faction.GetClosestEnemyUnit(fleetPosition);
                        if (targetUnit != null) {
                            fleet.FleetAI.AddFormationCommand(fleetPosition, Calculator.GetAngleOutOfTwoPositions(fleetPosition, targetUnit.GetPosition()), Command.CommandAction.Replace);
                            fleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(targetUnit));
                        } else {
                            fleet.FleetAI.AddFormationCommand(fleetPosition, Calculator.GetAngleOutOfTwoPositions(fleetPosition, fleetCommand.GetPosition()), Command.CommandAction.Replace);
                            fleet.FleetAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand));
                        }
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
                    MiningStation newMinningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(faction.factionIndex, Station.StationType.MiningStation, "MiningStation", faction.GetPosition(), Random.Range(0, 360), false));
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
                            Fleet fleet = faction.CreateNewFleet("AttackFleet", combatShips);
                            if (fleet != null) {
                                fleet.FleetAI.AddFormationTowardsPositionCommand(enemyStation.GetPosition(), fleetCommand.GetSize() * 4);
                                fleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(enemyStation), Command.CommandAction.AddToEnd);
                            }
                        } else {
                            Unit targetUnit = faction.GetClosestEnemyUnit(fleetCommand.GetPosition());
                            if (targetUnit != null) {
                                Fleet fleet = faction.CreateNewFleet("AttackFleet", combatShips);
                                fleet.FleetAI.AddFormationTowardsPositionCommand(targetUnit.GetPosition(), fleetCommand.GetSize() * 4);
                                fleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(targetUnit));
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
                fleetCommand.GetConstructionBay().AddConstructionToQueue(BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Zarrack).CreateShipBlueprint(faction.factionIndex, "Science Ship"));
            } else if (randomNumber < 50) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Aria).CreateShipBlueprint(faction.factionIndex));
            } else if (randomNumber < 80) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Lancer).CreateShipBlueprint(faction.factionIndex));
            } else {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Aterna).CreateShipBlueprint(faction.factionIndex));
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
                fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Transport).CreateShipBlueprint(faction.factionIndex));
            } else if (wantNewStationBuilder) {
                fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.StationBuilder).CreateShipBlueprint(faction.factionIndex));
            }
        }
    }
}