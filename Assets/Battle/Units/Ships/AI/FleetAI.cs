using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public Fleet fleet { get; private set; }

    public List<Command> commands;
    public bool newCommand;
    public CommandType currentCommandState;
    public FleetFormation.FormationType formationType;

    public void SetupFleetAI(Fleet fleet) {
        this.fleet = fleet;
        commands = new List<Command>(10);
        AddFormationCommand();
        formationType = FleetFormation.ChooseRandomFormation();
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
            CommandResult result = ResolveCommand(commands[0], deltaTime);
            if (result == CommandResult.StopRemove || result == CommandResult.ContinueRemove) {
                NextCommand();
            }
            if (result == CommandResult.ContinueRemove || result == CommandResult.Continue)
                UpdateAI(deltaTime);
            Profiler.EndSample();
        }
    }

    #region CommandLogic

    CommandResult ResolveCommand(Command command, float deltaTime) {
        switch (command.commandType) {
            case CommandType.Idle:
                return DoIdleCommand(command, deltaTime);
            case CommandType.Wait:
                return DoWaitCommand(command, deltaTime);
            case CommandType.TurnToRotation:
                return DoTurnToRotation(command, deltaTime);
            case CommandType.TurnToPosition:
                return DoTurnToPosition(command, deltaTime);
            case CommandType.Move:
                return DoMoveCommand(command, deltaTime);
            case CommandType.AttackMove:
            case CommandType.AttackMoveUnit:
            case CommandType.AttackFleet:
            case CommandType.Protect:
                return DoAttackCommand(command, deltaTime);
            case CommandType.Dock:
                return DoDockCommand(command, deltaTime);
            case CommandType.Follow:
                break;
            case CommandType.Formation:
                return DoFormationCommand(command, deltaTime);
            case CommandType.FormationLocation:
                return DoFormationLocationCommand(command, deltaTime);
            case CommandType.UndockCommand:
                break;
            case CommandType.Transport:
                break;
            case CommandType.TransportDelay:
                break;
            case CommandType.Research:
                break;
            case CommandType.DisbandFleet:
                return DoDisbandFleetCommand(command, deltaTime);
            default:
                break;
        }
        return CommandResult.Stop;
    }

    /// <summary> Idles until something removes this command. </summary>
    CommandResult DoIdleCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.Idle;
            SetFleetIdle();
            newCommand = false;
        }
        return CommandResult.Stop;
    }

    /// <summary> Waits for a certain amount of time, Stop until the time is up, ContinueRemove once finished. </summary>
    CommandResult DoWaitCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.Wait;
            SetFleetIdle();
            newCommand = false;
        }
        command.waitTime -= deltaTime;
        if (command.waitTime <= 0) {
            return CommandResult.ContinueRemove;
        }
        return CommandResult.Stop;
    }

    /// <summary> Rotates all ships towards angle, Stop until turned to rotation, ContinueRemove once Finished. </summary>
    CommandResult DoTurnToRotation(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.TurnToRotation;
            SetFleetRotation(command.targetRotation);
            newCommand = false;
        }
        if (fleet.AreShipsIdle()) {
            return CommandResult.ContinueRemove;
        }
        return CommandResult.Stop;
    }

    /// <summary> Rotates towards position, Stop until turned to angle, ContinueRemove once Finished. </summary>
    CommandResult DoTurnToPosition(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.TurnToPosition;
            SetFleetRotation(command.targetPosition);
            newCommand = false;
        }
        if (fleet.AreShipsIdle()) {
            return CommandResult.ContinueRemove;
        }
        return CommandResult.Stop;
    }
    
    /// <summary> Rotates towards position then moves towards position, Stop until moved to position, ContinueRemoveOnce Finished. </summary>
    CommandResult DoMoveCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.TurnToPosition;
            SetFleetMoveCommand(command.targetPosition);
            newCommand = false;
            return CommandResult.Stop;
        }

        if (currentCommandState == CommandType.TurnToPosition && fleet.AreShipsIdle()) {
            fleet.NextShipsCommand();
            currentCommandState = CommandType.Move;
            return CommandResult.Stop;
        }

        if (currentCommandState == CommandType.Move && fleet.AreShipsIdle()) {
            return CommandResult.ContinueRemove;
        }
        return CommandResult.Stop;
    }

    CommandResult DoAttackCommand(Command command, float deltaTime) {
        if (!newCommand) {
            if (command.commandType == CommandType.AttackFleet) {
                if (Vector2.Distance(command.targetFleet.GetPosition(), command.targetPosition) > fleet.maxWeaponRange / 4) {
                    newCommand = true;
                }
            } else if (command.commandType == CommandType.AttackMoveUnit) {
                if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
                    SetFleetIdle();
                    return CommandResult.StopRemove;
                }
            }
        }
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
            currentCommandState = CommandType.Move;
            newCommand = false;
            return CommandResult.Stop;
        }
        //Checks if the command should stop or reapply itself
        if (currentCommandState == CommandType.Move && fleet.enemyUnitsInRange.Count == 0 && command.targetFleet == null && (command.targetUnit == null || !command.targetUnit.IsSpawned()) && command.protectUnit == null) {
            return CommandResult.ContinueRemove;
        } else if (currentCommandState == CommandType.AttackFleet && command.targetFleet == null) {
            if (command.commandType == CommandType.AttackFleet) {
                return CommandResult.StopRemove;
            } else if (fleet.GetNearbyEnemyFleet() == null) {
                AddFormationTowardsPositionCommand(command.targetPosition, fleet.GetSize() / 2, CommandAction.AddToBegining);
                return CommandResult.Stop;
            }
            newCommand = true;
        }
        //Finds a new target fleet if there is one, then orders ships to attack it.
        if (command.targetFleet == null) {
            Fleet targetFleet = fleet.GetNearbyEnemyFleet();
            if (targetFleet != null && currentCommandState == CommandType.Move) {
                command.targetFleet = targetFleet;
                currentCommandState = CommandType.AttackFleet;
                for (int i = 0; i < fleet.GetShips().Count; i++) {
                    Vector2 relativeVector = fleet.GetShips()[i].GetPosition() - fleet.GetPosition();
                    Ship target = null;
                    float relativeDistance = 0;
                    for (int j = 0; j < targetFleet.GetShips().Count; j++) {
                        float newRelativeDistance = Vector2.Distance(relativeVector, targetFleet.GetShips()[j].GetPosition() - targetFleet.GetPosition());
                        if (target == null || newRelativeDistance < relativeDistance) {
                            target = targetFleet.GetShips()[j];
                            relativeDistance = newRelativeDistance;
                        }
                    }
                    fleet.GetShips()[i].shipAI.AddUnitAICommand(CreateSkirmishCommand(target, targetFleet), CommandAction.Replace);
                }
                return CommandResult.Stop;
            }
        }
        //Sets all idle ships to attackMove to the commands targetPosition
        if (fleet.enemyUnitsInRange.Count > 0) {
            for (int i = 0; i < fleet.GetShips().Count; i++) {
                if (fleet.GetShips()[i].IsIdle()) {
                    SetFleetAttackMovePosition(command.targetPosition);
                }
            }
        }
        return CommandResult.Stop;
    }

    /// <summary> Moves the ship to the target station and then docks at it. </summary>
    CommandResult DoDockCommand(Command command, float deltaTime) {
        if (newCommand) {
            SetFleetDockTarget(command.destinationStation);
            newCommand = false;
        }
        if (fleet.AreShipsIdle()) {
            return CommandResult.StopRemove;
        }
        return CommandResult.Stop;
    }

    CommandResult DoFormationCommand(Command command, float deltaTime) {
        if (newCommand) {
            (List<Ship>, List<Vector2>) shipTargetPositions = FleetFormation.GetFormationShipPosition(fleet, fleet.GetPosition(), command.targetRotation, 0f, formationType);
            for (int i = 0; i < shipTargetPositions.Item1.Count; i++) {
                shipTargetPositions.Item1[i].shipAI.AddUnitAICommand(CreateMoveCommand(shipTargetPositions.Item2[i]), CommandAction.Replace);
                shipTargetPositions.Item1[i].shipAI.AddUnitAICommand(CreateRotationCommand(command.targetRotation));
            }
            currentCommandState = CommandType.Formation;
            newCommand = false;
            return CommandResult.Stop;
        }
        if (currentCommandState == CommandType.Formation && fleet.AreShipsIdle()) {
            return CommandResult.StopRemove;
        }
        return CommandResult.Stop;
    }

    CommandResult DoDisbandFleetCommand(Command command, float deltaTime) {
        fleet.DisbandFleet();
        return CommandResult.Stop;
    }

    CommandResult DoFormationLocationCommand(Command command, float deltaTime) {
        if (newCommand) {
            (List<Ship>, List<Vector2>) shipTargetPositions = FleetFormation.GetFormationShipPosition(fleet, command.targetPosition, command.targetRotation, 0f, formationType);
            for (int i = 0; i < shipTargetPositions.Item1.Count; i++) {
                shipTargetPositions.Item1[i].shipAI.AddUnitAICommand(CreateMoveCommand(shipTargetPositions.Item2[i]), CommandAction.Replace);
                shipTargetPositions.Item1[i].shipAI.AddUnitAICommand(CreateRotationCommand(command.targetRotation));
            }
            currentCommandState = CommandType.Formation;
            newCommand = false;
            return CommandResult.Stop;
        }
        if (currentCommandState == CommandType.Formation && fleet.AreShipsIdle()) {
            return CommandResult.StopRemove;
        }
        return CommandResult.Stop;
    }
    #endregion

    #region FleetAIControls
    public void SetFleetIdle() {
        foreach (var ship in fleet.ships) {
            ship.SetIdle();
        }
    }

    public void SetFleetRotation(float rotation) {
        for (int i = 0; i < fleet.GetShips().Count; i++) {
            fleet.GetShips()[i].SetTargetRotate(rotation);
        }
    }

    public void SetFleetRotation(Vector2 targetPostion) {
        for (int i = 0; i < fleet.GetShips().Count; i++) {
            fleet.GetShips()[i].SetTargetRotate(targetPostion);
        }
    }

    public float GetTimeUntilFinishedWithCommand() {
        if (commands.Count == 0) return 0;
        Command command = commands[0];
        if (command.commandType == CommandType.Wait) return command.waitTime;
        else if (command.commandType == CommandType.Move) {
            float distance = Vector2.Distance(fleet.GetPosition(), command.targetPosition);
            return distance / fleet.minShipSpeed;
        }
        return 0;
    }


    /// <summary>
    /// Clears all other commands, rotates the ships towards the position 
    /// and tells them to move toward the position at the same speed in a rotated formation.
    /// </summary>
    /// <param name="movePosition">the position to move to</param>
    public void SetFleetMoveCommand(Vector2 movePosition) {
        for (int i = 0; i < fleet.GetShips().Count; i++) {
            Vector2 shipOffset = fleet.GetPosition() - fleet.GetShips()[i].GetPosition();
            fleet.GetShips()[i].shipAI.AddUnitAICommand(CreateRotationCommand(movePosition - shipOffset), CommandAction.Replace);
            fleet.GetShips()[i].shipAI.AddUnitAICommand(CreateIdleCommand());
            fleet.GetShips()[i].shipAI.AddUnitAICommand(CreateMoveCommand(movePosition - shipOffset, fleet.minShipSpeed));
        }
    }

    /// <summary>
    /// Clears all other commands, sets the formation of the ships towards the position 
    /// and tells them to move toward the position at the same speed in the previous formation.
    /// </summary>
    /// <param name="movePosition">the position to move to</param>
    public void SetFleetMoveFormationCommand(Vector2 movePosition) {
        for (int i = 0; i < fleet.GetShips().Count; i++) {
            Vector2 shipOffset = fleet.GetPosition() - fleet.GetShips()[i].GetPosition();
            AddFormationCommand(fleet.GetPosition(), Vector2.Angle(fleet.GetPosition(), movePosition));
            fleet.GetShips()[i].shipAI.AddUnitAICommand(CreateIdleCommand());
            fleet.GetShips()[i].shipAI.AddUnitAICommand(CreateMoveCommand(movePosition - shipOffset, fleet.minShipSpeed));
        }
    }

    /// <summary>
    /// Clears all other commands and adds a dock command.
    /// </summary>
    /// <param name="targetStation"></param>
    public void SetFleetDockTarget(Station targetStation) {
        for (int i = 0; i < fleet.GetShips().Count; i++) {
            fleet.GetShips()[i].shipAI.AddUnitAICommand(CreateDockCommand(targetStation, fleet.minShipSpeed), CommandAction.Replace);
        }
    }

    /// <summary>
    /// Clears all other commands and adds an attack move command towards the position.
    /// </summary>
    /// <param name="movePosition">the position to AttackMove to</param>
    public void SetFleetAttackMovePosition(Vector2 movePosition) {
        for (int i = 0; i < fleet.GetShips().Count; i++) {
            Vector2 shipOffset = fleet.GetPosition() - fleet.GetShips()[i].GetPosition();
            fleet.GetShips()[i].shipAI.AddUnitAICommand(CreateAttackMoveCommand(movePosition - shipOffset, fleet.minShipSpeed), CommandAction.Replace);
        }
    }

    public void AddFormationCommand(CommandAction commandAction = CommandAction.Replace) {
        AddUnitAICommand(CreateFormationCommand(fleet.GetShips()[0].GetRotation()), commandAction);
    }

    public void AddFormationCommand(Vector2 position, CommandAction commandAction = CommandAction.Replace) {
        AddUnitAICommand(CreateFormationCommand(position, fleet.GetShips()[0].GetRotation()), commandAction);
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
        AddFormationCommand(Vector2.MoveTowards(fleet.GetPosition(), targetPosition, distance), Calculator.GetAngleOutOfTwoPositions(fleet.GetPosition(), targetPosition), commandAction);
    }
    #endregion
}