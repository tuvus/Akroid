using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public class SimulationFactionAI : FactionAI {
    public Shipyard fleetCommand { get; private set; }
    public List<Fleet> defenceFleets;
    static int wantedDefenceFleets = 1;
    public List<Fleet> attackFleets;
    public List<ShipGroup> newThreats;
    static float threatDistance = 1000;
    public List<ShipGroup> threats;
    float updateTime;
    public bool autoBuildShips;

    public override void SetupFactionAI(Faction faction) {
        base.SetupFactionAI(faction);
        autoBuildShips = true;
        updateTime = Random.Range(0, 0.2f);
        defenceFleets = new List<Fleet>();
        attackFleets = new List<Fleet>();
        newThreats = new List<ShipGroup>();
        threats = new List<ShipGroup>();
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

    void ManageThreats() {
        float farthestStationDistance = 0;
        for (int i = 0; i < faction.stations.Count; i++) {
            float stationDistance = Vector2.Distance(faction.stations[i].GetPosition(), faction.GetPosition());
            farthestStationDistance = math.max(farthestStationDistance, stationDistance);
        }
        for (int i = 0; i < faction.closeEnemyUnits.Count; i++) {
            if (faction.closeEnemyUnitsDistance[i] > farthestStationDistance * 1.4f)
                break;

        }
    }

    void ManageFleets() {
        for (int i = 0; i < defenceFleets.Count; i++) {
            if (defenceFleets[i] == null)
                defenceFleets.RemoveAt(i);
        }
        for (int i = 0; i < attackFleets.Count; i++) {
            if (attackFleets[i] == null)
                attackFleets.RemoveAt(i);
        }
        for (int i = newThreats.Count - 1; i >= 0; i--) {
            List<Ship> targetShips = newThreats[i].GetShips();
            Fleet closestFleet = null;
            float distanceToThreat = 0;
            for (int f = 0; f < defenceFleets.Count; f++) {
                float tempDistance = Vector2.Distance(newThreats[i].GetPosition(), defenceFleets[f].GetPosition());
                if (tempDistance < distanceToThreat || closestFleet == null) {
                    closestFleet = defenceFleets[f];
                    distanceToThreat = tempDistance;
                }
            }
            if (closestFleet == null)
                break;
            if (closestFleet.FleetAI.commands[0].commandType == Command.CommandType.AttackMove) {
                Fleet newThreatFleet = null;
                for (int j = 0; j < targetShips.Count; j++) {
                    if (targetShips[j].fleet != null) {
                        newThreatFleet = targetShips[j].fleet;
                        break;
                    }
                }
                if (newThreatFleet != null) {
                    closestFleet.FleetAI.AddUnitAICommand(Command.CreateAttackFleetCommand(newThreatFleet), Command.CommandAction.Replace);
                } else {
                    closestFleet.FleetAI.ClearCommands();
                    for (int j = 0; j < newThreats[i].GetShips().Count; j++) {
                        closestFleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(newThreats[i].GetShips()[j]));
                    }
                }
                newThreats[i].sentFleets.Add(closestFleet);
                if (newThreats[i].IsSentFleetsStronger()) {
                    threats.Add(newThreats[i]);
                    newThreats.RemoveAt(i);
                }
            }
        }
        for (int i = 0; i < defenceFleets.Count; i++) {
            Fleet fleet = defenceFleets[i];
            if (fleet.IsFleetIdle()) {
                Vector2 randomTargetPosition = faction.GetAveragePosition() + Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0, 360), Random.Range(100, faction.GetSize()));
                fleet.FleetAI.AddUnitAICommand(Command.CreateFormationCommand(fleet.GetPosition(), Calculator.GetAngleOutOfTwoPositions(fleet.GetPosition(), randomTargetPosition)), Command.CommandAction.Replace);
                fleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(randomTargetPosition), Command.CommandAction.AddToEnd);
            }
        }
        for (int i = 0; i < attackFleets.Count; i++) {
            Fleet fleet = attackFleets[i];
            if (fleet.IsFleetIdle() || !fleet.HasNearbyEnemyCombatShip()) {
                bool merged = false;
                for (int f = i + 1; f < attackFleets.Count; f++) {
                    if ((attackFleets[f].IsFleetIdle() || !attackFleets[f].HasNearbyEnemyCombatShip()) && Vector2.Distance(fleet.GetPosition(), attackFleets[f].GetPosition()) < 1000) {
                        fleet.MergeIntoFleet(attackFleets[f]);
                        merged = true;
                        break;
                    }
                }
                if (merged)
                    continue;
            }
            if (fleet.IsFleetIdle() && (faction.HasEnemy() || !fleet.IsDockedWithStation(fleetCommand))) {
                if (fleet.GetShips().Count <= 2 || fleet.GetTotalFleetHealth() <= 1000) {
                    List<Ship> shipsInFleet = new List<Ship>(fleet.GetShips());
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
            if (idleShips[i].IsIdle() && idleShips[i].fleet == null) {
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
                        if (defenceFleets.Count < wantedDefenceFleets) {
                            Fleet fleet = faction.CreateNewFleet("DefenceFleet" + (int)(defenceFleets.Count + 1), combatShips);
                            defenceFleets.Add(fleet);
                            fleet.FleetAI.AddFormationTowardsPositionCommand(faction.GetAveragePosition(), fleetCommand.GetSize() * 4);
                        } else {
                            Station enemyStation = faction.GetClosestEnemyStation(fleetCommand.GetPosition());
                            if (enemyStation != null) {
                                Fleet fleet = faction.CreateNewFleet("AttackFleet", combatShips);
                                attackFleets.Add(fleet);
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