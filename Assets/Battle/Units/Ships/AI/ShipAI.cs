using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using static Command;

public class ShipAI : MonoBehaviour {
    enum CommandResult {
        Stop = 0,
        StopRemove = 1,
        ContinueRemove = 2,
        Continue = 3,
    }

    Ship ship;

    public List<Command> commands;
    public bool newCommand;
    public CommandType currentCommandType;

    public void SetupShipAI(Ship ship) {
        this.ship = ship;
        commands = new List<Command>(10);
    }

    public void AddUnitAICommand(Command command, CommandAction commandAction = CommandAction.AddToEnd) {
        if ((command.commandType == CommandType.AttackMove || command.commandType == CommandType.AttackMoveUnit || command.commandType == CommandType.Protect) && ship.GetTurrets().Count == 0) {
            return;
        }
        if (commandAction == CommandAction.AddToBegining) {
            newCommand = true;
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
            commands.RemoveAt(0);
            newCommand = true;
        }
    }

    public void ClearCommands() {
        commands.Clear();
        ship.SetIdle();
    }

    public void UpdateAI(float deltaTime) {
        if (commands.Count > 0) {
            Profiler.BeginSample("ShipAI ResolveCommand");
            CommandResult result = ResolveCommand(commands[0], deltaTime, 0);
            if (result == CommandResult.StopRemove || result == CommandResult.ContinueRemove) {
                commands.RemoveAt(0);
                newCommand = true;
            }
            if (result == CommandResult.ContinueRemove || result == CommandResult.Continue)
                UpdateAI(deltaTime);
            Profiler.EndSample();
        }
    }

