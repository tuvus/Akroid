using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using static Command;

public class FleetAI : MonoBehaviour {
    enum CommandResult {
        Stop = 0,
        StopRemove = 1,
        ContinueRemove = 2,
        Continue = 3,
    }
    Fleet fleet;

    public List<Command> commands;
    public bool newCommand;
    public CommandType currentCommandType;

    public void SetupFleetAI(Fleet fleet) {
        this.fleet = fleet;
        commands = new List<Command>(10);
        AddFormationCommand();
    }

    public void AddUnitAICommand(Command command, CommandAction commandAction = CommandAction.AddToEnd) {
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
    }

    public void UpdateAI(float deltaTime) {
        if (commands.Count > 0) {
            Profiler.BeginSample("FleetAI ResolveCommand");
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
                SetFleetIdle();
                newCommand = false;
            }
            return CommandResult.Stop;
        }
        //Waits for a certain amount of time, Stop until the time is up, ContinueRemove once finished.
        if (command.commandType == CommandType.Wait) {
            if (newCommand) {
                currentCommandType = CommandType.Wait;
                SetFleetIdle();
                newCommand = false;
            }
            commands[index].waitTime -= deltaTime;
            if (index == -1 || command.waitTime <= 0) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Rotates all ships towards angle, Stop until turned to rotation, ContinueRemove once Finished
        if (command.commandType == CommandType.TurnToRotation) {
            if (newCommand) {
                currentCommandType = CommandType.TurnToRotation;
                SetFleetRotation(command.targetRotation);
                newCommand = false;
            }
            if (fleet.AreShipsIdle()) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Rptates towards position, Stop until turned to angle, ContinueRemove once Finished.
        if (command.commandType == CommandType.TurnToPosition) {
            if (newCommand) {
                currentCommandType = CommandType.TurnToPosition;
                SetFleetRotation(command.targetPosition);
                newCommand = false;
            }
            if (fleet.AreShipsIdle()) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Rotates towards position then moves towards position, Stop until moved to postion, ContinueRemoveOnce Finished.
        if (command.commandType == CommandType.Move) {
            if (newCommand) {
                currentCommandType = CommandType.TurnToPosition;
                SetFleetMoveCommand(command.targetPosition);
                newCommand = false;
                return CommandResult.Stop;
            }

            if (currentCommandType == CommandType.TurnToPosition && fleet.AreShipsIdle()) {
                fleet.NextShipsCommand();
                currentCommandType = CommandType.Move;
                return CommandResult.Stop;
            }

            if (currentCommandType == CommandType.Move && fleet.AreShipsIdle()) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        if (command.commandType == CommandType.AttackMove || command.commandType == CommandType.AttackFleet || command.commandType == CommandType.AttackMoveUnit || command.commandType == CommandType.Protect) {
            //Sets the target position of the command and tells all ships to attack move
            if (newCommand) {
                if (command.commandType == CommandType.Protect) {
                    Vector2 fleetCenter = fleet.GetPosition();
                    command.targetPosition = Vector2.MoveTowards(fleetCenter, command.protectUnit.GetPosition(), Vector2.Distance(fleetCenter, command.protectUnit.GetPosition()) - (fleet.GetMaxShipSize() + command.protectUnit.GetSize()) * 2);
                } else if (command.commandType == CommandType.AttackMoveUnit) {
                    Vector2 fleetCenter = fleet.GetPosition();
                    command.targetPosition = Vector2.MoveTowards(fleetCenter, command.targetUnit.GetPosition(), Vector2.Distance(fleetCenter, command.targetUnit.GetPosition()) - (fleet.GetMaxShipSize() + command.targetUnit.GetSize()) * 2);
                } else if (command.commandType == CommandType.AttackFleet) {
                    command.targetPosition = command.targetFleet.GetPosition();
                }
                SetFleetAttackMovePosition(command.targetPosition);
                currentCommandType = CommandType.Move;
                newCommand = false;
                return CommandResult.Stop;
            }
            //Checks if the command should stop or reapply itself
            if (currentCommandType == CommandType.Move && fleet.enemyUnitsInRange.Count == 0 && command.targetFleet == null && (command.targetUnit == null || !command.targetUnit.IsSpawned()) && command.protectUnit == null) {
                return CommandResult.ContinueRemove;
            } else if (currentCommandType == CommandType.AttackFleet && command.targetFleet == null) {
                if (command.commandType == CommandType.AttackFleet) {
                    return CommandResult.StopRemove;
                }
                if (fleet.GetNearbyEnemyFleet() == null) {
                    AddFormationTowardsPositionCommand(command.targetPosition, fleet.GetSize() / 2, CommandAction.AddToBegining);
                    return CommandResult.Stop;
                }
                newCommand = true;
            }
            //Finds a new target fleet if there is one, then orders ships to attack it.
            if (command.targetFleet == null) {
                Fleet targetFleet = fleet.GetNearbyEnemyFleet();
                if (targetFleet != null && currentCommandType == CommandType.Move) {
                    command.targetFleet = targetFleet;
                    currentCommandType = CommandType.AttackFleet;
                    for (int i = 0; i < fleet.GetAllShips().Count; i++) {
                        Vector2 relativeVector = fleet.GetAllShips()[i].GetPosition() - fleet.GetPosition();
                        Ship target = null;
                        float relativeDistance = 0;
                        for (int j = 0; j < targetFleet.GetAllShips().Count; j++) {
                            float newRelativeDistance = Vector2.Distance(relativeVector, targetFleet.GetAllShips()[j].GetPosition() - targetFleet.GetPosition());
                            if (target == null || newRelativeDistance < relativeDistance) {
                                target = targetFleet.GetAllShips()[j];
                                relativeDistance = newRelativeDistance;
                            }
                        }
                        fleet.GetAllShips()[i].shipAI.AddUnitAICommand(CreateSkirmishCommand(target, targetFleet), CommandAction.Replace);
                    }
                    return CommandResult.Stop;
                }
            }
            //Sets all idle ships to attackMove to the commands targetposition
            if (fleet.enemyUnitsInRange.Count > 0) {
                for (int i = 0; i < fleet.ships.Count; i++) {
                    if (fleet.ships[i].IsIdle()) {
                        SetFleetAttackMovePosition(command.targetPosition);
                    }
                }
            }
            return CommandResult.Stop;
        }
        if (command.commandType == CommandType.Dock) {
            if (newCommand) {
                SetFleetDockTarget(command.destinationStation);
                newCommand = false;
            }
            if (fleet.AreShipsIdle()) {
                return CommandResult.StopRemove;
            }
            return CommandResult.Stop;
        }
        if (command.commandType == CommandType.Formation) {
            if (newCommand) {
                float size = fleet.GetMaxShipSize();
                Vector2 fleetCenter = fleet.GetPosition();
                Vector2 startPosition = fleetCenter - Calculator.GetPositionOutOfAngleAndDistance(command.targetRotation - 90, size * fleet.ships.Count * 2);
                Vector2 endPosition = fleetCenter - Calculator.GetPositionOutOfAngleAndDistance(command.targetRotation + 90, size * fleet.ships.Count * 2);
                for (int i = 0; i < fleet.ships.Count; i++) {
                    fleet.ships[i].shipAI.AddUnitAICommand(CreateMoveCommand(Vector2.Lerp(startPosition, endPosition, i / (float)(fleet.ships.Count - 1))), CommandAction.Replace);
                    fleet.ships[i].shipAI.AddUnitAICommand(CreateRotationCommand(command.targetRotation));
                }
                currentCommandType = CommandType.Formation;
                newCommand = false;
                return CommandResult.Stop;
            }
            if (currentCommandType == CommandType.Formation && fleet.AreShipsIdle()) {
                return CommandResult.StopRemove;
            }

        }
        if (command.commandType == CommandType.FormationLocation) {
            if (newCommand) {
                float size = fleet.GetMaxShipSize();
                Vector2 startPosition = command.targetPosition - Calculator.GetPositionOutOfAngleAndDistance(command.targetRotation - 90, size * fleet.ships.Count * 2);
                Vector2 endPosition = command.targetPosition - Calculator.GetPositionOutOfAngleAndDistance(command.targetRotation + 90, size * fleet.ships.Count * 2);
                for (int i = 0; i < fleet.ships.Count; i++) {
                    fleet.ships[i].shipAI.AddUnitAICommand(CreateMoveCommand(Vector2.Lerp(startPosition, endPosition, i / (float)(fleet.ships.Count - 1))), CommandAction.Replace);
                    fleet.ships[i].shipAI.AddUnitAICommand(CreateRotationCommand(command.targetRotation));
                }
                currentCommandType = CommandType.Formation;
                newCommand = false;
                return CommandResult.Stop;
            }
            if (currentCommandType == CommandType.Formation && fleet.AreShipsIdle()) {
                return CommandResult.StopRemove;
            }
        }
        if (command.commandType == CommandType.DisbandFleet) {
            fleet.DisbandFleet();
            return CommandResult.Stop;
        }
        return CommandResult.Stop;
    }

    public void SetFleetIdle() {
        for (int i = 0; i < fleet.ships.Count; i++) {
            fleet.ships[i].SetIdle();
        }
    }

    public void SetFleetRotation(float rotation) {
        for (int i = 0; i < fleet.ships.Count; i++) {
            fleet.ships[i].SetTargetRotate(rotation);
        }
    }

    public void SetFleetRotation(Vector2 targetPostion) {
        for (int i = 0; i < fleet.ships.Count; i++) {
            fleet.ships[i].SetTargetRotate(targetPostion);
        }
    }

    /// <summary>
    /// Clears all other commands, rotates the ships towards the position 
    /// and tells them to move toward the position at the same speed in a rotated formation.
    /// </summary>
    /// <param name="movePosition">the position to move to</param>
    public void SetFleetMoveCommand(Vector2 movePosition) {
        for (int i = 0; i < fleet.ships.Count; i++) {
            Vector2 shipOffset = fleet.GetPosition() - fleet.ships[i].GetPosition();
            fleet.ships[i].shipAI.AddUnitAICommand(CreateRotationCommand(movePosition - shipOffset), CommandAction.Replace);
            fleet.ships[i].shipAI.AddUnitAICommand(CreateIdleCommand());
            fleet.ships[i].shipAI.AddUnitAICommand(CreateMoveCommand(movePosition - shipOffset, fleet.minFleetSpeed));
        }
    }

    /// <summary>
    /// Clears all other commands, sets the formation of the ships towards the position 
    /// and tells them to move toward the position at the same speed in the previous formation.
    /// </summary>
    /// <param name="movePosition">the position to move to</param>
    public void SetFleetMoveFormationCommand(Vector2 movePosition) {
        for (int i = 0; i < fleet.ships.Count; i++) {
            Vector2 shipOffset = fleet.GetPosition() - fleet.ships[i].GetPosition();
            AddFormationCommand(fleet.GetPosition(), Vector2.Angle(fleet.GetPosition(), movePosition));
            fleet.ships[i].shipAI.AddUnitAICommand(CreateIdleCommand());
            fleet.ships[i].shipAI.AddUnitAICommand(CreateMoveCommand(movePosition - shipOffset, fleet.minFleetSpeed));
        }
    }

    /// <summary>
    /// Clears all other commands and adds a dock command.
    /// </summary>
    /// <param name="targetStation"></param>
    public void SetFleetDockTarget(Station targetStation) {
        for (int i = 0; i < fleet.ships.Count; i++) {
            fleet.ships[i].shipAI.AddUnitAICommand(CreateDockCommand(targetStation, fleet.minFleetSpeed), CommandAction.Replace);
        }
    }

    /// <summary>
    /// Clears all other commands and adds an attack move command towards the position.
    /// </summary>
    /// <param name="movePosition">the position to AttackMove to</param>
    public void SetFleetAttackMovePosition(Vector2 movePosition) {
        for (int i = 0; i < fleet.ships.Count; i++) {
            Vector2 shipOffset = fleet.GetPosition() - fleet.ships[i].GetPosition();
            fleet.ships[i].shipAI.AddUnitAICommand(CreateAttackMoveCommand(movePosition - shipOffset, fleet.minFleetSpeed), CommandAction.Replace);
        }
    }

    public void AddFormationCommand(CommandAction commandAction = CommandAction.Replace) {
        AddUnitAICommand(CreateFormationCommand(fleet.ships[0].GetRotation()), commandAction);
    }

    public void AddFormationCommand(Vector2 position, CommandAction commandAction = CommandAction.Replace) {
        AddUnitAICommand(CreateFormationCommand(position, fleet.ships[0].GetRotation()), commandAction);
    }

    public void AddFormationCommand(Vector2 position, float rotation, CommandAction commandAction = CommandAction.Replace) {
        AddUnitAICommand(CreateFormationCommand(position, rotation), commandAction);
    }

    /// <summary>
    /// Adds a formation command distance towards targetPosition pointing towards targetPosition
    /// </summary>
    /// <param name="targetPosition">the position to point towards</param>
    /// <param name="distance">the distance from the current fleet position</param>
    /// <param name="commandAction">how the command should be added to the list</param>
    public void AddFormationTowardsPositionCommand(Vector2 targetPosition, float distance, CommandAction commandAction = CommandAction.Replace) {
        AddFormationCommand(Vector2.MoveTowards(fleet.GetPosition(), targetPosition, distance), Calculator.GetAngleOutOfTwoPositions(fleet.GetPosition(), targetPosition));
    }
}