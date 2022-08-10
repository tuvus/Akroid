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

    public void AddUnitAICommand(UnitAICommand command, CommandAction commandAction) {
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
        Profiler.BeginSample("ShipAI");
        for (int i = 0; i < commands.Count; i++) {
            Profiler.BeginSample("ResolveCommand " + commands[i].commandType);
            CommandResult result = ResolveCommand(commands[i], deltaTime, i);
            Profiler.EndSample();
            if (result == CommandResult.StopRemove || result == CommandResult.ContinueRemove) {
                commands.RemoveAt(i);
                i--;
                newCommand = true;
            }
            if (result == CommandResult.Stop || result == CommandResult.StopRemove) {
                break;
            }
        }
        Profiler.EndSample();
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
            if (index == -1 || command.waitTime - deltaTime <= 0) {
                return CommandResult.ContinueRemove;
            }
            if (newCommand) {
                currentCommandType = UnitAICommand.CommandType.Wait;
                ship.SetIdle();
                newCommand = false;
            }
            commands[index] = new UnitAICommand(UnitAICommand.CommandType.Idle, command.waitTime - deltaTime);
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
                if (ship.dockedStation != null) {
                    ship.UndockShip(command.targetPosition);
                }
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
            if (newCommand) {
                currentCommandType = UnitAICommand.CommandType.Move;
                if (ship.dockedStation != null) {
                    ship.UndockShip(command.targetPosition);
                }
                ship.SetMovePosition(command.targetPosition);
                newCommand = false;
            }

            if (currentCommandType == UnitAICommand.CommandType.Move) {
                if (command.targetUnit == null || (command.targetUnit.IsShip() && !((Ship)command.targetUnit).IsCombatShip()) || !command.targetUnit.IsSpawned()) {
                    command.targetUnit = GetClosestEnemyUnitInRadius(ship.GetMaxTurretRange() * 2);
                    if (command.targetUnit != null) {
                        ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinTurretRange() * .8f);
                        currentCommandType = UnitAICommand.CommandType.AttackMove;
                        return CommandResult.Stop;
                    }
                }
                if (currentCommandType == UnitAICommand.CommandType.Move && ship.shipAction == Ship.ShipAction.Idle) {
                    return CommandResult.ContinueRemove;
                }
            }
            if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
                newCommand = true;
                return CommandResult.Stop;
            }
            if (currentCommandType == UnitAICommand.CommandType.AttackMove) {
                if (ship.shipAction == Ship.ShipAction.Idle) {
                    currentCommandType = UnitAICommand.CommandType.TurnToRotation;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinTurretRange() * .8f);
                }
                return CommandResult.Stop;
            }

            if (currentCommandType == UnitAICommand.CommandType.TurnToRotation) {
                if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMinTurretRange() * .8f) {
                    currentCommandType = UnitAICommand.CommandType.AttackMove;
                } else {
                    ship.SetTargetRotate(command.targetUnit.GetPosition(), ship.GetCombatRotation());
                }
            }

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
                if (ship.dockedStation != null) {
                    ship.UndockShip(command.targetUnit.GetPosition());
                }
                currentCommandType = UnitAICommand.CommandType.Move;
                ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinTurretRange() * .8f);
                newCommand = false;
            }
            if (currentCommandType == UnitAICommand.CommandType.Move) {
                if (ship.shipAction == Ship.ShipAction.Idle) {
                    currentCommandType = UnitAICommand.CommandType.TurnToRotation;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetMinTurretRange() * .8f);
                    return CommandResult.Stop;
                }
            }

            if (currentCommandType == UnitAICommand.CommandType.TurnToRotation) {
                if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMinTurretRange() * .8f) {
                    currentCommandType = UnitAICommand.CommandType.AttackMove;
                } else {
                    ship.SetTargetRotate(command.targetUnit.GetPosition(), ship.GetCombatRotation());
                }
            }
            return CommandResult.Stop;
        }
        //Follows friendly ship, Continue until friendly ship is destroyed, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.Follow) {
            if (command.targetUnit == null) {
                return CommandResult.ContinueRemove;
            }
            float distance = Vector2.Distance(ship.transform.position, command.targetUnit.transform.position) - (ship.GetSize() + command.targetUnit.GetSize());
            if (distance > ship.GetSize()) {
                CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.Move, Vector3.MoveTowards(ship.transform.position, command.targetUnit.transform.position, distance)), deltaTime);
                if (result == CommandResult.ContinueRemove || result == CommandResult.StopRemove) {
                    ship.SetThrusters(false);
                    return CommandResult.Continue;
                }
                return CommandResult.Stop;
            }
            ship.SetThrusters(false);
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
                if (ship.dockedStation != null && ship.dockedStation != command.targetUnit) {
                    ship.UndockShip(command.targetUnit.GetPosition());
                }
                ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
                currentCommandType = UnitAICommand.CommandType.Dock;
                newCommand = false;
            }
            if (ship.shipAction == Ship.ShipAction.Idle) {
                ship.DockShip((Station)command.targetUnit);
                return CommandResult.StopRemove;
            }
            return CommandResult.Stop;
        }
        //AttackMove to the star, do reasearch, then remove command.
        if (command.commandType == UnitAICommand.CommandType.Research) {
            if (command.targetStar == null) {
                return CommandResult.StopRemove;
            }
            if (newCommand) {
                if (ship.GetResearchEquiptment().WantMoreData()) {
                    ship.SetMovePosition(command.targetStar.GetPosition(), ship.GetSize() + command.targetStar.GetSize() * 2);
                    currentCommandType = UnitAICommand.CommandType.Move;
                } else {
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
                    currentCommandType = UnitAICommand.CommandType.Dock;
                }
                newCommand = false;
            }
            if (ship.shipAction == Ship.ShipAction.Idle) {
                if (currentCommandType == UnitAICommand.CommandType.Move) {
                    ship.GetResearchEquiptment().GatherData(command.targetStar, deltaTime);
                    ship.SetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
                    currentCommandType = UnitAICommand.CommandType.Dock;
                    return CommandResult.Stop;
                } else if (currentCommandType == UnitAICommand.CommandType.Dock) {
                    ship.DockShip((Station)command.targetUnit);
                    return CommandResult.Stop;
                }
            }
            return CommandResult.Stop;
        }
        if (command.commandType == UnitAICommand.CommandType.Transport) {
            if (newCommand) {
                newCommand = false;
            }
            if (ship.dockedStation == null) {
                if (newCommand) {
                    newCommand = false;
                    if (ship.dockedStation == null) {
                        if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0) {
                            ship.UndockShip(command.destinationStation.GetPosition());
                            ship.SetMovePosition(command.destinationStation.GetPosition(), ship.GetSize() + command.destinationStation.GetSize());
                            currentCommandType = UnitAICommand.CommandType.Dock;
                        } else {
                            ship.UndockShip(command.productionStation.GetPosition());
                            ship.SetMovePosition(command.productionStation.GetPosition(), ship.GetSize() + command.productionStation.GetSize());
                            currentCommandType = UnitAICommand.CommandType.Dock;
                        }
                        return CommandResult.Stop;
                    }
                }
                if (ship.shipAction == Ship.ShipAction.Idle) {
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) > 0) {
                        ship.DockShip(command.destinationStation);
                    } else {
                        ship.DockShip(command.productionStation);
                    }
                    currentCommandType = UnitAICommand.CommandType.Wait;
                }
                return CommandResult.Stop;
            } else {
                if (ship.dockedStation == command.productionStation) {
                    if (ship.GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.UndockShip(command.destinationStation.GetPosition());
                        ship.SetMovePosition(command.destinationStation.GetPosition(), ship.GetSize() + command.destinationStation.GetSize());
                        currentCommandType = UnitAICommand.CommandType.Dock;
                    }
                } else if (ship.dockedStation == command.destinationStation) {
                    if (ship.GetCargoBay().GetAllCargo(CargoBay.CargoTypes.Metal) <= 0) {
                        ship.UndockShip(command.productionStation.GetPosition());
                        ship.SetMovePosition(command.productionStation.GetPosition(), ship.GetSize() + command.productionStation.GetSize());
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
