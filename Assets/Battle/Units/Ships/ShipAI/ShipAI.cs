using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class ShipAI : MonoBehaviour {
    enum CommandResult {
        Stop = 0,
        StopRemove = 1,
        ContinueRemove = 2,
        Continue = 3,
    }

    public enum CommandAction {
        AddToBegining = -1,
        Replace = 0,
        AddToEnd = 1,
    }

    Ship ship;

    public List<UnitAICommand> commands;
    public bool newCommand;
    public UnitAICommand.CommandType currentCommandType;

    public void SetupShipAI(Ship ship) {
        this.ship = ship;
        commands = new List<UnitAICommand>(10);
    }

    public void AddUnitAICommand(UnitAICommand command, CommandAction commandAction = CommandAction.AddToEnd) {
        if ((command.commandType == UnitAICommand.CommandType.AttackMove || command.commandType == UnitAICommand.CommandType.AttackMoveUnit || command.commandType == UnitAICommand.CommandType.Protect) && ship.GetTurrets().Count == 0) {
            return;
        }
        if (commandAction == CommandAction.AddToBegining) {
            newCommand = true;
            commands.Insert(0, command);
        }
        if (commandAction == CommandAction.Replace) {
            newCommand = true;
            ClearCommands();
            commands.Add(command);
        }
        if (commandAction == CommandAction.AddToEnd) {
            if (commands.Count == 0)
                newCommand = true;
            commands.Add(command);
        }
    }

    public void NextCommand() {
        if (commands.Count > 0)
            commands.RemoveAt(0);
    }

    public void ClearCommands() {
        commands.Clear();
    }

    public void UpdateAI(float deltaTime) {
        if (commands.Count > 0) {
            Profiler.BeginSample("ShipAI ResolveCommand " + commands[0].commandType);
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

    CommandResult ResolveCommand(UnitAICommand command, float deltaTime, int index = -1) {
        //Idles until something removes this command.
        if (command.commandType == UnitAICommand.CommandType.Idle) {
            if (newCommand) {
                currentCommandType = UnitAICommand.CommandType.Idle;
                ship.SetIdle();
                newCommand = false;
            }
            return CommandResult.Stop;
        }
        //Waits for a certain amount of time, Stop until the time is up,  ContinueRemove once finished.
        if (command.commandType == UnitAICommand.CommandType.Wait) {
            if (newCommand) {
                currentCommandType = UnitAICommand.CommandType.Wait;
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
        if (command.commandType == UnitAICommand.CommandType.TurnToRotation) {
            if (newCommand) {
                currentCommandType = UnitAICommand.CommandType.TurnToRotation;
                ship.SetTargetRotate(command.targetRotation);
                newCommand = false;
            }
            if (ship.shipAction == Ship.ShipAction.Idle) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Rptates towards position, Stop until turned to angle, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.TurnToPosition) {
            if (newCommand) {
                currentCommandType = UnitAICommand.CommandType.TurnToPosition;
                ship.SetTargetRotate(command.targetPosition);
                newCommand = false;
            }
            if (ship.shipAction == Ship.ShipAction.Idle) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Rotates towards position then moves towards position, Stop until moved to postion, ContinueRemoveOnce Finished.
        if (command.commandType == UnitAICommand.CommandType.Move) {
            if (newCommand) {
                currentCommandType = UnitAICommand.CommandType.Move;
                ship.SetMovePosition(command.targetPosition);
                newCommand = false;
            }

            if (ship.shipAction == Ship.ShipAction.Idle && currentCommandType == UnitAICommand.CommandType.Move) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Follows closest enemy ship then goes to target position, Stop until all nearby enemy ships are removed and at target position, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.AttackMove) {
            if (currentCommandType != UnitAICommand.CommandType.Move && (command.targetUnit == null || !command.targetUnit.IsSpawned() || Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMaxWeaponRange() * 2)) {
                newCommand = true;
                command.targetUnit = null;
            }
            if (newCommand) {
                ship.SetMovePosition(command.targetPosition);
                currentCommandType = UnitAICommand.CommandType.Move;
                newCommand = false;
            }

            if (currentCommandType == UnitAICommand.CommandType.Move) {
                command.targetUnit = GetClosestEnemyUnitInRadius(ship.GetMaxWeaponRange() * 2);
                if (command.targetUnit != null) {
                    if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) < ship.GetMinWeaponRange()) {
                        currentCommandType = UnitAICommand.CommandType.TurnToRotation;
                    } else {
                        currentCommandType = UnitAICommand.CommandType.AttackMove;
                        ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    }
                } else if (ship.shipAction == Ship.ShipAction.Idle) {
                    return CommandResult.ContinueRemove;
                } else {
                    return CommandResult.Stop;
                }
            }

            if (currentCommandType == UnitAICommand.CommandType.AttackMove) {
                if (ship.shipAction == Ship.ShipAction.Idle || Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) <= ship.GetMinWeaponRange() * .8f) {
                    currentCommandType = UnitAICommand.CommandType.TurnToRotation;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    return CommandResult.Stop;
                }
            }

            if (currentCommandType == UnitAICommand.CommandType.TurnToRotation) {
                if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMinWeaponRange()) {
                    currentCommandType = UnitAICommand.CommandType.AttackMove;
                } else {
                    ship.SetTargetRotate(command.targetUnit.GetPosition(), ship.GetCombatRotation());
                }
            }
            return CommandResult.Stop;
        }
        //Follows enemy ship, Stop until enemy ship is destroyed, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.AttackMoveUnit) {
            if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
                command.commandType = UnitAICommand.CommandType.AttackMove;
                if (newCommand) {
                    command.targetPosition = ship.GetPosition();
                }
                newCommand = true;
                return CommandResult.Stop;
            }
            if (newCommand) {
                currentCommandType = UnitAICommand.CommandType.Move;
                ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                command.targetPosition = command.targetUnit.GetPosition();
                newCommand = false;
            }

            if (currentCommandType == UnitAICommand.CommandType.Move) {
                if (ship.shipAction == Ship.ShipAction.Idle || Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) <= ship.GetMinWeaponRange() * .8f) {
                    currentCommandType = UnitAICommand.CommandType.TurnToRotation;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                    return CommandResult.Stop;
                }
            }

            if (currentCommandType == UnitAICommand.CommandType.TurnToRotation) {
                if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMinWeaponRange()) {
                    currentCommandType = UnitAICommand.CommandType.Move;
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
                } else {
                    ship.SetTargetRotate(command.targetUnit.GetPosition(), ship.GetCombatRotation());
                }
            }
            return CommandResult.Stop;
        }
        //Follows friendly ship, Continue until friendly ship is destroyed, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.Follow) {
            if (newCommand) {
                ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
                newCommand = false;
                return CommandResult.Stop;
            }
            if (command.targetUnit == null) {
                ship.SetIdle();
                return CommandResult.ContinueRemove;
            }
            ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
            return CommandResult.Continue;
        }
        //Follows closest enemy ship then follows freindly ship, Stop until friendly ship is destroyed, Creates an attackMoveCommand on current position once the friendly ship is destroyed.
        if (command.commandType == UnitAICommand.CommandType.Protect) {
            if (command.targetUnit == null) {
                commands[index] = new UnitAICommand(UnitAICommand.CommandType.AttackMove, ship.transform.position);
                return CommandResult.Stop;
            }
            float distance = Vector2.Distance(ship.transform.position, command.targetUnit.transform.position) - (ship.GetSize() + command.targetUnit.GetSize());
            CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.AttackMove, Vector3.MoveTowards(ship.transform.position, command.targetUnit.transform.position, distance)), deltaTime, index);
            if (result == CommandResult.ContinueRemove || result == CommandResult.StopRemove) {
                ship.SetThrusters(false);
                return CommandResult.Continue;
            }
            ship.SetThrusters(false);
            return CommandResult.Continue;
        }
        //Follows the friendly ship in a formation, Continue until friendly ship formation leader is destroyed, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.Formation) {
            if (command.targetUnit == null) {
                return CommandResult.ContinueRemove;
            }
            float distance = Vector2.Distance(ship.transform.position, (Vector2)command.targetUnit.transform.position + command.targetPosition);
            if (distance > ship.GetTurnSpeed() * deltaTime / 10) {
                CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.Move, Vector3.MoveTowards(ship.transform.position, (Vector2)command.targetUnit.transform.position + command.targetPosition, distance)), deltaTime);
                if (result == CommandResult.ContinueRemove || result == CommandResult.Continue) {
                    return CommandResult.Continue;
                }
            }
            transform.position = (Vector2)command.targetUnit.transform.position + command.targetPosition;
            return CommandResult.Continue;
        }
        //Follows the friendly ship in a formation relative to thier rotation, Continue until friendly ship formation leader is destroyed, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.FormationRotation) {
            if (command.targetUnit == null) {
                return CommandResult.ContinueRemove;
            }

            float targetAngle = command.targetRotation - command.targetUnit.GetRotation();
            float distanceToTargetAngle = Calculator.GetDistanceToPosition(command.targetPosition);
            Vector2 targetOffsetPosition = Calculator.GetPositionOutOfAngleAndDistance(targetAngle + Calculator.GetAngleOutOfPosition(command.targetPosition), distanceToTargetAngle);
            float distance = Vector2.Distance(ship.transform.position, (Vector2)command.targetUnit.transform.position + targetOffsetPosition);
            if (distance > ship.GetThrust() * deltaTime / 10) {
                CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.Move, (Vector2)command.targetUnit.transform.position + targetOffsetPosition), deltaTime);
                if (result == CommandResult.Stop || result == CommandResult.StopRemove) {
                    return CommandResult.Stop;
                }
            }
            ship.transform.position = (Vector2)command.targetUnit.transform.position + targetOffsetPosition;
            CommandResult rotationResult = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.TurnToRotation, command.targetUnit.GetRotation()), deltaTime);
            if (rotationResult == CommandResult.ContinueRemove || rotationResult == CommandResult.Continue) {
                return CommandResult.Continue;
            }
            return CommandResult.Stop;
        }
        //Goes to then docks at the station.
        if (command.commandType == UnitAICommand.CommandType.Dock) {
            if (newCommand) {
                if (command.destinationStation != null) {
                    ship.SetDockTarget(command.destinationStation);
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
        if (command.commandType == UnitAICommand.CommandType.Research) {
            if (command.targetStar == null || command.destinationStation == null) {
                return CommandResult.StopRemove;
            }
            if (newCommand) {
                if (ship.GetResearchEquiptment().WantMoreData()) {
                    ship.SetMovePosition(command.targetStar.GetPosition(), ship.GetSize() + command.targetStar.GetSize() * 2);
                    currentCommandType = UnitAICommand.CommandType.Move;
                } else {
                    ship.SetDockTarget(command.destinationStation);
                    currentCommandType = UnitAICommand.CommandType.Dock;
                }
                newCommand = false;
            }
            if (ship.shipAction == Ship.ShipAction.Idle) {
                if (currentCommandType == UnitAICommand.CommandType.Move) {
                    currentCommandType = UnitAICommand.CommandType.Research;
                    return CommandResult.Stop;
                } else if (currentCommandType == UnitAICommand.CommandType.Research) {
                    ship.GetResearchEquiptment().GatherData(command.targetStar, deltaTime);
                    if (!ship.GetResearchEquiptment().WantMoreData()) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = UnitAICommand.CommandType.Dock;
                    }
                    return CommandResult.Stop;
                } else if (currentCommandType == UnitAICommand.CommandType.Dock) {
                    currentCommandType = UnitAICommand.CommandType.Wait;
                    return CommandResult.Stop;
                } else if (currentCommandType == UnitAICommand.CommandType.Wait) {
                    if (ship.GetResearchEquiptment().WantMoreData()) {
                        ship.SetMovePosition(command.targetStar.GetPosition(), ship.GetSize() + command.targetStar.GetSize() * 2);
                        currentCommandType = UnitAICommand.CommandType.Move;
                    }
                    return CommandResult.Stop;
                }
            }
            return CommandResult.Stop;
        }
        if (command.commandType == UnitAICommand.CommandType.Transport) {
            if (command.destinationStation == null || !command.destinationStation.IsSpawned()) {
                return CommandResult.StopRemove;
            }
            if (command.productionStation == null || !command.destinationStation.IsSpawned()) {
                return CommandResult.StopRemove;
            }
            if (ship.dockedStation != null || newCommand) {
                newCommand = false;
                if (ship.dockedStation == command.productionStation) {
                    if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = UnitAICommand.CommandType.Dock;
                    } else {
                        currentCommandType = UnitAICommand.CommandType.Wait;
                    }
                } else if (ship.dockedStation == command.destinationStation) {
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.SetDockTarget(command.productionStation);
                        currentCommandType = UnitAICommand.CommandType.Dock;
                    } else {
                        currentCommandType = UnitAICommand.CommandType.Wait;
                    }
                } else {
                    if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = UnitAICommand.CommandType.Dock;
                    } else {
                        ship.SetDockTarget(command.productionStation);
                        currentCommandType = UnitAICommand.CommandType.Dock;
                    }
                }
                return CommandResult.Stop;
            }
        }
        if (command.commandType == UnitAICommand.CommandType.TransportDelay) {
            if (command.destinationStation == null || !command.destinationStation.IsSpawned()) {
                return CommandResult.StopRemove;
            }
            if (command.productionStation == null || !command.destinationStation.IsSpawned()) {
                return CommandResult.StopRemove;
            }
            if (ship.dockedStation != null || newCommand) {
                newCommand = false;
                if (ship.dockedStation == command.productionStation) {
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) > 0) {
                        command.waitTime -= deltaTime;
                    }
                    if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0 || command.waitTime <= 0) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = UnitAICommand.CommandType.Dock;
                        command.waitTime = command.targetRotation;
                    } else {
                        currentCommandType = UnitAICommand.CommandType.Wait;
                    }
                } else if (ship.dockedStation == command.destinationStation) {
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) > 0) {
                        command.waitTime -= deltaTime;
                    }
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) <= 0 || command.waitTime <= 0) {
                        ship.SetDockTarget(command.productionStation);
                        currentCommandType = UnitAICommand.CommandType.Dock;
                        command.waitTime = command.targetRotation;
                    } else {
                        currentCommandType = UnitAICommand.CommandType.Wait;
                    }
                } else {
                    command.waitTime = command.targetRotation;
                    if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.SetDockTarget(command.destinationStation);
                        currentCommandType = UnitAICommand.CommandType.Dock;
                    } else {
                        ship.SetDockTarget(command.productionStation);
                        currentCommandType = UnitAICommand.CommandType.Dock;
                    }
                }
                return CommandResult.Stop;
            }
        }
        return CommandResult.Stop;
    }

    Unit GetClosestEnemyUnitInRadius(float radius) {
        Unit targetUnit = null;
        float distance = 0;
        for (int i = 0; i < ship.enemyUnitsInRange.Count; i++) {
            Unit tempUnit = ship.enemyUnitsInRange[i];
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
