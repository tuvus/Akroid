using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static Command;

public class ShipAI {
    public readonly List<Command> commands;

    private readonly Ship ship;
    public CommandType currentCommandState;
    private bool newCommand;

    public ShipAI(Ship ship) {
        this.ship = ship;
        commands = new List<Command>(10);
        newCommand = false;
        currentCommandState = CommandType.Idle;
    }

    public void AddUnitAICommand(Command command, CommandAction commandAction = CommandAction.AddToEnd) {
        if ((command.commandType == CommandType.AttackMove || command.commandType == CommandType.AttackMoveUnit ||
                command.commandType == CommandType.Protect) && !ship.HasWeapons()) {
            return;
        }

        if (commandAction == CommandAction.AddToBeginning) {
            newCommand = true;
            if (commands.Any()) commands.First().OnCommandNoLongerActive(ship);
            commands.Insert(0, command);
        } else if (commandAction == CommandAction.Replace) {
            newCommand = true;
            ClearCommands();
            commands.Add(command);
        } else if (commandAction == CommandAction.AddToEnd) {
            if (commands.Count == 0)
                newCommand = true;
            commands.Add(command);
        }
    }

    public void NextCommand() {
        if (commands.Count > 0) {
            commands.First().OnRemoveCommand(ship, true);
            commands.RemoveAt(0);
            newCommand = true;
        }

        if (commands.Count == 0 && ship.shipAction == Ship.ShipAction.Idle) {
            ship.SetIdle();
        }
    }

    public void ClearCommands() {
        if (commands.Count > 0) {
            commands.First().OnRemoveCommand(ship, true);
            for (int i = 1; i < commands.Count; i++) {
                commands[i].OnRemoveCommand(ship, false);
            }
        }

        commands.Clear();
        ship.SetIdle();
        if (ship.fleet == null)
            ship.faction.baseGroup.AddBattleObject(ship);
    }

    public void UpdateAI(float deltaTime) {
        if (commands.Count > 0) {
            // Profiler.BeginSample("ShipAI ResolveCommand");
            CommandResult result = ResolveCommand(commands[0], deltaTime);
            if (result == CommandResult.StopRemove || result == CommandResult.ContinueRemove) {
                commands.RemoveAt(0);
                newCommand = true;
            }

            if (result == CommandResult.ContinueRemove || result == CommandResult.Continue)
                UpdateAI(deltaTime);
            // Profiler.EndSample();
        }
    }

    private enum CommandResult {
        Stop = 0,
        StopRemove = 1,
        ContinueRemove = 2,
        Continue = 3
    }

    #region CommandLogic

    private CommandResult ResolveCommand(Command command, float deltaTime) {
        return command.commandType switch {
            CommandType.Idle => DoIdleCommand(command, deltaTime),
            CommandType.Wait => DoWaitCommand(command, deltaTime),
            CommandType.TurnToRotation => DoTurnToRotationCommand(command, deltaTime),
            CommandType.TurnToPosition => DoTurnToPositionCommand(command, deltaTime),
            CommandType.Move => DoMoveRotateCommand(command, deltaTime),
            CommandType.AttackMove => DoAttackMoveCommand(command, deltaTime),
            CommandType.AttackMoveUnit => DoAttackMoveUnitCommand(command, deltaTime),
            CommandType.AttackFleet => DoAttackFleetCommand(command, deltaTime),
            CommandType.Follow => DoFollowCommand(command, deltaTime),
            CommandType.Protect => DoProtectCommand(command, deltaTime),
            CommandType.Formation => DoFormationCommand(command, deltaTime),
            CommandType.FormationLocation => DoFormationLocationCommand(command, deltaTime),
            CommandType.Dock => DoDockCommand(command, deltaTime),
            CommandType.UndockCommand => DoUndockCommand(command, deltaTime),
            CommandType.Transport => DoTransportCommand(command, deltaTime),
            CommandType.TransportDelay => DoTransportDelayCommand(command, deltaTime),
            CommandType.Trade => DoTradeCommand(command, deltaTime),
            CommandType.Research => DoResearchCommand(command, deltaTime),
            CommandType.CollectGas => DoCollectGasCommand(command, deltaTime),
            CommandType.Colonize => DoColonizeCommand(command, deltaTime),
            CommandType.BuildStation => DoBuildStationCommand(command, deltaTime)
        };
    }

