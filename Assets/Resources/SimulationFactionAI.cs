using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public class SimulationFactionAI : FactionAI {
    public Shipyard fleetCommand { get; private set; }
    public List<Fleet> defenseFleets;
    static int wantedDefenseFleets = 1;
    public List<Fleet> attackFleets;
    public List<Fleet> threats;
    static float threatDistance = 1000;
    float updateTime;
    public bool autoCommandFleets;
    public bool autoBuildShips;

    public override void SetupFactionAI(Faction faction) {
        base.SetupFactionAI(faction);
        autoBuildShips = true;
        autoCommandFleets = true;
        updateTime = Random.Range(0, 0.2f);
        defenseFleets = new List<Fleet>();
        attackFleets = new List<Fleet>();
        threats = new List<Fleet>();
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
                if (autoCommandFleets && defenseFleets.Count + attackFleets.Count > 0) {
                    ManageThreats();
                    ManageFleets();
                }
                ManageIdleShips();
                if (autoCommandFleets) {
                    ManageDockedShips();
                }
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
        threats.Clear();
        for (int i = 0; i < faction.closeEnemyGroups.Count; i++) {
            if (faction.closeEnemyGroupsDistance[i] > farthestStationDistance * 1.4f)
                break;
            if (!faction.closeEnemyGroups[i].IsFleet())
                continue;
            Fleet targetFleet = (Fleet)faction.closeEnemyGroups[i];
            if (targetFleet.IsSentFleetsStronger(faction))
                return;
            threats.Add(targetFleet);
        }
    }

    void ManageFleets() {
        for (int i = defenseFleets.Count - 1; i >= 0; i--) {
            if (defenseFleets[i] == null)
                defenseFleets.RemoveAt(i);
        }
        for (int i = attackFleets.Count - 1; i >= 0; i--) {
            if (attackFleets[i] == null)
                attackFleets.RemoveAt(i);
        }
        for (int i = defenseFleets.Count - 1; i >= 0; i--) {
            Fleet defenseFleet = defenseFleets[i];
            List<Command> commands = defenseFleet.FleetAI.commands;
            if ((commands.Count > 0 && commands[0].commandType == Command.CommandType.AttackFleet)
                || (commands.Count > 1 && commands[0].commandType == Command.CommandType.FormationLocation && commands[1].commandType == Command.CommandType.AttackFleet))
                continue;
            Fleet threat = null;
            float distanceToThreat = 0;
            for (int f = 0; f < threats.Count; f++) {
                float tempDistance = Vector2.Distance(threats[f].GetPosition(), defenseFleet.GetPosition());
                if (tempDistance < distanceToThreat || threat == null) {
                    threat = threats[f];
                    distanceToThreat = tempDistance;
                }
            }
            if (threat != null) {
                defenseFleet.FleetAI.AddUnitAICommand(Command.CreateFormationCommand(defenseFleet.GetPosition(), Calculator.GetAngleOutOfTwoPositions(defenseFleet.GetPosition(), threat.GetPosition())), Command.CommandAction.Replace);
                defenseFleet.FleetAI.AddUnitAICommand(Command.CreateAttackFleetCommand(threat), Command.CommandAction.AddToEnd);
                threat.sentFleets.Add(defenseFleet);
                if (threat.IsSentFleetsStronger()) {
                    threats.Add(threat);
                    threats.RemoveAt(i);
                }
            } else {
                if ((commands.Count > 0 && commands[0].commandType == Command.CommandType.AttackMoveUnit)
                || (commands.Count > 1 && commands[0].commandType == Command.CommandType.FormationLocation && commands[1].commandType == Command.CommandType.AttackMoveUnit))
                    continue;
                for (int f = 0; f < faction.closeEnemyGroupsDistance.Count; f++) {
                    if (faction.closeEnemyGroupsDistance[f] < faction.GetSize() * .8f) {
                        List<Unit> targetUnits = faction.closeEnemyGroups[f].GetUnits();
                        Unit closestUnit = targetUnits[0];
                        float closestUnitDistance = Vector2.Distance(defenseFleet.GetPosition(), closestUnit.GetPosition());
                        for (int u = i; u < targetUnits.Count; u++) {
                            float newUnitDistance = Vector2.Distance(defenseFleet.GetPosition(), targetUnits[u].GetPosition());
                            if (newUnitDistance < closestUnitDistance) {
                                closestUnit = targetUnits[u];
                                closestUnitDistance = newUnitDistance;
                            }
                        }
                        defenseFleet.FleetAI.AddUnitAICommand(Command.CreateFormationCommand(defenseFleet.GetPosition(), Calculator.GetAngleOutOfTwoPositions(defenseFleet.GetPosition(), closestUnit.GetPosition())), Command.CommandAction.Replace);
                        defenseFleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(closestUnit), Command.CommandAction.AddToEnd);
                    }
                }

            }
        }
        for (int i = 0; i < defenseFleets.Count; i++) {
            Fleet fleet = defenseFleets[i];
            if (fleet.IsFleetIdle()) {
                Vector2 randomTargetPosition = faction.GetAveragePosition() + Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0, 360), Random.Range(100, faction.GetSize()));
                fleet.FleetAI.AddUnitAICommand(Command.CreateFormationCommand(fleet.GetPosition(), Calculator.GetAngleOutOfTwoPositions(fleet.GetPosition(), randomTargetPosition)), Command.CommandAction.Replace);
                fleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(randomTargetPosition), Command.CommandAction.AddToEnd);
            }
        }
        for (int i = attackFleets.Count - 1; i >= 0; i--) {
            Fleet fleet = attackFleets[i];
            if (fleet.IsFleetIdle() || !fleet.HasNearbyEnemyCombatShip()) {
                bool merged = false;
                for (int f = i - 1; f >= 0; f--) {
                    if (fleet == attackFleets[f])
                        continue;
                    if ((attackFleets[f].IsFleetIdle() || !attackFleets[f].HasNearbyEnemyCombatShip()) && Vector2.Distance(fleet.GetPosition(), attackFleets[f].GetPosition()) < 1000 && fleet.GetShips().Count + attackFleets[f].GetShips().Count < 14) {
                        fleet.MergeIntoFleet(attackFleets[f]);
                        merged = true;
                        attackFleets.RemoveAt(i);
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
                if (idleShips[i].IsCombatShip() && autoCommandFleets) {
                    if (idleShips[i].dockedStation == null) {
                        idleShips[i].shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand), Command.CommandAction.Replace);
                    }
                } else if (idleShips[i].IsTransportShip()) {
                    Station miningStation = faction.GetClosestMiningStationWantingTransport(idleShips[i].GetPosition());
                    if (miningStation != null) {
                        ((MiningStationAI)miningStation.stationAI).AddTransportShip(idleShips[i]);
                    } else if (idleShips[i].dockedStation != fleetCommand) {
                        idleShips[i].shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand), Command.CommandAction.Replace);
                    }
                } else if (idleShips[i].IsScienceShip()) {
                    idleShips[i].shipAI.AddUnitAICommand(Command.CreateResearchCommand(faction.GetClosestStar(idleShips[i].GetPosition()), fleetCommand), Command.CommandAction.Replace);
                } else if (idleShips[i].IsConstructionShip()) {
                    Station newMiningStation = ((ConstructionShip)idleShips[i]).CreateStation(faction.GetPosition());
                    idleShips[i].shipAI.AddUnitAICommand(Command.CreateMoveCommand(newMiningStation.GetPosition()), Command.CommandAction.AddToEnd);
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
                        if (defenseFleets.Count < wantedDefenseFleets) {
                            Fleet fleet = faction.CreateNewFleet("DefenseFleet" + (int)(defenseFleets.Count + 1), combatShips);
                            defenseFleets.Add(fleet);
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
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction.factionIndex, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Zarrack), "Science Ship"));
            } else if (randomNumber < 50) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction.factionIndex, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Aria)));
            } else if (randomNumber < 80) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction.factionIndex, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Lancer)));
            } else {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction.factionIndex, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Aterna)));
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
                fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue( new Ship.ShipConstructionBlueprint(faction.factionIndex, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Transport)));
            } else if (wantNewStationBuilder) {
                fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(new Ship.ShipConstructionBlueprint(faction.factionIndex, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.StationBuilder)));
            }
        }
    }

    public override void RemoveFleet(Fleet fleet) {
        base.RemoveFleet(fleet);
        defenseFleets.Remove(fleet);
        attackFleets.Remove(fleet);
    }
}