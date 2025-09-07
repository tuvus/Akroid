using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static Command;

public class ShipAI {
    public readonly List<Command> commands;
    public readonly List<ShipAISubprocess> subprocesses;

    private readonly Ship ship;
    public CommandType currentCommandState;
    private bool newCommand;

    public ShipAI(Ship ship) {
        this.ship = ship;
        commands = new List<Command>(2);
        subprocesses = new List<ShipAISubprocess>(1);
        subprocesses.Add(new CrewSubprocess(ship, this));
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
        subprocesses.ForEach(s => s.Update());
        if (commands.Count == 0) return;

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
            CommandType.TradeTransport => DoTradeTransportCommand(command, deltaTime),
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
            SetMovePosition(command.targetPosition);
            SetMaxSpeed(command.maxSpeed);
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
                SetMovePosition(command.targetPosition);
                currentCommandState = CommandType.Move;
                SetMaxSpeed(command.maxSpeed);
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
                        SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    }
                } else if (ship.shipAction == Ship.ShipAction.Idle) {
                    if (command.commandType == CommandType.Protect) {
                        if (Vector2.Distance(ship.GetPosition(), command.protectUnit.GetPosition()) >
                            (ship.GetSize() + command.protectUnit.GetSize()) * 3)
                            SetMovePosition(command.protectUnit.GetPosition(),
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
                    SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
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
                SetMovePosition(command.protectUnit.GetPosition(),
                    (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                command.targetPosition = GetTargetMovePosition();
                currentCommandState = CommandType.Move;
                SetMaxSpeed(command.maxSpeed);
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
                        SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    }
                } else if (ship.shipAction == Ship.ShipAction.Idle) {
                    if (command.commandType == CommandType.Protect) {
                        if (Vector2.Distance(ship.GetPosition(), command.protectUnit.GetPosition()) >
                            (ship.GetSize() + command.protectUnit.GetSize()) * 3)
                            SetMovePosition(command.protectUnit.GetPosition(),
                                (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                        return CommandResult.Stop;
                    }

                    return CommandResult.ContinueRemove;
                } else {
                    SetMovePosition(command.protectUnit.GetPosition(),
                        (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                    command.targetPosition = GetTargetMovePosition();
                    return CommandResult.Stop;
                }
            }

            if (currentCommandState == CommandType.AttackMove) {
                if (ship.shipAction == Ship.ShipAction.Idle || distanceToTargetUnit <= ship.GetMinWeaponRange() * .8f) {
                    currentCommandState = CommandType.TurnToRotation;
                } else {
                    SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
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
                    SetMaxSpeed(command.maxSpeed);
                }

                newCommand = true;
                return CommandResult.Stop;
            }

            if (newCommand) {
                currentCommandState = CommandType.Move;
                SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                command.targetPosition = command.targetUnit.GetPosition();
                SetMaxSpeed(command.maxSpeed);
                newCommand = false;
            }

            if (currentCommandState == CommandType.Move) {
                if (ship.shipAction == Ship.ShipAction.Idle ||
                    Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) <=
                    ship.GetMinWeaponRange() * .8f) {
                    currentCommandState = CommandType.TurnToRotation;
                } else {
                    SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    return CommandResult.Stop;
                }
            }

            if (currentCommandState == CommandType.TurnToRotation) {
                if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMinWeaponRange()) {
                    currentCommandState = CommandType.Move;
                    SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
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
            SetMaxSpeed(command.maxSpeed);
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
            SetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
            newCommand = false;
            SetMaxSpeed(command.maxSpeed);
            return CommandResult.Stop;
        }

        if (command.targetUnit == null) {
            ship.SetIdle();
            return CommandResult.ContinueRemove;
        }

        SetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
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

        SetMovePosition(command.targetUnit.position + command.targetPosition);
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

        SetMovePosition(command.targetUnit.position + targetOffsetPosition);
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
                SetDockTarget(command.destinationStation);
                SetMaxSpeed(command.maxSpeed);
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
                SetMovePosition(command.targetPosition);
                currentCommandState = CommandType.Move;
            } else {
                SetDockTarget(command.destinationStation);
                currentCommandState = CommandType.Dock;
            }

            SetMaxSpeed(command.maxSpeed);
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

                SetDockTarget(command.destinationStation);
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
                SetMovePosition(command.targetPosition);
                currentCommandState = CommandType.Move;
                return CommandResult.Stop;
            }
            if (currentCommandState == CommandType.Idle) {
                newCommand = true;
            }
        }

        return CommandResult.Stop;
    }

    private CommandResult DoCollectGasCommand(Command command, float deltaTime) {
        if (command.targetGasCloud == null || command.destinationStation == null) {
            return CommandResult.StopRemove;
        }

        if (command.supplierContract != null &&
            !ship.faction.factionTrade.activeContracts.Contains(command.supplierContract))
            command.supplierContract = null;

        // Find a new contract if we don't have one and aren't currently going to collect gas
        if (newCommand || (command.supplierContract == null && currentCommandState != CommandType.Move &&
            currentCommandState != CommandType.CollectGas)) {
            FactionTrade factionTrade = ship.faction.factionTrade;
            var gasRequests = new List<Tuple<float, Unit, FactionTrade.TradeOffer>>();
            factionTrade.GetFactionsWeCanSellTo().ToList().ForEach(f => f.resourcesRequested[CargoBay.CargoType.Gas]
                .ToList().ForEach(r => {
                    long amount = math.min(ship.GetAvailableCargoSpace(CargoBay.CargoType.Gas) +
                        ship.GetAllCargoOfType(CargoBay.CargoType.Gas), r.Value.amount);
                    if (amount == 0) return;
                    var offer = new FactionTrade.TradeOffer(r.Value, amount);
                    gasRequests.Add(new Tuple<float, Unit, FactionTrade.TradeOffer>(
                        amount * factionTrade.GetOurSellValueOfOffer(r.Key.faction, offer), r.Key, offer));
                }));
            gasRequests.Sort((a, b) => a.Item1.CompareTo(b.Item2));
            var chosenRequest = gasRequests.FirstOrDefault();
            if (chosenRequest != null) {
                command.supplierContract =
                    new FactionTrade.TradeContract(ship, chosenRequest.Item2, chosenRequest.Item3);
                factionTrade.AddContract(command.supplierContract);
            }
            currentCommandState = CommandType.Idle;

            SetMaxSpeed(command.maxSpeed);
            newCommand = false;
        }

        if (currentCommandState == CommandType.Move && Vector2.Distance(ship.GetPosition(), command.targetGasCloud.GetPosition()) < command.targetGasCloud.size) {
            // We must be at the gas cloud, start collecting gas
            currentCommandState = CommandType.CollectGas;
        } else if (currentCommandState == CommandType.Dock && ship.dockedStation == command.supplierContract.receiver) {
            // We must be at the receiver station, start unloading
            currentCommandState = CommandType.Wait;
            ((Station)command.supplierContract.receiver).contractShipsDocked
                .Add(command.supplierContract);
            return CommandResult.Stop;
        } else if (currentCommandState == CommandType.Wait) {
            if (!ship.faction.factionTrade.activeContracts.Contains(command.supplierContract)) {
                // We have unloaded all the cargo or the contract has been canceled
                command.supplierContract = null;
                currentCommandState = CommandType.Idle;
            }
            return CommandResult.Stop;
        }

        if (currentCommandState == CommandType.CollectGas && Vector2.Distance(ship.GetPosition(), command.targetGasCloud.GetPosition()) < command.targetGasCloud.size) {
            if (command.targetGasCloud.HasResources()) {
                foreach (GasCollector gasCollector in ship.moduleSystem.Get<GasCollector>()) {
                    if (gasCollector.CollectGas(command.targetGasCloud, deltaTime)) {
                        return CommandResult.Stop;
                    }
                }

                currentCommandState = CommandType.Idle;
                // If we don't have a contract stop and try to find a new one before going back to the fleet command.
                if (command.supplierContract == null)
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
            if ((command.supplierContract == null &&
                    ship.GetAllCargoOfType(CargoBay.CargoType.Gas) <
                    ship.GetAvailableCargoSpace(CargoBay.CargoType.Gas))
                || (command.supplierContract != null && ship.GetAllCargoOfType(CargoBay.CargoType.Gas) <
                    command.supplierContract.cargo[CargoBay.CargoType.Gas].amount)) {
                if (!command.targetGasCloud.HasResources())
                    command.targetGasCloud = ship.faction.GetClosestGasCloud(ship.GetPosition());
                if (command.targetGasCloud == null)
                    return CommandResult.StopRemove;
                command.targetPosition = command.targetGasCloud.GetPosition() + new Vector2(
                    ship.random.NextFloat(-command.targetGasCloud.size, command.targetGasCloud.size) / 2,
                    ship.random.NextFloat(-command.targetGasCloud.size, command.targetGasCloud.size) / 2);
                SetMovePosition(command.targetPosition, 2);
                currentCommandState = CommandType.Move;
            } else if (command.supplierContract != null) {
                SetDockTarget((Station)command.supplierContract.receiver);
                currentCommandState = CommandType.Dock;
            } else if (ship.dockedStation != ship.faction.GetFleetCommand() && ship.faction.GetFleetCommand() != null) {
                // We have enough cargo and no contracts
                SetDockTarget(ship.faction.GetFleetCommand());
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
                SetMaxSpeed(command.maxSpeed);
                ship.SetGroup(command.productionStation.GetGroup());
                newCommand = false;
            }

            if (ship.dockedStation == command.productionStation) {
                if (command.useAlternateCommandOnceDone && currentCommandState == CommandType.Move) {
                    currentCommandState = CommandType.Idle;
                    return CommandResult.StopRemove;
                }

                if (ship.GetAvailableCargoSpace(command.cargoType) <= 0) {
                    SetDockTarget(command.destinationStation);
                    currentCommandState = CommandType.Dock;
                } else {
                    ship.LoadCargoFromUnit(cargoTransferSpeed, command.cargoType, command.productionStation);
                    currentCommandState = CommandType.Wait;
                }
            } else if (ship.dockedStation == command.destinationStation) {
                if (ship.GetAllCargoOfType(command.cargoType) <= 0) {
                    SetDockTarget(command.productionStation);
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
                    SetDockTarget(command.destinationStation);
                    currentCommandState = CommandType.Dock;
                } else {
                    SetDockTarget(command.productionStation);
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
                SetMaxSpeed(command.maxSpeed);
                ship.SetGroup(command.productionStation.GetGroup());
                newCommand = false;
            }

            if (ship.dockedStation == command.productionStation) {
                if (ship.GetAllCargoOfType(command.cargoType) > 0) {
                    command.waitTime -= deltaTime;
                }

                if (ship.GetAvailableCargoSpace(command.cargoType) <= 0 || command.waitTime <= 0) {
                    SetDockTarget(command.destinationStation);
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
                    SetDockTarget(command.productionStation);
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
                    SetDockTarget(command.destinationStation);
                    currentCommandState = CommandType.Dock;
                } else {
                    SetDockTarget(command.productionStation);
                    currentCommandState = CommandType.Dock;
                }
            }
        }

        return CommandResult.Stop;
    }

    private CommandResult DoTradeTransportCommand(Command command, float deltaTime) {
        FactionTrade factionTrade = ship.faction.factionTrade;
        var activeContracts = factionTrade.activeContracts;

        // Check to make sure that the contracts are still valid
        if ((command.supplierContract != null &&
                !activeContracts.Contains(command.supplierContract) &&
                command.supplierContract.cargo.Count != 0) || (command.requestContract != null &&
                !activeContracts.Contains(command.requestContract) &&
                command.requestContract.cargo.Count != 0)) {

            if (command.supplierContract != null && activeContracts.Contains(command.supplierContract))
                factionTrade.RemoveContract(command.supplierContract);
            if (activeContracts.Contains(command.requestContract))
                factionTrade.RemoveContract(command.requestContract);

            command.supplierContract = null;
            command.requestContract = null;
        }
        if ((command.pickupContract != null && !activeContracts.Contains(command.pickupContract) &&
                command.pickupContract.transportOffer.personnel.TotalPopulation() != 0) ||
            (command.dropOffContract != null && !activeContracts.Contains(command.dropOffContract) &&
                command.dropOffContract.transportOffer.personnel.TotalPopulation() != 0)) {

            if (command.pickupContract != null && activeContracts.Contains(command.supplierContract))
                factionTrade.RemoveContract(command.supplierContract);
            if (command.dropOffContract != null)
                factionTrade.RemoveContract(command.dropOffContract);

            command.pickupContract = null;
            command.dropOffContract = null;
        }

        if (command.requestContract == null && command.dropOffContract == null)
            currentCommandState = CommandType.Idle;

        if (command.requestContract == null && command.dropOffContract == null) {
            // Finding a new trade route is an expensive operation if we can't find
            // a good route the first time we should wait a little before trying again
            if (command.waitTime > 0) {
                command.waitTime -= deltaTime;
                return CommandResult.Stop;
            }
            command.waitTime += .5f;

            // Try and find a new trade route
            var possibleTradeRoutes = new List<Tuple<FactionTrade.TradeContract, FactionTrade.TradeContract,
                FactionTrade.TransportContract, FactionTrade.TransportContract, float>>();
            foreach (Station station in factionTrade.GetFactionsWeCanBuyFrom().SelectMany(f => f.faction.stations)) {
                if (station == command.destinationStation || command.destinationStation == null) {
                    possibleTradeRoutes.AddRange(factionTrade.GetFactionsWeCanSellTo()
                        .SelectMany(f => f.faction.stations)
                        .Select(s => GetBestContractsBetweenStations(station, s))
                        .Where(tr => tr.Item5 > 0));
                } else if (command.destinationStation.faction == ship.faction ||
                    factionTrade.tradeSellAgreements.ContainsKey(command.destinationStation.faction)) {
                    var route = GetBestContractsBetweenStations(station, command.destinationStation);
                    if (route.Item5 > 0) possibleTradeRoutes.Add(route);
                }
            }
            if (!possibleTradeRoutes.Any()) return CommandResult.Stop;

            var bestTradeRoute = possibleTradeRoutes.Aggregate((a, b) => a.Item5 >= b.Item5 ? a : b);

            // Sign the contracts
            command.supplierContract = bestTradeRoute.Item1;
            if (command.supplierContract != null)
                factionTrade.AddContract(command.supplierContract,
                    !bestTradeRoute.Item1.cargo.ContainsKey(CargoBay.CargoType.Metal) ||
                    command.supplierContract.provider is not MiningStation);
            command.requestContract = bestTradeRoute.Item2;
            if (command.requestContract != null)
                factionTrade.AddContract(command.requestContract);
            command.pickupContract = bestTradeRoute.Item3;
            if (command.pickupContract != null)
                factionTrade.AddContract(command.pickupContract);
            command.dropOffContract = bestTradeRoute.Item4;
            if (command.dropOffContract != null)
                factionTrade.AddContract(command.dropOffContract);

            currentCommandState = CommandType.TradeTransport;
            newCommand = true;
            command.waitTime = 0;
        }

        if (ship.shipAction == Ship.ShipAction.Idle || newCommand) {
            newCommand = false;

            if (command.supplierContract != null || command.pickupContract != null) {
                Station provider = command.supplierContract != null
                    ? (Station)command.supplierContract.provider
                    : (Station)command.pickupContract.provider;
                // Collect the cargo
                if (ship.dockedStation != provider) {
                    SetDockTarget(provider);
                    SetMaxSpeed(command.maxSpeed);
                    currentCommandState = CommandType.Dock;
                    return CommandResult.Stop;
                } else if (!factionTrade.activeContracts.Contains(command.supplierContract) &&
                    !factionTrade.activeContracts.Contains(command.pickupContract)) {
                    command.supplierContract = null;
                    command.pickupContract = null;
                    currentCommandState = CommandType.Idle;
                } else if (ship.dockedStation == provider) {
                    //Add Contract to station to transfer cargo
                    if (currentCommandState != CommandType.Wait) {
                        if (command.supplierContract != null)
                            provider.contractShipsDocked.Add(command.supplierContract);
                        if (command.pickupContract != null) provider.contractShipsDocked.Add(command.pickupContract);
                        currentCommandState = CommandType.Wait;
                    }
                    return CommandResult.Stop;
                } else {
                    return CommandResult.Stop;
                }
            }
            if (command.requestContract != null || command.dropOffContract != null) {
                Station receiver = command.requestContract != null
                    ? (Station)command.requestContract.receiver
                    : (Station)command.dropOffContract.receiver;
                // Deliver the cargo
                if (ship.dockedStation != receiver) {
                    SetDockTarget(receiver);
                    SetMaxSpeed(command.maxSpeed);
                    currentCommandState = CommandType.Dock;
                    return CommandResult.Stop;
                } else if (!factionTrade.activeContracts.Contains(command.requestContract) &&
                    !factionTrade.activeContracts.Contains(command.dropOffContract)) {
                    command.requestContract = null;
                    command.dropOffContract = null;
                    currentCommandState = CommandType.Idle;
                } else if (ship.dockedStation == receiver) {
                    //Add Contract to station to transfer cargo
                    if (currentCommandState != CommandType.Wait) {
                        if (command.requestContract != null)
                            receiver.contractShipsDocked.Add(command.requestContract);
                        if (command.dropOffContract != null)
                            receiver.contractShipsDocked.Add(command.dropOffContract);
                        currentCommandState = CommandType.Wait;
                    }
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
            SetMovePosition(command.targetPlanet.position, command.targetPlanet.size + ship.size + 100);
            command.targetPosition = GetTargetMovePosition();
            SetMaxSpeed(command.maxSpeed);
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
            SetMovePosition(command.destinationStation.position, command.destinationStation.size + ship.size + 2);
            command.targetPosition = GetTargetMovePosition();
            SetMaxSpeed(command.maxSpeed);
            newCommand = false;
        }

        if (Vector2.Distance(ship.position, command.destinationStation.position) <=
            ship.size + command.destinationStation.size + 4) {
            if (!command.destinationStation.BuildStation())
                throw new InvalidProgramException("Trying to build an already built station!");
            command.destinationStation.moduleSystem.Get<MiningBay>().ForEach(m => m.FillEmployees());
            ship.Explode();
            return CommandResult.Stop;
        }

        return CommandResult.Stop;
    }

    #endregion

    #region HelperMethods

    private void SetDockTarget(Station targetStation) {
        foreach (ShipAISubprocess subprocess in subprocesses) {
            if (subprocess.OverrideSetDockTarget(targetStation)) return;
        }

        ship.SetDockTarget(targetStation);
    }

    private void SetMovePosition(Vector2 targetPosition) {
        foreach (ShipAISubprocess subprocess in subprocesses) {
            if (subprocess.OverrideSetMovePosition(targetPosition)) return;
        }
        ship.SetMovePosition(targetPosition);
    }

    private void SetMovePosition(Vector2 targetPosition, float distanceFromPosition) {
        targetPosition = Vector2.MoveTowards(ship.GetPosition(), targetPosition,
            Vector2.Distance(ship.GetPosition(), targetPosition) - distanceFromPosition);

        foreach (ShipAISubprocess subprocess in subprocesses) {
            if (subprocess.OverrideSetMovePosition(targetPosition)) return;
        }
        ship.SetMovePosition(targetPosition);
    }

    private void SetMaxSpeed(float maxSpeed) {
        foreach (ShipAISubprocess subprocess in subprocesses) {
            if (subprocess.OverrideSetMaxSpeed(maxSpeed)) return;
        }
        ship.SetMaxSpeed(maxSpeed);
    }

    private Vector2 GetTargetMovePosition() {
        foreach (ShipAISubprocess subprocess in subprocesses) {
            Vector2? targetPos = subprocess.OverrideGetTargetPosition();
            if (targetPos != null) return targetPos.Value;
        }
        return ship.GetTargetMovePosition();
    }

    public Tuple<FactionTrade.TradeContract, FactionTrade.TradeContract, FactionTrade.TransportContract,
        FactionTrade.TransportContract, float> GetBestContractsBetweenStations(Station origin, Station destination,
        bool includeCurrentCargo = true) {
        if (origin == destination) return new(null, null, null, null, 0);
        FactionTrade.TradeContract providerContract = null;
        FactionTrade.TradeContract requesterContract = null;
        FactionTrade.TransportContract toHireContract = null;
        FactionTrade.TransportContract toDeliverContract = null;
        float totalValue = 0f;
        var factionTrade = ship.faction.factionTrade;

        if (ship.moduleSystem.Get<CargoBay>().Any()) {
            // Get all cargo offered and requested from the stations and assign a value to them
            var cargoTradeTypes = new List<Tuple<FactionTrade.TradeOffer, FactionTrade.TradeOffer, float>>();
            CargoBay.allCargoTypes.ForEach(c => {
                if (origin.faction.factionTrade.resourcesOffered[c].ContainsKey(origin) &&
                    destination.faction.factionTrade.resourcesRequested[c].ContainsKey(destination)) {
                    var offer = origin.faction.factionTrade.resourcesOffered[c][origin];
                    var request = destination.faction.factionTrade.resourcesRequested[c][destination];
                    float value = factionTrade.GetOurSellValueOfOffer(destination.faction, request) -
                        factionTrade.GetOurBuyValueOfOffer(origin.faction, offer);
                    if (value > 0)
                        cargoTradeTypes.Add(new(offer, request, value));
                } else if (destination.faction.factionTrade.resourcesRequested[c].ContainsKey(destination)
                    && ship.GetAllCargoOfType(c) > 0) {
                    var request = destination.faction.factionTrade.resourcesRequested[c][destination];
                    cargoTradeTypes.Add(new(new FactionTrade.TradeOffer(c, 0, 0), request, request.price));
                }
            });
            // Sort them by most valuable first
            cargoTradeTypes = cargoTradeTypes.OrderByDescending(ct => ct.Item3).ToList();

            // Get all of our sizes of cargo bays along with how many we have
            var cargoBays = new List<Tuple<long, int>>();
            ship.moduleSystem.Get<CargoBay>().ForEach(cb => {
                int cargoBaysUsed = cb.GetMaxCargoBays();
                if (includeCurrentCargo)
                    cargoBaysUsed -= cb.GetCargoBaysUsed();
                if (cargoBays.Any(c => c.Item1 == cb.GetCargoBayCapacity())) {
                    int index = cargoBays.FindIndex(c => c.Item1 == cb.GetCargoBayCapacity());
                    cargoBays[index] = new(cargoBays[index].Item1,
                        cargoBays[index].Item2 + cargoBaysUsed);
                } else {
                    cargoBays.Add(new(cb.GetCargoBayCapacity(), cargoBaysUsed));
                }
            });
            // Sort them by largest capacity descending
            cargoBays.Sort((a, b) => b.Item1.CompareTo(a.Item1));

            // Some cargo bays might be half filled if we are including the current cargo
            // This is space that can only be used for that cargo type
            Dictionary<CargoBay.CargoType, long> halfFilledCargo = new();
            if (includeCurrentCargo) {
                ship.moduleSystem.Get<CargoBay>().ForEach(cb => {
                    foreach (var bay in cb.cargoBays) {
                        long openSpace = bay.Value % cb.GetCargoBayCapacity();
                        if (openSpace == 0) return;
                        if (halfFilledCargo.ContainsKey(bay.Key)) {
                            halfFilledCargo[bay.Key] += openSpace;
                        } else {
                            halfFilledCargo.Add(bay.Key, openSpace);
                        }
                    }
                });
            }

            var contractCargo = new List<Tuple<FactionTrade.TradeOffer, FactionTrade.TradeOffer>>();
            // Fill up the contract cargo with most valuable cargo first
            foreach (var typeToLoad in cargoTradeTypes) {
                long amountToLoad = typeToLoad.Item2.amount;
                long totalAmount = 0;
                CargoBay.CargoType cargoType = typeToLoad.Item1.cargoType;

                long previousCargo = 0;
                if (includeCurrentCargo) {
                    // Account for any cargo that we already have
                    previousCargo = math.min(amountToLoad, ship.GetAllCargoOfType(cargoType));
                    amountToLoad -= previousCargo;
                    totalValue += previousCargo * typeToLoad.Item2.price;

                    if (halfFilledCargo.TryGetValue(cargoType, out long cargo)) {
                        // The first priority goes to filling half full cargo bays
                        totalAmount += math.min(cargo, amountToLoad);
                        amountToLoad -= math.min(cargo, amountToLoad);
                    }
                }

                long providerAmount = typeToLoad.Item1.amount;
                if (origin is MiningStation miningStation &&
                    miningStation.moduleSystem.Get<MiningBay>().Any(b => b.activelyMining))
                    providerAmount = long.MaxValue;
                amountToLoad = math.min(providerAmount, amountToLoad);

                for (int i = 0; i < cargoBays.Count; i++) {
                    int cargoBaysFullyFilled = math.min(cargoBays[i].Item2, (int)(amountToLoad / cargoBays[i].Item1));
                    amountToLoad -= cargoBaysFullyFilled * cargoBays[i].Item1;
                    totalAmount += cargoBaysFullyFilled * cargoBays[i].Item1;
                    if (cargoBays[i].Item2 - cargoBaysFullyFilled == 0) {
                        cargoBays.RemoveAt(i);
                        i--;
                    } else {
                        cargoBays[i] = new(cargoBays[i].Item1, cargoBays[i].Item2 - cargoBaysFullyFilled);
                    }
                }
                if (amountToLoad > 0 && cargoBays.Count != 0) {
                    // In this case there must be at least one cargo bay left and all cargo bays have a capacity
                    // higher than totalAmount
                    totalAmount += amountToLoad;
                    amountToLoad = 0;
                    if (cargoBays[^1].Item2 != 1) {
                        cargoBays[^1] = new(cargoBays[^1].Item1, cargoBays[^1].Item2 - 1);
                    } else {
                        cargoBays.RemoveAt(cargoBays.Count - 1);
                    }
                }
                totalValue += totalAmount * typeToLoad.Item3;
                if (totalAmount + previousCargo == 0) continue;
                contractCargo.Add(new(new FactionTrade.TradeOffer(typeToLoad.Item1, totalAmount),
                    new FactionTrade.TradeOffer(typeToLoad.Item2, totalAmount + previousCargo)));
            }

            if (contractCargo.Any(cc => cc.Item1.amount > 0))
                providerContract = new FactionTrade.TradeContract(origin, ship,
                    contractCargo.Select(cc => cc.Item1).Where(c => c.amount > 0)
                        .ToArray());
            if (contractCargo.Any(cc => cc.Item2.amount > 0))
                requesterContract = new FactionTrade.TradeContract(ship, destination,
                    contractCargo.Select(cc => cc.Item2).Where(c => c.amount > 0).ToArray());
        }

        if (ship.moduleSystem.Get<HabitationArea>().Any(h => h.IsTransferHabitat()) &&
            (origin.faction.factionTrade.personnelToHire.TryGetValue(origin,
                    out FactionTrade.TransportOffer hireOffer) &&
                destination.faction.factionTrade.personnelRequested.TryGetValue(destination,
                    out FactionTrade.TransportOffer requestOffer))) {

            long openCapacity = ship.moduleSystem.Get<HabitationArea>()
                .Sum(h => includeCurrentCargo ? h.GetFreeSpace() : h.GetCapacity());
            var occupationValueAmount = new List<Tuple<Occupation, float, long, long>>();
            HabitationArea.allOccupations.ForEach(o => {
                if (hireOffer.personnel.Get(o) > 0 && requestOffer.personnel.Get(o) > 0) {
                    float value = factionTrade.GetOurSellValueOfOffer(origin.faction, requestOffer.payment.Get(o)) -
                        factionTrade.GetOurBuyValueOfOffer(destination.faction, hireOffer.payment.Get(o));
                    if (value <= 0) return;
                    occupationValueAmount.Add(new(o, value, hireOffer.personnel.Get(o), requestOffer.personnel.Get(o)));
                }
            });
            var contractPersonnel = new Population();
            // Holds personnel that are already picked up
            var currentContractPersonnel = new Population();

            occupationValueAmount.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            foreach (var occupationTransport in occupationValueAmount) {
                Occupation occupation = occupationTransport.Item1;
                long previousPopulation = 0;
                if (includeCurrentCargo) {
                    previousPopulation = math.min(ship.moduleSystem.Get<HabitationArea>()
                            .Sum(h => h.population.Get(occupation)),
                        occupationTransport.Item4);
                    currentContractPersonnel.Add(occupation, previousPopulation);
                }

                long toAdd = math.min(openCapacity,
                    math.min(occupationTransport.Item3, occupationTransport.Item4 - previousPopulation));
                openCapacity -= toAdd;
                totalValue += toAdd * occupationTransport.Item2;
                totalValue += previousPopulation * requestOffer.payment.Get(occupation);
                contractPersonnel.Add(occupation, toAdd);
            }

            if (contractPersonnel.TotalPopulation() > 0)
                toHireContract = new(origin, ship,
                    new(new Population(contractPersonnel),
                        origin.faction.factionTrade.personnelToHire[origin].payment));
            contractPersonnel.AddPopulation(currentContractPersonnel);
            if (contractPersonnel.TotalPopulation() > 0)
                toDeliverContract = new(ship, destination,
                    new(new Population(contractPersonnel),
                        destination.faction.factionTrade.personnelRequested[destination].payment));
        }

        return new(providerContract, requesterContract, toHireContract, toDeliverContract, totalValue);
    }

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
                if (currentCommandState == CommandType.Move)
                    positions.Add(command.targetPosition);
                if (command.supplierContract == null || command.supplierContract.receiver == null) continue;
                positions.Add(command.supplierContract.receiver.GetPosition());
            } else if (command.commandType == CommandType.Colonize) {
                if (command.targetPlanet == null) continue;
                positions.Add(Vector2.MoveTowards(ship.GetPosition(), command.targetPlanet.GetPosition(),
                    Vector2.Distance(ship.GetPosition(), command.targetPlanet.GetPosition()) -
                    (ship.GetSize() + command.targetPlanet.GetSize() + 100)));
            } else if (command.commandType is CommandType.Idle or CommandType.Wait
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
            } else if (command.commandType == CommandType.TradeTransport) {
                if (command.supplierContract != null)
                    positions.Add(command.supplierContract.provider.position);
                else if (command.pickupContract != null)
                    positions.Add(command.pickupContract.provider.position);
                if (command.requestContract != null)
                    positions.Add(command.requestContract.receiver.position);
                else if (command.dropOffContract != null)
                    positions.Add(command.dropOffContract.receiver.position);
            } else {
                if (command.targetPosition == null) continue;
                positions.Add(command.targetPosition);
            }
        }

        return positions;
    }

    #endregion
}