    /// <summary> Idles until something removes this command. </summary>
    private CommandResult DoIdleCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.Idle;
            ship.SetIdle();
            newCommand = false;
        }

        return CommandResult.Stop;
    }

    /// <summary>
    /// Waits for a certain amount of time, Stop until the time is up, ContinueRemove once finished.
    /// </summary>
    private CommandResult DoWaitCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.Wait;
            newCommand = false;
        }

        command.waitTime -= deltaTime;
        if (command.waitTime <= 0) {
            return CommandResult.ContinueRemove;
        }

        return CommandResult.Stop;
    }

    /// <summary> Rotates towards angle, Stop until turned to rotation, ContinueRemove once Finished </summary>
    private CommandResult DoTurnToRotationCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.TurnToRotation;
            ship.SetTargetRotate(command.targetRotation);
            newCommand = false;
        }

        if (ship.shipAction == Ship.ShipAction.Idle) {
            return CommandResult.ContinueRemove;
        }

        return CommandResult.Stop;
    }

    /// <summary> Rotates towards position, Stop until turned to angle, ContinueRemove once Finished. </summary>
    private CommandResult DoTurnToPositionCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.TurnToPosition;
            ship.SetTargetRotate(command.targetPosition);
            newCommand = false;
        }

        if (ship.shipAction == Ship.ShipAction.Idle) {
            return CommandResult.ContinueRemove;
        }

        return CommandResult.Stop;
    }

    /// <summary>
    ///     Rotates towards position then moves towards position, Stop until moved to position, ContinueRemoveOnce
    ///     Finished.
    /// </summary>
    private CommandResult DoMoveRotateCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.Move;
            ship.SetMovePosition(command.targetPosition);
            ship.SetMaxSpeed(command.maxSpeed);
            newCommand = false;
        }

        if (ship.shipAction == Ship.ShipAction.Idle && currentCommandState == CommandType.Move) {
            return CommandResult.ContinueRemove;
        }

        return CommandResult.Stop;
    }

    /// <summary>
    ///     Follows closest enemy ship then goes to target position, Stop until all nearby enemy ships are removed and at
    ///     target position, ContinueRemove once Finished.
    /// </summary>
    private CommandResult DoAttackMoveCommand(Command command, float deltaTime) {
        if (command.waitTime > 0)
            command.waitTime -= deltaTime;
        if (command.waitTime < 0 || newCommand) {
            command.waitTime += 0.2f;

            float distanceToTargetUnit = 0;
            if (command.targetUnit != null)
                distanceToTargetUnit = Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition());

            //If there is a targetUnit check if a new one should be calculated
            if (currentCommandState != CommandType.Move &&
                (command.targetUnit == null || !command.targetUnit.IsSpawned() ||
                    ship.fleet == null && distanceToTargetUnit > ship.GetMaxWeaponRange() * 2 ||
                    ship.fleet != null && ship.GetEnemyUnitsInRange().Count > 0 &&
                    command.targetUnit != ship.GetEnemyUnitsInRange()[0] ||
                    distanceToTargetUnit > ship.GetMaxWeaponRange() &&
                    command.targetUnit != GetClosestNearbyEnemyUnit())) {
                newCommand = true;
                command.targetUnit = null;
            }

            if (newCommand) {
                ship.SetMovePosition(command.targetPosition);
                currentCommandState = CommandType.Move;
                ship.SetMaxSpeed(command.maxSpeed);
                newCommand = false;
            }

            if (currentCommandState == CommandType.Move) {
                command.targetUnit = GetClosestNearbyEnemyUnit();
                if (command.targetUnit != null) {
                    distanceToTargetUnit = Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition());
                    if (distanceToTargetUnit < ship.GetMinWeaponRange()) {
                        currentCommandState = CommandType.TurnToRotation;
                    } else {
                        currentCommandState = CommandType.AttackMove;
                        ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    }
                } else if (ship.shipAction == Ship.ShipAction.Idle) {
                    if (command.commandType == CommandType.Protect) {
                        if (Vector2.Distance(ship.GetPosition(), command.protectUnit.GetPosition()) >
                            (ship.GetSize() + command.protectUnit.GetSize()) * 3)
                            ship.SetMovePosition(command.protectUnit.GetPosition(),
                                (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                        return CommandResult.Stop;
                    }

                    return CommandResult.ContinueRemove;
                } else {
                    return CommandResult.Stop;
                }
            }

            if (currentCommandState == CommandType.AttackMove) {
                if (ship.shipAction == Ship.ShipAction.Idle || distanceToTargetUnit <= ship.GetMinWeaponRange() * .8f) {
                    currentCommandState = CommandType.TurnToRotation;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    return CommandResult.Stop;
                }
            }

            if (currentCommandState == CommandType.TurnToRotation) {
                if (distanceToTargetUnit > ship.GetMinWeaponRange()) {
                    currentCommandState = CommandType.AttackMove;
                } else {
                    ship.SetTargetRotate(command.targetUnit.GetPosition(), ship.GetCombatRotation());
                }
            }
        }

        return CommandResult.Stop;
    }

    /// <summary>
    ///     Follows closest enemy ship then follows friendly ship, Stop until friendly ship is destroyed, Creates an
    ///     attackMoveCommand on current position once the friendly ship is destroyed.
    /// </summary>
    private CommandResult DoProtectCommand(Command command, float deltaTime) {
        if (command.commandType == CommandType.Protect && command.protectUnit == null) {
            command.commandType = CommandType.AttackMove;
            command.protectUnit = null;
            return DoAttackMoveCommand(command, deltaTime);
        }

        if (command.waitTime > 0)
            command.waitTime -= deltaTime;
        if (command.waitTime < 0 || newCommand) {
            command.waitTime += 0.2f;

            float distanceToTargetUnit = 0;
            if (command.targetUnit != null)
                distanceToTargetUnit = Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition());

            //If there is a targetUnit check if a new one should be calculated
            if (currentCommandState != CommandType.Move &&
                (command.targetUnit == null || !command.targetUnit.IsSpawned() ||
                    ship.fleet == null && distanceToTargetUnit > ship.GetMaxWeaponRange() * 2 ||
                    ship.fleet != null && ship.GetEnemyUnitsInRange().Count > 0 &&
                    command.targetUnit != ship.GetEnemyUnitsInRange()[0] ||
                    distanceToTargetUnit > ship.GetMaxWeaponRange() &&
                    command.targetUnit != GetClosestNearbyEnemyUnit())) {
                newCommand = true;
                command.targetUnit = null;
            }

            if (newCommand) {
                ship.SetMovePosition(command.protectUnit.GetPosition(),
                    (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                command.targetPosition = ship.GetTargetMovePosition();
                currentCommandState = CommandType.Move;
                ship.SetMaxSpeed(command.maxSpeed);
                newCommand = false;
            }

            if (currentCommandState == CommandType.Move) {
                command.targetUnit = GetClosestNearbyEnemyUnit();
                if (command.targetUnit != null) {
                    distanceToTargetUnit = Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition());
                    if (distanceToTargetUnit < ship.GetMinWeaponRange()) {
                        currentCommandState = CommandType.TurnToRotation;
                    } else {
                        currentCommandState = CommandType.AttackMove;
                        ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    }
                } else if (ship.shipAction == Ship.ShipAction.Idle) {
                    if (command.commandType == CommandType.Protect) {
                        if (Vector2.Distance(ship.GetPosition(), command.protectUnit.GetPosition()) >
                            (ship.GetSize() + command.protectUnit.GetSize()) * 3)
                            ship.SetMovePosition(command.protectUnit.GetPosition(),
                                (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                        return CommandResult.Stop;
                    }

                    return CommandResult.ContinueRemove;
                } else {
                    ship.SetMovePosition(command.protectUnit.GetPosition(),
                        (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                    command.targetPosition = ship.GetTargetMovePosition();
                    return CommandResult.Stop;
                }
            }

            if (currentCommandState == CommandType.AttackMove) {
                if (ship.shipAction == Ship.ShipAction.Idle || distanceToTargetUnit <= ship.GetMinWeaponRange() * .8f) {
                    currentCommandState = CommandType.TurnToRotation;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    return CommandResult.Stop;
                }
            }

            if (currentCommandState == CommandType.TurnToRotation) {
                if (distanceToTargetUnit > ship.GetMinWeaponRange()) {
                    currentCommandState = CommandType.AttackMove;
                } else {
                    ship.SetTargetRotate(command.targetUnit.GetPosition(), ship.GetCombatRotation());
                }
            }
        }

        return CommandResult.Stop;
    }

    /// <summary> Follows enemy ship, Stop until enemy ship is destroyed, ContinueRemove once Finished. </summary>
    private CommandResult DoAttackMoveUnitCommand(Command command, float deltaTime) {
        if (command.waitTime > 0)
            command.waitTime -= deltaTime;
        if (command.waitTime < 0 || newCommand) {
            command.waitTime += 0.2f;
            if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
                command.commandType = CommandType.AttackMove;
                if (newCommand) {
                    command.targetPosition = ship.GetPosition();
                    ship.SetMaxSpeed(command.maxSpeed);
                }

                newCommand = true;
                return CommandResult.Stop;
            }

            if (newCommand) {
                currentCommandState = CommandType.Move;
                ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                command.targetPosition = command.targetUnit.GetPosition();
                ship.SetMaxSpeed(command.maxSpeed);
                newCommand = false;
            }

            if (currentCommandState == CommandType.Move) {
                if (ship.shipAction == Ship.ShipAction.Idle ||
                    Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) <=
                    ship.GetMinWeaponRange() * .8f) {
                    currentCommandState = CommandType.TurnToRotation;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    return CommandResult.Stop;
                }
            }

            if (currentCommandState == CommandType.TurnToRotation) {
                if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMinWeaponRange()) {
                    currentCommandState = CommandType.Move;
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                } else {
                    ship.SetTargetRotate(command.targetUnit.GetPosition(), ship.GetCombatRotation());
                }
            }
        }

        return CommandResult.Stop;
    }

    /// <summary> Attacks the fleet with this ship </summary>
    private CommandResult DoAttackFleetCommand(Command command, float deltaTime) {
        if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
            command.targetUnit = GetClosestShipInTargetFleet(command.targetFleet);
            newCommand = true;
        }

        if ((newCommand || ship.shipAction == Ship.ShipAction.Idle) && command.targetUnit != null &&
            command.targetUnit.IsSpawned()) {
            Vector2 targetPosition;
            float targetAngle =
                Calculator.GetAngleOutOfTwoPositions(ship.GetPosition(), command.targetUnit.GetPosition());
            if (targetAngle <= 0) {
                targetAngle = Calculator.ConvertTo360DegRotation(targetAngle + 120);
                targetPosition = command.targetUnit.GetPosition() +
                    Calculator.GetPositionOutOfAngleAndDistance(targetAngle, ship.GetMinWeaponRange());
            } else {
                targetAngle = Calculator.ConvertTo360DegRotation(targetAngle - 120);
                targetPosition = command.targetUnit.GetPosition() -
                    Calculator.GetPositionOutOfAngleAndDistance(targetAngle, ship.GetMinWeaponRange());
            }

            ship.SetMoveRotateTarget(targetPosition);
            ship.SetMaxSpeed(command.maxSpeed);
            //newCommand = false;
        }

        if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
            ship.SetIdle();
            return CommandResult.ContinueRemove;
        }

        return CommandResult.Stop;
    }

    /// <summary> Follows friendly ship, Continue until friendly ship is destroyed, ContinueRemove once Finished. </summary>
    private CommandResult DoFollowCommand(Command command, float deltaTime) {
        if (newCommand) {
            ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
            newCommand = false;
            ship.SetMaxSpeed(command.maxSpeed);
            return CommandResult.Stop;
        }

        if (command.targetUnit == null) {
            ship.SetIdle();
            return CommandResult.ContinueRemove;
        }

        ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
        return CommandResult.Stop;
    }

    /// <summary>
    ///     Follows the friendly ship in a formation, Continue until friendly ship formation leader is destroyed,
    ///     ContinueRemove once Finished.
    /// </summary>
    private CommandResult DoFormationCommand(Command command, float deltaTime) {
        if (command.targetUnit == null) {
            return CommandResult.ContinueRemove;
        }

        float distance = Vector2.Distance(ship.position, command.targetUnit.position + command.targetPosition);
        if (distance > ship.GetTurnSpeed() * deltaTime / 10) {
            CommandResult result =
                ResolveCommand(
                    CreateMoveCommand(Vector3.MoveTowards(ship.position,
                        command.targetUnit.position + command.targetPosition,
                        distance)), deltaTime);
            if (result == CommandResult.ContinueRemove || result == CommandResult.Continue) {
                return CommandResult.Continue;
            }
        }

        ship.SetMovePosition(command.targetUnit.position + command.targetPosition);
        return CommandResult.Continue;
    }

    /// <summary>
    ///     Follows the friendly ship in a formation relative to their rotation, Continue until friendly ship formation
    ///     leader is destroyed, ContinueRemove once Finished.
    /// </summary>
    private CommandResult DoFormationLocationCommand(Command command, float deltaTime) {
        if (command.targetUnit == null) {
            return CommandResult.ContinueRemove;
        }

        float targetAngle = command.targetRotation - command.targetUnit.rotation;
        float distanceToTargetAngle = Calculator.GetDistanceToPosition(command.targetPosition);
        Vector2 targetOffsetPosition =
            Calculator.GetPositionOutOfAngleAndDistance(
                targetAngle + Calculator.GetAngleOutOfPosition(command.targetPosition),
                distanceToTargetAngle);
        float distance = Vector2.Distance(ship.position, command.targetUnit.position + targetOffsetPosition);
        if (distance > ship.GetThrust() * deltaTime / 10) {
            CommandResult result =
                ResolveCommand(CreateMoveCommand(command.targetUnit.position + targetOffsetPosition), deltaTime);
            if (result == CommandResult.Stop || result == CommandResult.StopRemove) {
                return CommandResult.Stop;
            }
        }

        ship.SetMovePosition(command.targetUnit.position + targetOffsetPosition);
        CommandResult rotationResult = ResolveCommand(CreateRotationCommand(command.targetUnit.rotation), deltaTime);
        if (rotationResult == CommandResult.ContinueRemove || rotationResult == CommandResult.Continue) {
            return CommandResult.Continue;
        }

        return CommandResult.Stop;
    }

    /// <summary> Goes to then docks at the station. </summary>
    private CommandResult DoDockCommand(Command command, float deltaTime) {
        if (newCommand) {
            if (command.destinationStation != null) {
                ship.SetDockTarget(command.destinationStation);
                ship.SetMaxSpeed(command.maxSpeed);
            } else {
                ship.SetIdle();
                return CommandResult.ContinueRemove;
            }

            newCommand = false;
        }

        if (command.destinationStation == null ||
            ship.shipAction == Ship.ShipAction.Idle && ship.dockedStation == command.destinationStation) {
            ship.SetIdle();
            return CommandResult.StopRemove;
        }

        return CommandResult.Stop;
    }

    /// <summary> Undocks from the station </summary>
    private CommandResult DoUndockCommand(Command command, float deltaTime) {
        if (command.commandType == CommandType.UndockCommand) {
            if (ship.dockedStation != null)
                ship.UndockShip(command.targetRotation);
            return CommandResult.StopRemove;
        }

        return CommandResult.Stop;
    }

    /// <summary> AttackMove to the star, do research, then remove command. </summary>
    private CommandResult DoResearchCommand(Command command, float deltaTime) {
        if (command.targetStar == null || command.destinationStation == null) {
            return CommandResult.StopRemove;
        }

        if (newCommand) {
            if (ship.moduleSystem.Get<ResearchEquipment>().Any(r => r.WantsMoreData())) {
                command.targetPosition = command.targetStar.GetPosition() + Calculator.GetPositionOutOfAngleAndDistance(
                    Calculator.GetAngleOutOfTwoPositions(command.targetStar.GetPosition(), ship.position) +
                    ship.random.NextFloat(-10, 10), ship.GetSize() + command.targetStar.GetSize() * 2);
                ship.SetMovePosition(command.targetPosition);
                currentCommandState = CommandType.Move;
            } else {
                ship.SetDockTarget(command.destinationStation);
                currentCommandState = CommandType.Dock;
            }

            ship.SetMaxSpeed(command.maxSpeed);
            newCommand = false;
        }

        if (ship.shipAction == Ship.ShipAction.Idle) {
            if (currentCommandState == CommandType.Move) {
                currentCommandState = CommandType.Research;
                return CommandResult.Stop;
            }
            if (currentCommandState == CommandType.Research) {
                foreach (ResearchEquipment researchEquipment in ship.moduleSystem.Get<ResearchEquipment>()) {
                    if (!researchEquipment.GatherData(command.targetStar, deltaTime)) {
                        return CommandResult.Stop;
                    }
                }

                ship.SetDockTarget(command.destinationStation);
                currentCommandState = CommandType.Dock;
                return CommandResult.Stop;
            }
            if (currentCommandState == CommandType.Dock) {
                if (ship.dockedStation == null) {
                    // The station must have been destroyed
                    return CommandResult.StopRemove;
                }

                currentCommandState = CommandType.Wait;
                ship.moduleSystem.Get<ResearchEquipment>()
                    .ForEach(r => ship.dockedStation.faction.AddScience(r.DownloadData()));
                return CommandResult.Stop;
            }
            if (currentCommandState == CommandType.Wait) {
                if (ship.GetHealth() < ship.GetMaxHealth()) return CommandResult.Stop;
                command.targetPosition = command.targetStar.GetPosition() + Calculator.GetPositionOutOfAngleAndDistance(
                    Calculator.GetAngleOutOfTwoPositions(command.targetStar.GetPosition(), ship.position) +
                    ship.random.NextFloat(-10, 10), ship.GetSize() + command.targetStar.GetSize() * 2);
                ship.SetMovePosition(command.targetPosition);
                currentCommandState = CommandType.Move;
                return CommandResult.Stop;
            }
            if (currentCommandState == CommandType.Idle) {
                newCommand = true;
            }
        }

        return CommandResult.Stop;
    }

    /// <summary> AttackMove to the gas cloud, do research, then remove command. </summary>
    private CommandResult DoCollectGasCommand(Command command, float deltaTime) {
        if (command.targetGasCloud == null || command.destinationStation == null) {
            return CommandResult.StopRemove;
        }

        if (newCommand || command.supplierContract == null) {
            FactionTrade factionTrade = ship.faction.factionTrade;
            var gasRequests = new List<Tuple<float, Unit, FactionTrade.Offer>>();
            factionTrade.resourcesRequested[CargoBay.CargoTypes.Gas]
                .ToList().ForEach(r => {
                    long amount = math.min(ship.GetAvailableCargoSpace(CargoBay.CargoTypes.Gas) +
                        ship.GetAllCargoOfType(CargoBay.CargoTypes.Gas), r.Value.amount);
                    if (amount == 0) return;
                    var offer = new FactionTrade.Offer(r.Value, amount);
                    gasRequests.Add(new Tuple<float, Unit, FactionTrade.Offer>(
                        amount * factionTrade.GetOurSellValueOfOffer(r.Key.faction, offer), r.Key, offer));
                });
            gasRequests.Sort((a, b) => a.Item1.CompareTo(b.Item2));
            var chosenRequest = gasRequests.FirstOrDefault();
            if (chosenRequest == null) return CommandResult.Stop;
            command.supplierContract = new FactionTrade.Contract(ship, chosenRequest.Item2, chosenRequest.Item3);
            ((Station)chosenRequest.Item2).AddContract(command.supplierContract.Value);

            currentCommandState = CommandType.Idle;
            ship.SetMaxSpeed(command.maxSpeed);
            newCommand = false;
        }

        if (ship.shipAction != Ship.ShipAction.Idle) return CommandResult.Stop;

        if (currentCommandState == CommandType.Move) {
            // We must be at the gas cloud, start collecting gas
            currentCommandState = CommandType.CollectGas;
        }
        if (currentCommandState == CommandType.Dock) {
            // We must be at the receiver station, start unloading
            currentCommandState = CommandType.Wait;
            ((Station)command.supplierContract.Value.receiver).contractShipsDocked
                .Add(command.supplierContract.Value);
            return CommandResult.Stop;
        }
        if (currentCommandState == CommandType.Wait) {
            if (!ship.faction.factionTrade.activeContracts.Contains(command.supplierContract.Value))
                command.supplierContract = null;
            return CommandResult.Stop;
        }
        if (currentCommandState == CommandType.CollectGas) {
            if (command.targetGasCloud.HasResources()) {
                foreach (GasCollector gasCollector in ship.moduleSystem.Get<GasCollector>()) {
                    if (gasCollector.CollectGas(command.targetGasCloud, deltaTime)) {
                        return CommandResult.Stop;
                    }
                }

                ship.SetDockTarget(command.destinationStation);
                currentCommandState = CommandType.Dock;
                return CommandResult.Stop;
            } else {
                command.targetGasCloud = ship.faction.GetClosestGasCloud(ship.GetPosition());
                if (command.targetGasCloud == null)
                    return CommandResult.StopRemove;
                // Move to a new gas cloud or go to the station early
                currentCommandState = CommandType.Idle;
            }
        }

        if (currentCommandState == CommandType.Idle) {
            if (ship.GetAllCargoOfType(CargoBay.CargoTypes.Gas) <
                command.supplierContract.Value.cargo[CargoBay.CargoTypes.Gas].amount) {
                command.targetPosition = command.targetGasCloud.GetPosition() + new Vector2(
                    ship.random.NextFloat(-command.targetGasCloud.size, command.targetGasCloud.size) / 2,
                    ship.random.NextFloat(-command.targetGasCloud.size, command.targetGasCloud.size) / 2);
                ship.SetMovePosition(command.targetPosition, 2);
                currentCommandState = CommandType.Move;
            } else {
                ship.SetDockTarget((Station)command.supplierContract.Value.receiver);
                currentCommandState = CommandType.Dock;
            }
        }

        return CommandResult.Stop;
    }


    /// <summary> Sets up the ship to transport goods from one station to another. The transport will only undock when full. </summary>
    private CommandResult DoTransportCommand(Command command, float deltaTime) {
        if (command.destinationStation == null || !command.destinationStation.IsSpawned())
            return CommandResult.StopRemove;
        if (command.productionStation == null || !command.productionStation.IsSpawned())
            return CommandResult.StopRemove;

        //TODO: Create a more robust cargo transfer system
        long cargoTransferSpeed = 400;
        if (ship.dockedStation != null || newCommand) {
            if (newCommand) {
                currentCommandState = CommandType.Transport;
                ship.SetMaxSpeed(command.maxSpeed);
                ship.SetGroup(command.productionStation.GetGroup());
                newCommand = false;
            }

            if (ship.dockedStation == command.productionStation) {
                if (command.useAlternateCommandOnceDone && currentCommandState == CommandType.Move) {
                    currentCommandState = CommandType.Idle;
                    return CommandResult.StopRemove;
                }

                if (ship.GetAvailableCargoSpace(command.cargoType) <= 0) {
                    ship.SetDockTarget(command.destinationStation);
                    currentCommandState = CommandType.Dock;
                } else {
                    ship.LoadCargoFromUnit(cargoTransferSpeed, command.cargoType, command.productionStation);
                    currentCommandState = CommandType.Wait;
                }
            } else if (ship.dockedStation == command.destinationStation) {
                if (ship.GetAllCargoOfType(command.cargoType) <= 0) {
                    ship.SetDockTarget(command.productionStation);
                    if (command.useAlternateCommandOnceDone)
                        currentCommandState = CommandType.Move;
                    else
                        currentCommandState = CommandType.Dock;
                } else {
                    if (command.autoUnload)
                        command.destinationStation.LoadCargoFromUnit(cargoTransferSpeed, command.cargoType, ship);
                    currentCommandState = CommandType.Wait;
                }
            } else {
                if (ship.GetAvailableCargoSpace(command.cargoType) <= 0) {
                    ship.SetDockTarget(command.destinationStation);
                    currentCommandState = CommandType.Dock;
                } else {
                    ship.SetDockTarget(command.productionStation);
                    currentCommandState = CommandType.Dock;
                }
            }
        }

        return CommandResult.Stop;
    }

    /// <summary>
    ///     Sets up the ship to transport goods from one station to another. The transport will undock when full or after
    ///     a certain amount of time.
    /// </summary>
    private CommandResult DoTransportDelayCommand(Command command, float deltaTime) {
        if (command.destinationStation == null || !command.destinationStation.IsSpawned()) {
            return CommandResult.StopRemove;
        }

        if (command.productionStation == null || !command.destinationStation.IsSpawned()) {
            return CommandResult.StopRemove;
        }

        //TODO: Create a more robust cargo transfer system
        long cargoTransferSpeed = 400;
        if (ship.dockedStation != null || newCommand) {
            if (newCommand) {
                currentCommandState = CommandType.Transport;
                ship.SetMaxSpeed(command.maxSpeed);
                ship.SetGroup(command.productionStation.GetGroup());
                newCommand = false;
            }

            if (ship.dockedStation == command.productionStation) {
                if (ship.GetAllCargoOfType(command.cargoType) > 0) {
                    command.waitTime -= deltaTime;
                }

                if (ship.GetAvailableCargoSpace(command.cargoType) <= 0 || command.waitTime <= 0) {
                    ship.SetDockTarget(command.destinationStation);
                    currentCommandState = CommandType.Dock;
                    command.waitTime = command.targetRotation;
                } else {
                    ship.LoadCargoFromUnit(cargoTransferSpeed, command.cargoType, command.productionStation);
                    currentCommandState = CommandType.Wait;
                }
            } else if (ship.dockedStation == command.destinationStation) {
                if (ship.GetAllCargoOfType(command.cargoType) > 0) {
                    command.waitTime -= deltaTime;
                }

                if (ship.GetAllCargoOfType(command.cargoType) <= 0 || command.waitTime <= 0) {
                    ship.SetDockTarget(command.productionStation);
                    currentCommandState = CommandType.Dock;
                    command.waitTime = command.targetRotation;
                } else {
                    if (command.autoUnload)
                        command.destinationStation.LoadCargoFromUnit(cargoTransferSpeed, command.cargoType, ship);
                    currentCommandState = CommandType.Wait;
                }
            } else {
                command.waitTime = command.targetRotation;
                if (ship.GetAvailableCargoSpace(command.cargoType) <= 0) {
                    ship.SetDockTarget(command.destinationStation);
                    currentCommandState = CommandType.Dock;
                } else {
                    ship.SetDockTarget(command.productionStation);
                    currentCommandState = CommandType.Dock;
                }
            }
        }

        return CommandResult.Stop;
    }

    private CommandResult DoTradeCommand(Command command, float deltaTime) {
        if (command.supplierContract == null && command.requestContract == null) {
            FactionTrade factionTrade = ship.faction.factionTrade;
            Dictionary<CargoBay.CargoTypes, List<Tuple<float, Unit, FactionTrade.Offer>>> providedContracts = new();
            CargoBay.allCargoTypes.Where(c => command.cargoType == CargoBay.CargoTypes.All || c == command.cargoType)
                .ToList().ForEach(c =>
                    providedContracts.Add(c, new List<Tuple<float, Unit, FactionTrade.Offer>>()));
            factionTrade.GetFactionsWeCanBuyFrom().ToList().ForEach(f =>
                providedContracts.Keys.ToList().ForEach(c =>
                    providedContracts[c].AddRange(f.resourcesOffered[c].Select(offer =>
                        new Tuple<float, Unit, FactionTrade.Offer>(
                            factionTrade.GetOurBuyValueOfOffer(f.faction, offer.Value) *
                            math.min(ship.GetAvailableCargoSpace(c), offer.Value.amount), offer.Key,
                            offer.Value)))));
            CargoBay.allCargoTypes.ForEach(c => providedContracts[c]
                .Sort((a, b) => {
                    int comparison = b.Item1.CompareTo(a.Item1);
                    if (comparison != 0) return comparison;
                    return math.distancesq(ship.position, a.Item2.position)
                        .CompareTo(math.distancesq(ship.position, b.Item2.position));
                }));

            Dictionary<CargoBay.CargoTypes, List<Tuple<float, Unit, FactionTrade.Offer>>>
                requestedContracts = new();

            CargoBay.allCargoTypes.Where(c => command.cargoType == CargoBay.CargoTypes.All || c == command.cargoType)
                .ToList().ForEach(c =>
                    requestedContracts.Add(c, new List<Tuple<float, Unit, FactionTrade.Offer>>()));
            factionTrade.GetFactionsWeCanSellTo().ToList().ForEach(f =>
                requestedContracts.Keys.ToList().ForEach(c => requestedContracts[c].AddRange(
                    f.resourcesRequested[c].Select(wanted =>
                        new Tuple<float, Unit, FactionTrade.Offer>(
                            factionTrade.GetOurSellValueOfOffer(wanted.Key.faction, wanted.Value) *
                            math.min(ship.GetAvailableCargoSpace(c), wanted.Value.amount), wanted.Key,
                            wanted.Value)
                    ))));

            var possibleContracts = new List<Tuple<float, FactionTrade.Contract, FactionTrade.Contract>>();
            CargoBay.allCargoTypes.ForEach(c => {
                foreach (var wanted in requestedContracts[c]) {
                    Tuple<float, Unit, FactionTrade.Offer>? provided = null;
                    if (command.destinationStation != null && command.destinationStation != wanted.Item2)
                        provided = providedContracts[c].FirstOrDefault(p => p.Item2 == command.destinationStation);
                    else if (command.destinationStation == null || command.destinationStation == wanted.Item2)
                        provided = providedContracts[c].FirstOrDefault();

                    if (provided == null) continue;
                    if (factionTrade.GetOurBuyValueOfOffer(wanted.Item2.faction, wanted.Item3) >=
                        factionTrade.GetOurSellValueOfOffer(wanted.Item2.faction, wanted.Item3)) break;
                    long amount = math.min(math.min(ship.GetAvailableCargoSpace(c), wanted.Item3.amount),
                        provided.Item3.amount);
                    var providedOffer = new FactionTrade.Offer(provided.Item3, amount);
                    var reqiested = new FactionTrade.Offer(wanted.Item3, amount);
                    possibleContracts.Add(new Tuple<float, FactionTrade.Contract, FactionTrade.Contract>(
                        (factionTrade.GetOurBuyValueOfOffer(provided.Item2.faction, providedOffer) +
                            factionTrade.GetOurSellValueOfOffer(provided.Item2.faction, reqiested)) * amount,
                        new FactionTrade.Contract(provided.Item2, ship, providedOffer),
                        new FactionTrade.Contract(ship, wanted.Item2, reqiested)
                    ));
                }
            });
            possibleContracts.Sort((a, b) => {
                int comparison = b.Item1.CompareTo(a.Item1);
                if (comparison != 0) return comparison;
                return math.distancesq(a.Item2.provider.position, b.Item3.receiver.position)
                    .CompareTo(math.distancesq(b.Item2.provider.position, b.Item3.receiver.position));
            });

            if (possibleContracts.Count == 0) return CommandResult.Stop;
            var chosenContract = possibleContracts.First();
            command.supplierContract = chosenContract.Item2;
            command.requestContract = chosenContract.Item3;
            ((Station)command.supplierContract.Value.provider).AddContract(command.supplierContract.Value);
            ((Station)command.requestContract.Value.receiver).AddContract(command.requestContract.Value);
            currentCommandState = CommandType.Trade;
            newCommand = true;
        }

        if (ship.shipAction == Ship.ShipAction.Idle || newCommand) {
            newCommand = false;
            if (command.supplierContract != null) {
                if (ship.dockedStation != command.supplierContract.Value.provider) {
                    ship.SetDockTarget((Station)command.supplierContract.Value.provider);
                    ship.SetMaxSpeed(command.maxSpeed);
                    currentCommandState = CommandType.Dock;
                    return CommandResult.Stop;
                } else if (!ship.faction.factionTrade.activeContracts.Contains(command.supplierContract.Value)) {
                    command.supplierContract = null;
                } else {
                    //Add Contract to station to transfer cargo
                    if (currentCommandState != CommandType.Wait)
                        ((Station)command.supplierContract.Value.provider).contractShipsDocked.Add(
                            command.supplierContract.Value);
                    currentCommandState = CommandType.Wait;
                    return CommandResult.Stop;
                }
            }
            if (command.requestContract != null) {
                if (ship.dockedStation != command.requestContract.Value.receiver) {
                    ship.SetDockTarget((Station)command.requestContract.Value.receiver);
                    ship.SetMaxSpeed(command.maxSpeed);
                    currentCommandState = CommandType.Dock;
                    return CommandResult.Stop;
                } else if (!ship.faction.factionTrade.activeContracts.Contains(command.requestContract.Value)) {
                    command.requestContract = null;
                } else {
                    //Add Contract to station to transfer cargo
                    if (currentCommandState != CommandType.Wait)
                        ((Station)command.requestContract.Value.receiver).contractShipsDocked.Add(
                            command.requestContract.Value);
                    currentCommandState = CommandType.Wait;
                    return CommandResult.Stop;
                }
            }
        }

        return CommandResult.Stop;
    }


    /// <summary> Moves the ship to the planet to colonize, then initiates colonization of the planet. </summary>
    private CommandResult DoColonizeCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.Move;
            ship.SetMovePosition(command.targetPlanet.position, command.targetPlanet.size + ship.size + 100);
            command.targetPosition = ship.GetTargetMovePosition();
            ship.SetMaxSpeed(command.maxSpeed);
            newCommand = false;
        }

        if (Vector2.Distance(ship.position, command.targetPlanet.position) <=
            ship.size + command.targetPlanet.size + 102) {
            foreach (HabitationArea habitationModule in ship.moduleSystem.modules
                .Where(c => c.GetType() == typeof(HabitationArea))
                .Cast<HabitationArea>()) {
                habitationModule.ColonizePlanet(command.targetPlanet);
            }

            ship.Explode();
            return CommandResult.Stop;
        }

        return CommandResult.Stop;
    }

    /// <summary>
    ///     Moves to the station and builds the station once it is close enough
    /// </summary>
    private CommandResult DoBuildStationCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.Move;
            ship.SetMovePosition(command.destinationStation.position, command.destinationStation.size + ship.size + 2);
            command.targetPosition = ship.GetTargetMovePosition();
            ship.SetMaxSpeed(command.maxSpeed);
            newCommand = false;
        }

        if (Vector2.Distance(ship.position, command.destinationStation.position) <=
            ship.size + command.destinationStation.size + 4) {
            if (!command.destinationStation.BuildStation())
                throw new InvalidProgramException("Trying to build an already built station!");
            ship.Explode();
            return CommandResult.Stop;
        }

        return CommandResult.Stop;
    }

    #endregion


    #region HelperMethods

    private Unit GetClosestNearbyEnemyUnit() {
        if (ship.fleet == null && ship.GetEnemyUnitsInRange().Count > 0)
            return ship.GetEnemyUnitsInRange()[0];
        Unit targetUnit = null;
        float distance = 0;
        for (int i = 0; i < ship.GetEnemyUnitsInRange().Count; i++) {
            Unit tempUnit = ship.GetEnemyUnitsInRange()[i];
            float tempDistance = Vector2.Distance(ship.position, tempUnit.position);
            if (targetUnit == null || tempDistance < distance) {
                targetUnit = tempUnit;
                distance = tempDistance;
            }
        }

        return targetUnit;
    }

    private Ship GetClosestShipInTargetFleet(Fleet fleet) {
        Ship targetShip = null;
        float targetDistance = 0;
        for (int i = 0; i < fleet.GetShips().Count; i++) {
            Ship newTargetShip = fleet.GetShips()[i];
            float newTargetDistance = Vector2.Distance(ship.GetPosition(), newTargetShip.GetPosition());
            if (newTargetDistance < targetDistance || targetShip == null) {
                targetShip = newTargetShip;
                targetDistance = newTargetDistance;
            }
        }

        return targetShip;
    }


    private Unit GetClosestEnemyUnitInRadius(float radius) {
        Unit targetUnit = null;
        float distance = 0;
        for (int i = 0; i < ship.GetEnemyUnitsInRange().Count; i++) {
            Unit tempUnit = ship.GetEnemyUnitsInRange()[i];
            float tempDistance = Vector2.Distance(ship.position, tempUnit.position);
            if (tempDistance <= radius && (targetUnit == null || tempDistance < distance)) {
                targetUnit = tempUnit;
                distance = tempDistance;
            }
        }

        return targetUnit;
    }

    private Ship GetClosestEnemyShipInRadius(float radius) {
        Ship targetUnit = null;
        float distance = 0;

        foreach (Faction faction in ship.faction.enemyFactions) {
            foreach (Ship tempShip in faction.ships) {
                float tempDistance = Vector2.Distance(ship.position, tempShip.position);
                if (tempDistance <= radius && (targetUnit == null || tempDistance < distance)) {
                    targetUnit = tempShip;
                    distance = tempDistance;
                }
            }
        }

        return targetUnit;
    }

    public List<Vector3> GetMovementPositionPlan() {
        List<Vector3> positions = new List<Vector3> { ship.GetPosition() };

        foreach (Command command in commands) {
            if (command.commandType == CommandType.Research) {
                if (currentCommandState == CommandType.Dock) {
                    if (command.destinationStation == null) continue;
                    positions.Add(command.destinationStation.GetPosition());
                } else {
                    positions.Add(command.targetPosition);
                }
            } else if (command.commandType == CommandType.CollectGas) {
                if (currentCommandState == CommandType.Dock) {
                    if (command.destinationStation == null) continue;
                    positions.Add(command.destinationStation.GetPosition());
                } else {
                    positions.Add(command.targetPosition);
                }
            } else if (command.commandType == CommandType.Colonize) {
                if (command.targetPlanet == null) continue;
                positions.Add(Vector2.MoveTowards(ship.GetPosition(), command.targetPlanet.GetPosition(),
                    Vector2.Distance(ship.GetPosition(), command.targetPlanet.GetPosition()) -
                    (ship.GetSize() + command.targetPlanet.GetSize() + 100)));
            } else if (command.commandType == CommandType.Idle || command.commandType == CommandType.Wait
                || command.commandType == CommandType.TurnToRotation ||
                command.commandType == CommandType.TurnToPosition) { } else if
                (command.commandType == CommandType.Protect) {
                if (command.protectUnit == null) continue;
                positions.Add(command.protectUnit.GetPosition());
            } else if (command.commandType == CommandType.AttackMoveUnit || command.commandType == CommandType.Follow) {
                if (command.targetUnit == null) continue;
                positions.Add(command.targetUnit.GetPosition());
            } else if (command.commandType == CommandType.AttackFleet) {
                if (command.targetUnit != null) {
                    positions.Add(command.targetUnit.GetPosition());
                    continue;
                }

                if (command.targetFleet != null) {
                    positions.Add(command.targetFleet.GetPosition());
                }
            } else if (command.commandType == CommandType.Dock) {
                if (command.destinationStation == null) continue;
                positions.Add(command.destinationStation.GetPosition());
            } else if (command.commandType == CommandType.Transport ||
                command.commandType == CommandType.TransportDelay) {
                if (commands.First() == command) {
                    if (ship.GetAllCargoOfType(command.cargoType) > 0) {
                        if (command.destinationStation != null)
                            positions.Add(command.destinationStation.GetPosition());
                        if (command.productionStation != null)
                            positions.Add(command.productionStation.GetPosition());
                    } else {
                        if (command.productionStation != null)
                            positions.Add(command.productionStation.GetPosition());
                        if (command.destinationStation != null)
                            positions.Add(command.destinationStation.GetPosition());
                    }
                } else {
                    if (command.destinationStation != null)
                        positions.Add(command.destinationStation.GetPosition());
                }
            } else if (command.commandType == CommandType.Trade) {
                if (command.supplierContract != null)
                    positions.Add(command.supplierContract.Value.provider.position);
                if (command.requestContract != null)
                    positions.Add(command.requestContract.Value.receiver.position);
            } else {
                if (command.targetPosition == null) continue;
                positions.Add(command.targetPosition);
            }
        }

        return positions;
    }

    #endregion
}
