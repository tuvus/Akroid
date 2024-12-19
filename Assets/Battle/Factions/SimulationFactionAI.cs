using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
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

    public SimulationFactionAI(BattleManager battleManager, Faction faction) : base(battleManager, faction) {
        autoConstruction = true;
        autoCommandFleets = true;
        updateTime = Random.Range(0, 0.2f);
        defenseFleets = new HashSet<Fleet>();
        attackFleets = new HashSet<Fleet>();
        threats = new HashSet<Fleet>();
    }

    public override void GenerateFactionAI() { }

    public override void OnStationBuilt(Station station) {
        if (station.GetStationType() == Station.StationType.FleetCommand) {
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
                    ManageSpecialShipBuilding();
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
            if (faction.stations.Any(s =>
                Vector2.Distance(s.GetPosition(), targetFleet.GetPosition()) <=
                threatDistance + targetFleet.GetSize() + targetFleet.maxWeaponRange))
                continue;
            threats.Add(targetFleet);
        }
    }

    void ManageFleets() {
        foreach (var defenseFleet in defenseFleets.ToList()) {
            bool hasTarget = defenseFleet.FleetAI.commands.Any(c => c.GetTargetObject() != null);

            // If we are already dealing with a threat check for closer threats to deal with
            if (hasTarget) {
                IObject currentThreat = defenseFleet.FleetAI.commands.First(c => c.GetTargetObject() != null).GetTargetObject();
                float distanceToCurrentThreat =
                    Vector2.Distance(defenseFleet.GetPosition(), currentThreat.GetPosition()) - currentThreat.GetSize();
                // If there are no closer threats then we should just continue
                if (distanceToCurrentThreat - defenseFleet.GetSize() <= threatDistance || threats.All(threat => threat == currentThreat ||
                    Vector2.Distance(defenseFleet.GetPosition(), threat.GetPosition()) - threat.GetSize() >
                    distanceToCurrentThreat - 100)) {
                    if (distanceToCurrentThreat <= threatDistance * 2 || currentThreat.GetType() != typeof(Fleet) ||
                        threats.Contains((Fleet)currentThreat))
                        continue;
                }

                continue;
            }

            // Check for new fleet targets
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
                defenseFleet.FleetAI.AddFleetAICommand(Command.CreateAttackFleetCommand(threat), Command.CommandAction.Replace);
                threat.sentFleets.Add(defenseFleet);
                continue;
            }

            // Find any extra units that the fleet needs to defend against
            if (!defenseFleet.FleetAI.HasActionCommand()) {
                for (int f = 0; f < faction.closeEnemyGroupsDistance.Count; f++) {
                    if (faction.closeEnemyGroupsDistance[f] < faction.GetSize()) {
                        List<Unit> targetUnits = faction.closeEnemyGroups[f].battleObjects.ToList();
                        Unit closestUnit = targetUnits.FirstOrDefault(unit =>
                            !unit.IsStation() && unit.IsShip() && ((Ship)unit).IsCombatShip() && unit.IsTargetable()
                            || Vector2.Distance(defenseFleet.GetPosition(), unit.GetPosition()) + defenseFleet.GetSize() <=
                            defenseFleet.maxWeaponRange);
                        if (closestUnit == null) continue;
                        float closestUnitDistance = Vector2.Distance(defenseFleet.GetPosition(), closestUnit.GetPosition());
                        for (int u = 0; u < targetUnits.Count; u++) {
                            float newUnitDistance = Vector2.Distance(defenseFleet.GetPosition(), targetUnits[u].GetPosition());
                            if (newUnitDistance < closestUnitDistance) {
                                closestUnit = targetUnits[u];
                                closestUnitDistance = newUnitDistance;
                            }
                        }

                        if (closestUnit != null) {
                            if (closestUnit.IsShip() && ((Ship)closestUnit).fleet != null) {
                                defenseFleet.FleetAI.AddFleetAICommand(Command.CreateAttackFleetCommand(((Ship)closestUnit).fleet),
                                    Command.CommandAction.Replace);
                                break;
                            } else {
                                defenseFleet.FleetAI.AddFleetAICommand(Command.CreateAttackMoveCommand(closestUnit),
                                    Command.CommandAction.Replace);
                                break;
                            }
                        }
                    }
                }
            }

            // If no kind of threat has been found then lets patrol the area
            if (defenseFleet.IsFleetIdle()) {
                float farthestStationDistance = faction.stations.Max(s => Vector2.Distance(s.GetPosition(), faction.position));
                Vector2 randomTargetPosition = faction.GetAveragePosition() +
                    Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0, 360),
                        Random.Range(100, farthestStationDistance + 500));
                defenseFleet.FleetAI.AddFleetAICommand(
                    Command.CreateFormationCommand(defenseFleet.GetPosition(),
                        Calculator.GetAngleOutOfTwoPositions(defenseFleet.GetPosition(), randomTargetPosition)),
                    Command.CommandAction.Replace);
                defenseFleet.FleetAI.AddFleetAICommand(Command.CreateAttackMoveCommand(randomTargetPosition),
                    Command.CommandAction.AddToEnd);
            }
        }

        // Attack Fleets
        foreach (var attackFleet in attackFleets.ToList()) {
            // If the fleet has an attack command it is likely in battle so don't give it another command
            if (attackFleet.FleetAI.commands.Any(c => c.IsAttackCommand() && c.commandType != Command.CommandType.AttackMove)) continue;

            Unit targetFleetUnit = attackFleet.enemyUnitsInRange.FirstOrDefault(s => s.IsShip() && ((Ship)s).fleet != null);
            if (targetFleetUnit != null) {
                Fleet targetFleet = ((Ship)targetFleetUnit).fleet;
                attackFleet.FleetAI.ClearCommands();
                attackFleet.FleetAI.AddFleetAICommand(Command.CreateAttackFleetCommand(targetFleet));
            }

            // Check if the fleet should merge with another attack fleet
            if (attackFleet.IsFleetIdle() || !attackFleet.HasNearbyEnemyCombatShip()) {
                bool merged = false;
                foreach (var mergeFleet in attackFleets.ToList()) {
                    if (attackFleet == mergeFleet)
                        continue;
                    if ((attackFleet.IsFleetIdle() || !attackFleet.HasNearbyEnemyCombatShip()) &&
                        Vector2.Distance(attackFleet.GetPosition(), mergeFleet.GetPosition()) < 1000 &&
                        attackFleet.GetShips().Count + mergeFleet.GetShips().Count < 14) {
                        attackFleet.MergeIntoFleet(mergeFleet);
                        merged = true;
                        attackFleets.Remove(attackFleet);
                        break;
                    }
                }

                if (merged)
                    continue;
            }

            // If the fleet is idle find a new action or target for the fleet
            if (attackFleet.IsFleetIdle() && (faction.HasEnemy() || !attackFleet.IsDockedWithStation(fleetCommand))) {
                // Check if we should disband the fleet instead
                if (attackFleet.GetShips().Count <= 2 || attackFleet.GetTotalFleetHealth() <= 1000) {
                    List<Ship> shipsInFleet = new List<Ship>(attackFleet.GetShips());
                    attackFleet.DisbandFleet();
                    foreach (Ship ship in shipsInFleet) {
                        ship.shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand));
                    }

                    continue;
                }

                // Attack any close fleets
                if (attackFleet.GetNearbyEnemyFleet() != null) {
                    attackFleet.FleetAI.AddFleetAICommand(Command.CreateAttackFleetCommand(attackFleet.GetNearbyEnemyFleet()),
                        Command.CommandAction.Replace);
                    continue;
                }

                // Find a target station to attack
                Vector2 fleetPosition = attackFleet.GetPosition();
                Station targetStation = faction.GetClosestEnemyStation(fleetPosition);
                if (targetStation != null) {
                    attackFleet.FleetAI.AddFormationCommand(fleetPosition,
                        Calculator.GetAngleOutOfTwoPositions(fleetPosition, targetStation.GetPosition()), Command.CommandAction.Replace);
                    attackFleet.FleetAI.AddFleetAICommand(Command.CreateAttackMoveCommand(targetStation));
                } else {
                    Unit targetUnit = faction.GetClosestEnemyUnit(fleetPosition);
                    if (targetUnit != null) {
                        attackFleet.FleetAI.AddFormationCommand(fleetPosition,
                            Calculator.GetAngleOutOfTwoPositions(fleetPosition, targetUnit.GetPosition()), Command.CommandAction.Replace);
                        attackFleet.FleetAI.AddFleetAICommand(Command.CreateAttackMoveCommand(targetUnit));
                    } else {
                        // No more units to target
                        attackFleet.FleetAI.AddFormationCommand(fleetPosition,
                            Calculator.GetAngleOutOfTwoPositions(fleetPosition, fleetCommand.GetPosition()), Command.CommandAction.Replace);
                        attackFleet.FleetAI.AddFleetAICommand(Command.CreateDockCommand(fleetCommand));
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
                    idleShip.shipAI.AddUnitAICommand(
                        Command.CreateResearchCommand(faction.GetClosestStar(idleShip.GetPosition()), fleetCommand),
                        Command.CommandAction.Replace);
                } else if (idleShip.IsGasCollectorShip()) {
                    idleShip.shipAI.AddUnitAICommand(
                        Command.CreateCollectGasCommand(faction.GetClosestGasCloud(idleShip.GetPosition()), fleetCommand),
                        Command.CommandAction.Replace);
                } else if (idleShip.IsConstructionShip()) {
                    idleShip.shipAI.AddUnitAICommand(
                        Command.CreateBuildStationCommand(idleShip.faction, Station.StationType.MiningStation, faction.GetPosition()),
                        Command.CommandAction.Replace);
                    return;
                }
            }
        }
    }

    void ManageDockedShips() {
        if (faction.HasEnemy()) {
            if (fleetCommand.enemyUnitsInRange.Count > 0) {
                Vector2 position = fleetCommand.enemyUnitsInRange[0].GetPosition();
                foreach (var combatShip in fleetCommand.GetAllDockedShips().Where(s => s.IsCombatShip())) {
                    combatShip.shipAI.AddUnitAICommand(Command.CreateAttackMoveCommand(position), Command.CommandAction.AddToEnd);
                    combatShip.shipAI.AddUnitAICommand(Command.CreateDockCommand(fleetCommand), Command.CommandAction.AddToEnd);
                }
            } else {
                HashSet<Ship> combatShips = fleetCommand.GetAllDockedShips().Where(s => s.IsCombatShip() && !s.IsDamaged())
                    .Take(maxCombatShips).ToHashSet();
                if (combatShips.Count > 8) {
                    int totalHealth = combatShips.Sum(s => s.GetTotalHealth());
                    if (totalHealth > 3000) {
                        if (defenseFleets.Count < wantedDefenseFleets) {
                            if (combatShips.Count >= (int)(minCombatShips * 1.5f)) {
                                Fleet fleet = faction.CreateNewFleet("DefenseFleet" + (int)(defenseFleets.Count + 1), combatShips);
                                defenseFleets.Add(fleet);
                                fleet.FleetAI.AddFormationTowardsPositionCommand(faction.GetAveragePosition(), fleetCommand.GetSize() * 4,
                                    Command.CommandAction.Replace);
                            }
                        } else {
                            Fleet fleet = faction.CreateNewFleet("AttackFleet", combatShips);
                            attackFleets.Add(fleet);
                        }
                    }
                }
            }
        }
    }

    void ManageSpecialShipBuilding() {
        int transportQueueCount = fleetCommand.GetConstructionBay().GetNumberOfShipsOfTypeFaction(Ship.ShipType.Transport, faction);
        int stationBuilderQueueCount =
            fleetCommand.GetConstructionBay().GetNumberOfShipsOfClassFaction(Ship.ShipClass.StationBuilder, faction);
        int gasCollectorQueueCount = fleetCommand.GetConstructionBay().GetNumberOfShipsOfTypeFaction(Ship.ShipType.GasCollector, faction);
        bool wantTransport = faction.GetTotalWantedTransports() > faction.GetShipCountOfType(Ship.ShipType.Transport) + transportQueueCount;
        bool wantNewStationBuilder = fleetCommand.faction.GetAvailableAsteroidFieldsCount() >
            faction.GetShipCountOfType(Ship.ShipType.Construction) + stationBuilderQueueCount;
        int gasCollectorsWanted = faction.GetShipCountOfType(Ship.ShipType.Transport) / 2 + 1;

        if (fleetCommand.GetConstructionBay().HasOpenBays()) {
            if (faction.GetShipCountOfType(Ship.ShipType.GasCollector) + gasCollectorQueueCount < gasCollectorsWanted) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction,
                    BattleManager.Instance.GetShipBlueprint(Ship.ShipType.GasCollector)));
            } else if (transportQueueCount == 0 && stationBuilderQueueCount == 0) {
                if (wantTransport) {
                    fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(
                        new Ship.ShipConstructionBlueprint(faction, BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Transport)));
                } else if (wantNewStationBuilder) {
                    fleetCommand.GetConstructionBay().AddConstructionToBeginningQueue(new Ship.ShipConstructionBlueprint(faction,
                        BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.StationBuilder)));
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

            if (randomNumber < 15) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction,
                    BattleManager.Instance.GetShipBlueprint(Ship.ShipType.Research), "Science Ship"));
            } else if (randomNumber < 45) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction,
                    BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Aria)));
            } else if (randomNumber < 80) {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction,
                    BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Lancer)));
            } else {
                fleetCommand.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction,
                    BattleManager.Instance.GetShipBlueprint(Ship.ShipClass.Aterna)));
            }
        }
    }

    void ManageStationUpgrades() {
        if (fleetCommand.GetAllCargoOfType(CargoBay.CargoTypes.Metal) > 10000) {
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

    public override Station GetFleetCommand() {
        return fleetCommand;
    }
}
