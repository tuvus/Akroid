using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using static System.Collections.Specialized.BitVector32;
using Random = UnityEngine.Random;

public class SimulationFactionAI : FactionAI {
    public Shipyard fleetCommand { get; private set; }
    public HashSet<Fleet> defenseFleets;
    static int wantedDefenseFleets = 2;
    public HashSet<Fleet> attackFleets;
    public HashSet<Fleet> threats;
    static float threatDistance = 1000;
    float updateTime;
    public bool autoCommandFleets;
    public bool autoConstruction;
    public int minCombatShips = 10;
    public int maxCombatShips = 20;

    public override void SetupFactionAI(BattleManager battleManager, Faction faction) {
        base.SetupFactionAI(battleManager, faction);
        autoConstruction = true;
        autoCommandFleets = true;
        updateTime = Random.Range(0, 0.2f);
        defenseFleets = new HashSet<Fleet>();
        attackFleets = new HashSet<Fleet>();
        threats = new HashSet<Fleet>();
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
                if (autoConstruction) {
                    ManageStationBuilding();
                    ManageShipBuilding();
                    ManageStationUpgrades();
                }
                updateTime += .2f;
            }
        }
        Profiler.EndSample();
    }

    void ManageThreats() {
        float farthestStationDistance = faction.stations.Max(s => Vector2.Distance(s.GetPosition(), faction.position));
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
        foreach (var defenseFleet in defenseFleets.ToList()) {
            List<Command> commands = defenseFleet.FleetAI.commands;
            if ((commands.Count > 0 && commands[0].commandType == Command.CommandType.AttackFleet)
                || (commands.Count > 1 && commands[0].commandType == Command.CommandType.FormationLocation && commands[1].commandType == Command.CommandType.AttackFleet))
                continue;
            Fleet threat = null;
            float distanceToThreat = 0;
            foreach (var tempThreat in threats.ToList()) {
                float tempDistance = Vector2.Distance(tempThreat.GetPosition(), defenseFleet.GetPosition());
                if ((tempDistance < distanceToThreat && tempThreat.IsSentFleetsStronger()) || threat == null) {
                    threat = tempThreat;
                    distanceToThreat = tempDistance;
                }
            }
            if (threat != null) {
                defenseFleet.FleetAI.AddUnitAICommand(Command.CreateFormationCommand(defenseFleet.GetPosition(), Calculator.GetAngleOutOfTwoPositions(defenseFleet.GetPosition(), threat.GetPosition())), Command.CommandAction.Replace);
                defenseFleet.FleetAI.AddUnitAICommand(Command.CreateAttackFleetCommand(threat), Command.CommandAction.AddToEnd);
                threat.sentFleets.Add(defenseFleet);
            } else {
                if ((commands.Count > 0 && commands[0].commandType == Command.CommandType.AttackMoveUnit)
                || (commands.Count > 1 && commands[0].commandType == Command.CommandType.FormationLocation && commands[1].commandType == Command.CommandType.AttackMoveUnit))
                    continue;
                for (int f = 0; f < faction.closeEnemyGroupsDistance.Count; f++) {
                    if (faction.closeEnemyGroupsDistance[f] < faction.GetSize() * .8f) {
                        List<Unit> targetUnits = faction.closeEnemyGroups[f].battleObjects.ToList();
                        Unit closestUnit = targetUnits.First();
                        float closestUnitDistance = Vector2.Distance(defenseFleet.GetPosition(), closestUnit.GetPosition());
                        for (int u = 0; u < targetUnits.Count; u++) {
                            float newUnitDistance = Vector2.Distance(defenseFleet.GetPosition(), targetUnits[u].GetPosition());
                            if (newUnitDistance < closestUnitDistance) {
                                closestUnit = targetUnits[u];
                                closestUnitDistance = newUnitDistance;
                            }
                        }
                        if (closestUnit != null) {
                            defenseFleet.FleetAI.AddUnitAICommand(Command.CreateFormationCommand(defenseFleet.GetPosition(), Calculator.GetAngleOutOfTwoPositions(defenseFleet.GetPosition(), closestUnit.GetPosition())), Command.CommandAction.Replace);
                            defenseFleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(closestUnit), Command.CommandAction.AddToEnd);
                        }
                    }
                }

            }
        }
        foreach (var defenseFleet in defenseFleets.ToList()) {
            if (defenseFleet.IsFleetIdle()) {
                Vector2 randomTargetPosition = faction.GetAveragePosition() + Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0, 360), Random.Range(100, faction.GetSize() + 500));
                defenseFleet.FleetAI.AddUnitAICommand(Command.CreateFormationCommand(defenseFleet.GetPosition(), Calculator.GetAngleOutOfTwoPositions(defenseFleet.GetPosition(), randomTargetPosition)), Command.CommandAction.Replace);
                defenseFleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(randomTargetPosition), Command.CommandAction.AddToEnd);
            }
        }
        foreach (var attackFleet in attackFleets.ToList()) {
            if (attackFleet.IsFleetIdle() || !attackFleet.HasNearbyEnemyCombatShip()) {
                bool merged = false;
                foreach (var mergeFleet in attackFleets.ToList()) {
                    if (attackFleet == mergeFleet)
                        continue;
                    if ((attackFleet.IsFleetIdle() || !attackFleet.HasNearbyEnemyCombatShip()) && Vector2.Distance(attackFleet.GetPosition(), mergeFleet.GetPosition()) < 1000 && attackFleet.GetShips().Count + mergeFleet.GetShips().Count < 14) {
                        attackFleet.MergeIntoFleet(mergeFleet);
                        merged = true;
                        attackFleets.Remove(attackFleet);
                        break;
                    }
                }
                if (merged)
                    continue;
            }
            if (attackFleet.IsFleetIdle() && (faction.HasEnemy() || !attackFleet.IsDockedWithStation(fleetCommand))) {
                if (attackFleet.GetShips().Count <= 2 || attackFleet.GetTotalFleetHealth() <= 1000) {
                    List<Ship> shipsInFleet = new List<Ship>(attackFleet.GetShips());
                    attackFleet.DisbandFleet();
                    foreach (Ship ship in shipsInFleet) {
                        ship.shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand));
                    }
                } else {
                    Vector2 fleetPosition = attackFleet.GetPosition();
                    Station targetStation = faction.GetClosestEnemyStation(fleetPosition);
                    if (targetStation != null) {
                        attackFleet.FleetAI.AddFormationCommand(fleetPosition, Calculator.GetAngleOutOfTwoPositions(fleetPosition, targetStation.GetPosition()), Command.CommandAction.Replace);
                        attackFleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(targetStation));
                    } else {
                        Unit targetUnit = faction.GetClosestEnemyUnit(fleetPosition);
                        if (targetUnit != null) {
                            attackFleet.FleetAI.AddFormationCommand(fleetPosition, Calculator.GetAngleOutOfTwoPositions(fleetPosition, targetUnit.GetPosition()), Command.CommandAction.Replace);
                            attackFleet.FleetAI.AddUnitAICommand(Command.CreateAttackMoveCommand(targetUnit));
                        } else {
                            attackFleet.FleetAI.AddFormationCommand(fleetPosition, Calculator.GetAngleOutOfTwoPositions(fleetPosition, fleetCommand.GetPosition()), Command.CommandAction.Replace);
                            attackFleet.FleetAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand));
                        }
                    }
                }
            }
        }
    }

    void ManageIdleShips() {
        foreach (var idleShip in idleShips) {
            if (idleShip.IsIdle() && idleShip.fleet == null) {
                if (idleShip.IsCombatShip() && autoCommandFleets) {
                    if (idleShip.dockedStation == null) {
                        idleShip.shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand), Command.CommandAction.Replace);
                    }
                } else if (idleShip.IsTransportShip()) {
                    Station miningStation = faction.GetClosestMiningStationWantingTransport(idleShip.GetPosition());
                    if (miningStation != null) {
                        ((MiningStationAI)miningStation.stationAI).AddTransportShip(idleShip);
                    } else if (idleShip.dockedStation != fleetCommand) {
                        idleShip.shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand), Command.CommandAction.Replace);
                    }
                } else if (idleShip.IsScienceShip()) {
                    idleShip.shipAI.AddUnitAICommand(Command.CreateResearchCommand(faction.GetClosestStar(idleShip.GetPosition()), fleetCommand), Command.CommandAction.Replace);
                } else if (idleShip.IsConstructionShip()) {
                    Station newMiningStation = ((ConstructionShip)idleShip).CreateStation(faction.GetPosition());
                    idleShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(newMiningStation.GetPosition()), Command.CommandAction.AddToEnd);
                    return;
                }
            }
        }
    }

    void ManageDockedShips() {
        if (faction.HasEnemy()) {
            if (fleetCommand.enemyUnitsInRange.Count > 0) {
                HashSet<Ship> combatShips = fleetCommand.GetHanger().GetAllCombatShips();
                Vector2 position = fleetCommand.enemyUnitsInRange[0].GetPosition();
                foreach (var combatShip in combatShips) {
                    combatShip.shipAI.AddUnitAICommand(Command.CreateAttackMoveCommand(position), Command.CommandAction.AddToEnd);
                    combatShip.shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand), Command.CommandAction.AddToEnd);
                }
            } else {
                HashSet<Ship> combatShips = fleetCommand.GetHanger().GetAllUndamagedCombatShips().Take(maxCombatShips).ToHashSet();
                if (combatShips.Count > 8) {
                    int totalHealth = combatShips.Sum(s => s.GetTotalHealth());
                    if (totalHealth > 3000) {
                        if (defenseFleets.Count < wantedDefenseFleets) {
                            if (combatShips.Count >= (int)(minCombatShips * 1.5f)) {
                                Fleet fleet = faction.CreateNewFleet("DefenseFleet" + (int)(defenseFleets.Count + 1), combatShips);
                                defenseFleets.Add(fleet);
                                fleet.FleetAI.AddFormationTowardsPositionCommand(faction.GetAveragePosition(), fleetCommand.GetSize() * 4);
                            }
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
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Zarrack), "Science Ship"));
            } else if (randomNumber < 50) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Aria)));
            } else if (randomNumber < 80) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Lancer)));
            } else {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Aterna)));
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
                fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(new Ship.ShipConstructionBlueprint(faction, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Transport)));
            } else if (wantNewStationBuilder) {
                fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(new Ship.ShipConstructionBlueprint(faction, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.StationBuilder)));
            }
        }
    }

    void ManageStationUpgrades() {
        if (fleetCommand.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) > 30000) {
            for (int i = 0; i < fleetCommand.moduleSystem.systems.Count; i++) {
                if (fleetCommand.moduleSystem.CanUpgradeSystem(i, fleetCommand)) {
                    fleetCommand.moduleSystem.UpgradeSystem(i, fleetCommand);
                }
            }
        }
    }
    public override void RemoveFleet(Fleet fleet) {
        base.RemoveFleet(fleet);
        defenseFleets.Remove(fleet);
        attackFleets.Remove(fleet);
    }
}