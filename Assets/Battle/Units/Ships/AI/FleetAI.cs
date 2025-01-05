using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using static Command;

[System.Serializable]
public class FleetAI {
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

    public FleetAI(Fleet fleet) {
        this.fleet = fleet;
        commands = new List<Command>(10);
        AddFormationCommand();
        formationType = FleetFormation.ChooseRandomFormation();
    }

    public void AddFleetAICommand(Command command, CommandAction commandAction = CommandAction.AddToEnd) {
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

    /// <returns> True if the fleet has some non idle and wait command that will be reached without intervention. </returns>
    public bool HasActionCommand() {
        foreach (var command in commands) {
            if (command.commandType == CommandType.Idle) return false;
            if (command.IsAttackCommand()) return true;
        }

        return false;
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
        return command.commandType switch {
            CommandType.Idle => DoIdleCommand(command, deltaTime),
            CommandType.Wait => DoWaitCommand(command, deltaTime),
            CommandType.TurnToRotation => DoTurnToRotation(command, deltaTime),
            CommandType.TurnToPosition => DoTurnToPosition(command, deltaTime),
            CommandType.Move => DoMoveCommand(command, deltaTime),
            CommandType.Follow => DoFollowCommand(command, deltaTime),
            CommandType.AttackMove => DoAttackCommand(command, deltaTime),
            CommandType.AttackMoveUnit => DoAttackCommand(command, deltaTime),
            CommandType.AttackFleet => DoAttackCommand(command, deltaTime),
            CommandType.Protect => DoAttackCommand(command, deltaTime),
            CommandType.Dock => DoDockCommand(command, deltaTime),
            CommandType.Formation => DoFormationCommand(command, deltaTime),
            CommandType.FormationLocation => DoFormationLocationCommand(command, deltaTime),
            CommandType.BuildStation => DoBuildStationCommand(command, deltaTime),
            CommandType.DisbandFleet => DoDisbandFleetCommand(command, deltaTime),
            _ => CommandResult.Stop,
        };
    }

    /// <summary> Idles until something removes this command. </summary>
    CommandResult DoIdleCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.Idle;
            SetShipsIdle();
            newCommand = false;
        }