    CommandResult ResolveCommand(Command command, float deltaTime, int index = -1) {
        //Idles until something removes this command.
        if (command.commandType == CommandType.Idle) {
            if (newCommand) {
                currentCommandType = CommandType.Idle;
                ship.SetIdle();
                newCommand = false;
            }
            return CommandResult.Stop;
        }
        //Waits for a certain amount of time, Stop until the time is up,  ContinueRemove once finished.
        if (command.commandType == CommandType.Wait) {
            if (newCommand) {
                currentCommandType = CommandType.Wait;
                ship.SetIdle();
                newCommand = false;
            }
            commands[index].waitTime -= deltaTime;
            if (index == -1 || command.waitTime <= 0) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Rotates towards angle, Stop until turned to rotation, ContinueRemove once Finished
        if (command.commandType == CommandType.TurnToRotation) {
            if (newCommand) {
                currentCommandType = CommandType.TurnToRotation;
                ship.SetTargetRotate(command.targetRotation);
                newCommand = false;
            }
            if (ship.shipAction == Ship.ShipAction.Idle) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Rptates towards position, Stop until turned to angle, ContinueRemove once Finished.
        if (command.commandType == CommandType.TurnToPosition) {
            if (newCommand) {
                currentCommandType = CommandType.TurnToPosition;
                ship.SetTargetRotate(command.targetPosition);
                newCommand = false;
            }
            if (ship.shipAction == Ship.ShipAction.Idle) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Rotates towards position then moves towards position, Stop until moved to postion, ContinueRemoveOnce Finished.
        if (command.commandType == CommandType.Move) {
            if (newCommand) {
                currentCommandType = CommandType.Move;
                ship.SetMovePosition(command.targetPosition);
                ship.SetMaxSpeed(command.maxSpeed);
                newCommand = false;
            }

            if (ship.shipAction == Ship.ShipAction.Idle && currentCommandType == CommandType.Move) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Follows closest enemy ship then goes to target position, Stop until all nearby enemy ships are removed and at target position, ContinueRemove once Finished.
        //Follows closest enemy ship then follows freindly ship, Stop until friendly ship is destroyed, Creates an attackMoveCommand on current position once the friendly ship is destroyed.
        if (command.commandType == CommandType.AttackMove || command.commandType == CommandType.Protect) {
            if (command.commandType == CommandType.Protect && command.protectUnit == null) {
                command.commandType = CommandType.AttackMove;
                command.protectUnit = null;
            }

            float distanceToTargetUnit = 0;
            if (command.targetUnit != null)
                distanceToTargetUnit = Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition());

            //If there is a targetUnit check if a new one should be calculated
            if (currentCommandType != CommandType.Move && (command.targetUnit == null || !command.targetUnit.IsSpawned() || distanceToTargetUnit > ship.GetMaxWeaponRange() * 2 || (distanceToTargetUnit > ship.GetMaxWeaponRange() && command.targetUnit != GetClosestNearbyEnemyUnit()))) {
                newCommand = true;
                command.targetUnit = null;
            }
            if (newCommand) {
                if (command.commandType == CommandType.Protect) {
                    ship.SetMovePosition(command.protectUnit.GetPosition(), (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                    command.targetPosition = ship.GetTargetMovePosition();
                } else {
                    ship.SetMovePosition(command.targetPosition);
                }
                currentCommandType = CommandType.Move;
                ship.SetMaxSpeed(command.maxSpeed);
                newCommand = false;
            }

            if (currentCommandType == CommandType.Move) {
                command.targetUnit = GetClosestNearbyEnemyUnit();
                if (command.targetUnit != null) {
                    distanceToTargetUnit = Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition());
                    if (distanceToTargetUnit < ship.GetMinWeaponRange()) {
                        currentCommandType = CommandType.TurnToRotation;
                    } else {
                        currentCommandType = CommandType.AttackMove;
                        ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    }
                } else if (ship.shipAction == Ship.ShipAction.Idle) {
                    if (command.commandType == CommandType.Protect) {
                        if (Vector2.Distance(ship.GetPosition(), command.protectUnit.GetPosition()) > (ship.GetSize() + command.protectUnit.GetSize()) * 3)
                            ship.SetMovePosition(command.protectUnit.GetPosition(), (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                        return CommandResult.Stop;
                    }
                    return CommandResult.ContinueRemove;
                } else {
                    if (command.commandType == CommandType.Protect) {
                        ship.SetMovePosition(command.protectUnit.GetPosition(), (ship.GetSize() + command.protectUnit.GetSize()) * 2);
                        command.targetPosition = ship.GetTargetMovePosition();
                    }
                    return CommandResult.Stop;
                }
            }

            if (currentCommandType == CommandType.AttackMove) {
                if (ship.shipAction == Ship.ShipAction.Idle || distanceToTargetUnit <= ship.GetMinWeaponRange() * .8f) {
                    currentCommandType = CommandType.TurnToRotation;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    return CommandResult.Stop;
                }
            }

            if (currentCommandType == CommandType.TurnToRotation) {
                if (distanceToTargetUnit > ship.GetMinWeaponRange()) {
                    currentCommandType = CommandType.AttackMove;
                } else {
                    ship.SetTargetRotate(command.targetUnit.GetPosition(), ship.GetCombatRotation());
                }
            }
            return CommandResult.Stop;
        }
        //Follows enemy ship, Stop until enemy ship is destroyed, ContinueRemove once Finished.
        if (command.commandType == CommandType.AttackMoveUnit) {
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
                currentCommandType = CommandType.Move;
                ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                command.targetPosition = command.targetUnit.GetPosition();
                newCommand = false;
            }

            if (currentCommandType == CommandType.Move) {
                if (ship.shipAction == Ship.ShipAction.Idle || Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) <= ship.GetMinWeaponRange() * .8f) {
                    currentCommandType = CommandType.TurnToRotation;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    return CommandResult.Stop;
                }
            }

            if (currentCommandType == CommandType.TurnToRotation) {
                if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMinWeaponRange()) {
                    currentCommandType = CommandType.Move;
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                } else {
                    ship.SetTargetRotate(command.targetUnit.GetPosition(), ship.GetCombatRotation());
                }
            }
            return CommandResult.Stop;
        }
        //Follows friendly ship, Continue until friendly ship is destroyed, ContinueRemove once Finished.
        if (command.commandType == CommandType.Follow) {
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
        //Follows the friendly ship in a formation, Continue until friendly ship formation leader is destroyed, ContinueRemove once Finished.
        if (command.commandType == CommandType.Formation) {
            if (command.targetUnit == null) {
                return CommandResult.ContinueRemove;
            }
            float distance = Vector2.Distance(ship.transform.position, (Vector2)command.targetUnit.transform.position + command.targetPosition);
            if (distance > ship.GetTurnSpeed() * deltaTime / 10) {
                CommandResult result = ResolveCommand(CreateMoveCommand(Vector3.MoveTowards(ship.transform.position, (Vector2)command.targetUnit.transform.position + command.targetPosition, distance)), deltaTime);
                if (result == CommandResult.ContinueRemove || result == CommandResult.Continue) {
                    return CommandResult.Continue;
                }
            }
            transform.position = (Vector2)command.targetUnit.transform.position + command.targetPosition;
            return CommandResult.Continue;
        }
        //Follows the friendly ship in a formation relative to thier rotation, Continue until friendly ship formation leader is destroyed, ContinueRemove once Finished.
        if (command.commandType == CommandType.FormationLocation) {
            if (command.targetUnit == null) {
                return CommandResult.ContinueRemove;
            }

            float targetAngle = command.targetRotation - command.targetUnit.GetRotation();
            float distanceToTargetAngle = Calculator.GetDistanceToPosition(command.targetPosition);
            Vector2 targetOffsetPosition = Calculator.GetPositionOutOfAngleAndDistance(targetAngle + Calculator.GetAngleOutOfPosition(command.targetPosition), distanceToTargetAngle);
            float distance = Vector2.Distance(ship.transform.position, (Vector2)command.targetUnit.transform.position + targetOffsetPosition);
            if (distance > ship.GetThrust() * deltaTime / 10) {
                CommandResult result = ResolveCommand(CreateMoveCommand((Vector2)command.targetUnit.transform.position + targetOffsetPosition), deltaTime);
                if (result == CommandResult.Stop || result == CommandResult.StopRemove) {
                    return CommandResult.Stop;
                }
            }
            ship.transform.position = (Vector2)command.targetUnit.transform.position + targetOffsetPosition;
            CommandResult rotationResult = ResolveCommand(CreateRotationCommand(command.targetUnit.GetRotation()), deltaTime);
            if (rotationResult == CommandResult.ContinueRemove || rotationResult == CommandResult.Continue) {
                return CommandResult.Continue;
            }
            return CommandResult.Stop;
        }
        //Goes to then docks at the station.
        if (command.commandType == CommandType.Dock) {
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
            if (command.destinationStation == null || (ship.shipAction == Ship.ShipAction.Idle && ship.dockedStation == command.destinationStation)) {
                ship.SetIdle();
                return CommandResult.StopRemove;
            }
            return CommandResult.Stop;
        }
        //AttackMove to the star, do reasearch, then remove command.
        if (command.commandType == CommandType.Research) {
            if (command.targetStar == null || command.destinationStation == null) {
                return CommandResult.StopRemove;
            }
            if (newCommand) {
                if (ship.GetResearchEquiptment().WantMoreData()) {
                    ship.SetMovePosition(command.targetStar.GetPosition(), ship.GetSize() + command.targetStar.GetSize() * 2);
                    currentCommandType = CommandType.Move;
                } else {
                    ship.SetDockTarget(command.destinationStation);
                    currentCommandType = CommandType.Dock;
                }
                ship.SetMaxSpeed(command.maxSpeed);
                newCommand = false;
            }
            if (ship.shipAction == Ship.ShipAction.Idle) {
                if (currentCommandType == CommandType.Move) {
                    currentCommandType = CommandType.Research;
                    return CommandResult.Stop;
                } else if (currentCommandType == CommandType.Research) {
                    ship.GetResearchEquiptment().GatherData(command.targetStar, deltaTime);
                    if (!ship.GetResearchEquiptment().WantMoreData()) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = CommandType.Dock;
                    }
                    return CommandResult.Stop;
                } else if (currentCommandType == CommandType.Dock) {
                    currentCommandType = CommandType.Wait;
                    return CommandResult.Stop;
                } else if (currentCommandType == CommandType.Wait) {
                    if (ship.GetResearchEquiptment().WantMoreData()) {
                        ship.SetMovePosition(command.targetStar.GetPosition(), ship.GetSize() + command.targetStar.GetSize() * 2);
                        currentCommandType = CommandType.Move;
                    }
                    return CommandResult.Stop;
                }
            }
            return CommandResult.Stop;
        }
        if (command.commandType == CommandType.Transport) {
            if (command.destinationStation == null || !command.destinationStation.IsSpawned()) {
                return CommandResult.StopRemove;
            }
            if (command.productionStation == null || !command.destinationStation.IsSpawned()) {
                return CommandResult.StopRemove;
            }
            if (ship.dockedStation != null || newCommand) {
                if (newCommand) {
                    currentCommandType = CommandType.Transport;
                    ship.SetMaxSpeed(command.maxSpeed);
                    newCommand = false;
                }
                if (ship.dockedStation == command.productionStation) {
                    if (command.useAlternateCommandOnceDone && currentCommandType == CommandType.Move) {
                        currentCommandType = CommandType.Idle;
                        return CommandResult.StopRemove;
                    }
                    if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = CommandType.Dock;
                    } else {
                        currentCommandType = CommandType.Wait;
                    }
                } else if (ship.dockedStation == command.destinationStation) {
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.SetDockTarget(command.productionStation);
                        if (command.useAlternateCommandOnceDone)
                            currentCommandType = CommandType.Move;
                        else
                            currentCommandType = CommandType.Dock;
                    } else {
                        currentCommandType = CommandType.Wait;
                    }
                } else {
                    if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = CommandType.Dock;
                    } else {
                        ship.SetDockTarget(command.productionStation);
                        currentCommandType = CommandType.Dock;
                    }
                }
            }
            return CommandResult.Stop;
        }
        if (command.commandType == CommandType.TransportDelay) {
            if (command.destinationStation == null || !command.destinationStation.IsSpawned()) {
                return CommandResult.StopRemove;
            }
            if (command.productionStation == null || !command.destinationStation.IsSpawned()) {
                return CommandResult.StopRemove;
            }
            if (ship.dockedStation != null || newCommand) {
                if (newCommand) {
                    currentCommandType = CommandType.Transport;
                    ship.SetMaxSpeed(command.maxSpeed);
                    newCommand = false;
                }
                if (ship.dockedStation == command.productionStation) {
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) > 0) {
                        command.waitTime -= deltaTime;
                    }
                    if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0 || command.waitTime <= 0) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = CommandType.Dock;
                        command.waitTime = command.targetRotation;
                    } else {
                        currentCommandType = CommandType.Wait;
                    }
                } else if (ship.dockedStation == command.destinationStation) {
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) > 0) {
                        command.waitTime -= deltaTime;
                    }
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) <= 0 || command.waitTime <= 0) {
                        ship.SetDockTarget(command.productionStation);
                        currentCommandType = CommandType.Dock;
                        command.waitTime = command.targetRotation;
                    } else {
                        currentCommandType = CommandType.Wait;
                    }
                } else {
                    command.waitTime = command.targetRotation;
                    if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = CommandType.Dock;
                    } else {
                        ship.SetDockTarget(command.productionStation);
                        currentCommandType = CommandType.Dock;
                    }
                }
                return CommandResult.Stop;
            }
        }
        return CommandResult.Stop;
    }

    Unit GetClosestNearbyEnemyUnit() {
        Unit targetUnit = null;
        float distance = 0;
        for (int i = 0; i < ship.GetEnemyUnitsInRange().Count; i++) {
            Unit tempUnit = ship.GetEnemyUnitsInRange()[i];
            float tempDistance = Vector2.Distance(ship.transform.position, tempUnit.transform.position);
            if (targetUnit == null || tempDistance < distance) {
                targetUnit = tempUnit;
                distance = tempDistance;
            }
        }
        return targetUnit;
    }


    Unit GetClosestEnemyUnitInRadius(float radius) {
        Unit targetUnit = null;
        float distance = 0;
        for (int i = 0; i < ship.GetEnemyUnitsInRange().Count; i++) {
            Unit tempUnit = ship.GetEnemyUnitsInRange()[i];
            float tempDistance = Vector2.Distance(ship.transform.position, tempUnit.transform.position);
            if (tempDistance <= radius && (targetUnit == null || tempDistance < distance)) {
                targetUnit = tempUnit;
                distance = tempDistance;
            }
        }
        return targetUnit;
    }

    Ship GetClosestEnemyShipInRadius(float radius) {
        Ship targetUnit = null;
        float distance = 0;
        for (int i = 0; i < ship.faction.enemyFactions.Count; i++) {
            Faction faction = ship.faction.enemyFactions[i];
            for (int f = 0; f < faction.ships.Count; f++) {
                Ship tempShip = faction.ships[f];
                float tempDistance = Vector2.Distance(ship.transform.position, tempShip.transform.position);
                if (tempDistance <= radius && (targetUnit == null || tempDistance < distance)) {
                    targetUnit = tempShip;
                    distance = tempDistance;
                }
            }
        }
        return targetUnit;
    }
}