        return CommandResult.Stop;
    }

    /// <summary> Waits for a certain amount of time, Stop until the time is up, ContinueRemove once finished. </summary>
    CommandResult DoWaitCommand(Command command, float deltaTime) {
        if (newCommand) {
            currentCommandState = CommandType.Wait;
            SetShipsIdle();
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

    CommandResult DoFollowCommand(Command command, float deltaTime) {
        if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
            return CommandResult.ContinueRemove;
        }

        Vector2 newTargetPostion = Vector2.MoveTowards(fleet.GetPosition(), command.targetUnit.GetPosition(),
            Vector2.Distance(fleet.GetPosition(), command.targetUnit.GetPosition()) - fleet.GetSize() + command.targetUnit.GetSize());
        if (newCommand) {
            currentCommandState = CommandType.TurnToPosition;
            command.targetPosition = newTargetPostion;
            SetFleetMoveCommand(newTargetPostion);
            newCommand = false;
            return CommandResult.Stop;
        }

        if (Vector2.Distance(command.targetPosition, newTargetPostion) > 100) {
            currentCommandState = CommandType.TurnToPosition;
            command.targetPosition = newTargetPostion;
            SetFleetMoveCommand(newTargetPostion);
        }

        if (currentCommandState == CommandType.TurnToPosition && fleet.AreShipsIdle()) {
            fleet.NextShipsCommand();
            currentCommandState = CommandType.Move;
            return CommandResult.Stop;
        }

        return CommandResult.Stop;
    }

    /// <summary>
    /// Moves the fleet in an attacking behaviour based on its command type.
    /// The command might find a new fleet target to engage with a more sophisticated attack strategy.
    /// After the temporary fleet is destroyed it will continue attacking its set target.
    /// </summary>
    CommandResult DoAttackCommand(Command command, float deltaTime) {
        // If there is an enemy fleet nearby lets use DoAttackFleet to engage them
        if (command.targetFleet == null) {
            command.targetFleet = fleet.GetNearbyEnemyFleet();
            if (command.targetFleet != null || currentCommandState == CommandType.AttackFleet)
                newCommand = true;
        }

        // TODO: If the actual target isn't the targetFleet do a range check to see if it left
        if (DoAttackFleet(command, deltaTime) == CommandResult.Stop) return CommandResult.Stop;
        if (command.commandType == CommandType.AttackFleet) return CommandResult.ContinueRemove;

        if (command.targetUnit != null && DoAttackUnit(command, deltaTime) == CommandResult.Stop) return CommandResult.Stop;
        if (command.commandType == CommandType.AttackMoveUnit) return CommandResult.ContinueRemove;

        // If there is an enemy unit nearby lets use DoAttackUnit to engage it
        if (command.targetUnit == null || !command.targetUnit.IsTargetable()) {
            command.targetUnit = fleet.enemyUnitsInRange.FirstOrDefault();
            if (command.targetUnit != null || currentCommandState == CommandType.AttackMoveUnit)
                newCommand = true;
        }

        if (command.protectUnit != null && DoProtectUnit(command, deltaTime) == CommandResult.Stop) return CommandResult.Stop;
        if (command.commandType == CommandType.Protect) return CommandResult.ContinueRemove;

        if (command.targetPosition != null && DoAttackMove(command, deltaTime) == CommandResult.Stop) return CommandResult.Stop;
        return CommandResult.ContinueRemove;
    }

    /// <summary> Moves the fleet's ships into position to attack the enemy fleet </summary>
    private CommandResult DoAttackFleet(Command command, float deltaTime) {
        if (command.targetFleet == null || !command.targetFleet.IsTargetable()) {
            command.targetFleet = null;
            return CommandResult.ContinueRemove;
        }

        //Sets the target position of the command and tells all ships to attack move
        if (newCommand) {
            newCommand = false;
            currentCommandState = CommandType.FormationLocation;

            // If we are far away form the target fleet form up before attacking
            if (Vector2.Distance(fleet.GetPosition(), command.targetFleet.GetPosition()) > fleet.GetMaxTurretRange() * 1.2) {
                AssignShipsToFormationLocation(command.targetFleet.GetPosition(), fleet.GetSize() / 2);
                return CommandResult.Stop;
            } else {
                SetShipsIdle();
            }
        }

        if (currentCommandState == CommandType.FormationLocation && fleet.AreShipsIdle()) {
            AssignShipsToAttackFleet(command.targetFleet, fleet.minShipSpeed);
            currentCommandState = CommandType.Move;
        }

        if (currentCommandState == CommandType.Move
            && Vector2.Distance(fleet.GetPosition(), command.targetFleet.GetPosition()) <= fleet.GetMaxTurretRange()) {
            SetAllShipsSpeed();
            currentCommandState = CommandType.AttackFleet;
        } else if (currentCommandState == CommandType.AttackFleet) {
            //Sets all idle ships to attackMove to the commands targetPosition
            if (fleet.enemyUnitsInRange.Count > 0) {
                foreach (var ship in fleet.ships) {
                    if (ship.IsIdle()) {
                        AssignShipToAttackFleet(ship, command.targetFleet);
                    }
                }
            }
        }

        return CommandResult.Stop;
    }

    private CommandResult DoAttackUnit(Command command, float deltaTime) {
        if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
            SetShipsIdle();
            command.targetUnit = null;
            return CommandResult.ContinueRemove;
        }

        // Save the postion for DoAttackMove if the target unit is destroyed
        if (command.commandType == CommandType.AttackMoveUnit)
            command.targetPosition = command.targetUnit.GetPosition();

        if (newCommand) {
            newCommand = false;
            currentCommandState = CommandType.FormationLocation;

            // If we are far away form the target unit form up before attacking
            if (Vector2.Distance(fleet.GetPosition(), command.targetUnit.GetPosition()) > fleet.GetMaxTurretRange()) {
                AssignShipsToFormationLocation(command.targetUnit.GetPosition(), fleet.GetSize() / 2);
                return CommandResult.Stop;
            } else {
                SetShipsIdle();
            }
        }


        if (currentCommandState == CommandType.FormationLocation && fleet.AreShipsIdle()) {
            foreach (var ship in fleet.ships) {
                ship.shipAI.AddUnitAICommand(CreateAttackMoveCommand(command.targetUnit, fleet.minShipSpeed));
            }

            currentCommandState = CommandType.Move;
        }

        if (currentCommandState == CommandType.Move
            && Vector2.Distance(fleet.GetPosition(), command.targetUnit.GetPosition()) <= fleet.GetMaxTurretRange() * 1.2) {
            SetAllShipsSpeed();
            currentCommandState = CommandType.AttackMoveUnit;
        }

        return CommandResult.Stop;
    }

    private CommandResult DoProtectUnit(Command command, float deltaTime) {
        if (command.protectUnit == null || !command.protectUnit.IsSpawned()) {
            return CommandResult.ContinueRemove;
        }

        Vector2 newTargetPostion = Vector2.MoveTowards(fleet.GetPosition(), command.protectUnit.GetPosition(),
            Vector2.Distance(fleet.GetPosition(), command.protectUnit.GetPosition()) - fleet.GetSize() + command.protectUnit.GetSize());
        if (newCommand) {
            currentCommandState = CommandType.TurnToPosition;
            command.targetPosition = newTargetPostion;
            SetFleetMoveCommand(newTargetPostion);
            newCommand = false;
            return CommandResult.Stop;
        }

        if (Vector2.Distance(command.targetPosition, newTargetPostion) > 100) {
            currentCommandState = CommandType.TurnToPosition;
            command.targetPosition = newTargetPostion;
            SetFleetMoveCommand(newTargetPostion);
        }

        if (currentCommandState == CommandType.TurnToPosition && fleet.AreShipsIdle()) {
            fleet.NextShipsCommand();
            currentCommandState = CommandType.Move;
            return CommandResult.Stop;
        }

        return CommandResult.Stop;
    }

    private CommandResult DoAttackMove(Command command, float deltaTime) {
        if (newCommand || fleet.AreShipsIdle()) {
            SetFleetAttackMovePosition(command.targetPosition);
            newCommand = false;
        }

        if (Vector2.Distance(fleet.position, command.targetPosition) <= fleet.GetSize())
            return CommandResult.ContinueRemove;
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

    /// <summary>
    /// Sends the ships to construct the station and docks them at it once constructed.
    /// If there is no construction ship in the fleet, the fleet will wait for the station to build.
    /// </summary>
    CommandResult DoBuildStationCommand(Command command, float deltaTime) {
        if (newCommand) {
            SetFleetMoveCommand(command.destinationStation.position, fleet.GetSize() + command.destinationStation.GetSize() + 10);
            Ship constructionShip = fleet.ships.FirstOrDefault(s => s.IsConstructionShip());
            if (constructionShip != null) {
                constructionShip.shipAI.AddUnitAICommand(Command.CreateBuildStationCommand(command.destinationStation),
                    CommandAction.Replace);
            }

            currentCommandState = CommandType.Move;
            newCommand = false;
        }

        if (command.destinationStation.IsBuilt()) {
            fleet.ships.Where(s => s.IsIdle() && s.dockedStation != command.destinationStation).ToList()
                .ForEach(s => s.shipAI.AddUnitAICommand(CreateDockCommand(command.destinationStation, fleet.minShipSpeed),
                    CommandAction.Replace));
            currentCommandState = CommandType.Dock;
        }

        if (fleet.AreShipsIdle() && fleet.ships.All(s => s.dockedStation == command.destinationStation)) {
            return CommandResult.StopRemove;
        }

        return CommandResult.Stop;
    }

    CommandResult DoFormationCommand(Command command, float deltaTime) {
        if (newCommand) {
            AssignShipsToFormation(fleet.GetPosition(), command.targetRotation);
            currentCommandState = CommandType.Formation;
            newCommand = false;
            return CommandResult.Stop;
        }

        if (fleet.AreShipsIdle()) {
            return CommandResult.StopRemove;
        }

        return CommandResult.Stop;
    }

    CommandResult DoFormationLocationCommand(Command command, float deltaTime) {
        if (newCommand) {
            AssignShipsToFormation(command.targetPosition, command.targetRotation);
            currentCommandState = CommandType.FormationLocation;
            newCommand = false;
            return CommandResult.Stop;
        }

        if (fleet.AreShipsIdle()) {
            return CommandResult.StopRemove;
        }

        return CommandResult.Stop;
    }

    CommandResult DoDisbandFleetCommand(Command command, float deltaTime) {
        fleet.DisbandFleet();
        return CommandResult.Stop;
    }

    #endregion

    #region SubCommands

    /// <summary>
    /// Gives each ship in this fleet an AttackFleet command with the targetUnit set to a respective ship in the enemy fleet.
    /// The targetUnit is found based on the relative positioning of the ship to this fleet center and the targetUnit's position to its fleet's center.
    /// </summary>
    private void AssignShipsToAttackFleet(Fleet targetFleet, float maxSpeed = float.MaxValue) {
        foreach (var ship in fleet.ships) {
            AssignShipToAttackFleet(ship, targetFleet, maxSpeed);
        }
    }

    /// <summary>
    /// Gives the ship in this fleet an AttackFleet command with the targetUnit set to a respective ship in the enemy fleet.
    /// The targetUnit is found based on the relative positioning of the ship to this fleet center and the targetUnit's position to its fleet's center.
    /// </summary>
    private void AssignShipToAttackFleet(Ship ship, Fleet targetFleet, float maxSpeed = float.MaxValue) {
        Vector2 shipOffset = fleet.GetPosition() - ship.GetPosition();

        Ship targetShip = null;
        float targetShipDistance = 0;
        foreach (var newTargetShip in targetFleet.ships) {
            Vector2 targetShipOffset = targetFleet.GetPosition() - newTargetShip.GetPosition();
            float newDistance = Vector2.Distance(shipOffset, targetShipOffset);
            if (targetShip == null || newDistance < targetShipDistance) {
                targetShip = newTargetShip;
                targetShipDistance = newDistance;
            }
        }

        ship.shipAI.AddUnitAICommand(CreateSkirmishCommand(targetFleet, targetShip, maxSpeed), CommandAction.Replace);
    }

    private void AssignShipsToFormationLocation(Vector2 targetPosition, float distance) {
        AssignShipsToFormation(Vector2.MoveTowards(fleet.GetPosition(), targetPosition, distance),
            Calculator.GetAngleOutOfTwoPositions(fleet.GetPosition(), targetPosition));
    }

    private void AssignShipsToFormation(Vector2 targetPosition, float targetRotation) {
        (List<Ship>, List<Vector2>) shipTargetPositions =
            FleetFormation.GetFormationShipPosition(fleet, targetPosition, targetRotation, 0f, formationType);
        for (int i = 0; i < shipTargetPositions.Item1.Count; i++) {
            shipTargetPositions.Item1[i].shipAI.AddUnitAICommand(CreateMoveCommand(shipTargetPositions.Item2[i]), CommandAction.Replace);
            shipTargetPositions.Item1[i].shipAI.AddUnitAICommand(CreateRotationCommand(targetRotation));
        }
    }

    #endregion

    #region FleetAIControls

    public void SetShipsIdle() {
        foreach (var ship in fleet.ships) {
            ship.SetIdle();
        }
    }

    public void SetAllShipsSpeed(float maxSpeed = float.MaxValue) {
        foreach (var ship in fleet.ships) {
            ship.shipAI.commands.ForEach(command => command.maxSpeed = maxSpeed);
            ship.SetMaxSpeed(maxSpeed);
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

    public void SetFleetMoveCommand(Vector2 movePosition, float distanceFromPosition) {
        SetFleetMoveCommand(Vector2.MoveTowards(movePosition, fleet.position, distanceFromPosition));
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
    private void SetFleetAttackMovePosition(Vector2 movePosition) {
        foreach (var ship in fleet.ships) {
            Vector2 shipOffset = fleet.GetPosition() - ship.GetPosition();
            ship.shipAI.AddUnitAICommand(CreateAttackMoveCommand(movePosition - shipOffset, fleet.minShipSpeed), CommandAction.Replace);
        }
    }

    public void AddFormationCommand(CommandAction commandAction = CommandAction.Replace) {
        AddFleetAICommand(CreateFormationCommand(fleet.GetShips()[0].rotation), commandAction);
    }

    public void AddFormationCommand(Vector2 position, CommandAction commandAction = CommandAction.Replace) {
        AddFleetAICommand(CreateFormationCommand(position, fleet.GetShips()[0].rotation), commandAction);
    }

    public void AddFormationCommand(Vector2 position, float rotation, CommandAction commandAction = CommandAction.Replace) {
        AddFleetAICommand(CreateFormationCommand(position, rotation), commandAction);
    }

    /// <summary>
    /// Adds a formation command distance towards targetPosition pointing towards targetPosition
    /// </summary>
    /// <param name="targetPosition">the position to point towards</param>
    /// <param name="distance">the distance from the current fleet position</param>
    /// <param name="commandAction">how the command should be added to the list</param>
    public void AddFormationTowardsPositionCommand(Vector2 targetPosition, float distance,
        CommandAction commandAction = CommandAction.Replace) {
        AddFormationCommand(Vector2.MoveTowards(fleet.GetPosition(), targetPosition, distance),
            Calculator.GetAngleOutOfTwoPositions(fleet.GetPosition(), targetPosition), commandAction);
    }

    #endregion

    #region HelperMethods

    public List<Vector3> GetMovementPositionPlan() {
        List<Vector3> positions = new() { fleet.GetPosition() };

        foreach (var command in commands) {
            if (command.commandType == Command.CommandType.Research) {
                if (currentCommandState == CommandType.Dock) {
                    if (command.destinationStation == null) continue;
                    positions.Add(command.destinationStation.GetPosition());
                } else {
                    positions.Add(command.targetStar.GetPosition());
                }
            } else if (command.commandType == CommandType.CollectGas) {
                if (currentCommandState == CommandType.Dock) {
                    if (command.destinationStation == null) continue;
                    positions.Add(command.destinationStation.GetPosition());
                } else {
                    positions.Add(command.targetGasCloud.GetPosition());
                }
            } else if (command.commandType == CommandType.Idle || command.commandType == CommandType.Wait
                || command.commandType == CommandType.TurnToRotation ||
                command.commandType == CommandType.TurnToPosition
                || command.commandType == CommandType.DisbandFleet ||
                command.commandType == CommandType.Formation) { } else if
                (command.commandType == CommandType.Protect) {
                if (command.protectUnit == null) continue;
                positions.Add(command.protectUnit.GetPosition());
            } else if (command.commandType == CommandType.Follow) {
                if (command.targetUnit == null) continue;
                positions.Add(command.targetUnit.GetPosition());
            } else if (command.commandType == CommandType.AttackFleet || command.commandType == CommandType.AttackMoveUnit ||
                command.commandType == CommandType.AttackMove) {
                if (command.targetFleet != null) positions.Add(command.targetFleet.GetPosition());
                if (command.commandType == CommandType.AttackFleet) continue;
                if (command.targetUnit != null) positions.Add(command.targetUnit.GetPosition());
                if (command.commandType == CommandType.AttackMoveUnit) continue;
                positions.Add(command.targetPosition);
            } else if (command.commandType == CommandType.Dock || command.commandType == CommandType.BuildStation) {
                if (command.destinationStation == null) continue;
                positions.Add(command.destinationStation.GetPosition());
            } else if (command.commandType == CommandType.AttackMove) {
                positions.Add(command.targetPosition);
            } else {
                positions.Add(command.targetPosition);
            }
        }

        return positions;
    }

    #endregion
}
