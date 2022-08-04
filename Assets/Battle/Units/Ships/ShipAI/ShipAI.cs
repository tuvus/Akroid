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

    public void SetupShipAI(Ship ship) {
        this.ship = ship;
        commands = new List<UnitAICommand>(10);
    }

    public void AddUnitAICommand(UnitAICommand command, CommandAction commandAction) {
        if ((command.commandType == UnitAICommand.CommandType.AttackMove || command.commandType == UnitAICommand.CommandType.AttackMoveUnit || command.commandType == UnitAICommand.CommandType.Protect) && ship.GetTurrets().Count == 0) {
            return;
        }
        if (commandAction == CommandAction.AddToBegining) {
            ship.SetThrusters(false);
            commands.Insert(0, command);
        }
        if (commandAction == CommandAction.Replace) {
            ship.SetThrusters(false);
            ClearCommands();
            commands.Add(command);
        }
        if (commandAction == CommandAction.AddToEnd) {
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
            return CommandResult.Stop;
        }
        //Waits for a certain amount of time, Stop until the time is up,  ContinueRemove once finished.
        if (command.commandType == UnitAICommand.CommandType.Idle) {
            if (index == -1 || command.waitTime - deltaTime <= 0) {
                return CommandResult.ContinueRemove;
            }
            commands[index] = new UnitAICommand(UnitAICommand.CommandType.Idle, command.waitTime - deltaTime);
            return CommandResult.Stop;
        }
        //Rotates towards angle, Stop until turned to rotation, ContinueRemove once Finished
        if (command.commandType == UnitAICommand.CommandType.TurnToRotation) {
            float localRotation = Calculator.GetLocalTargetRotation(ship.transform.eulerAngles.z, command.targetRotation);
            if (Mathf.Abs(localRotation) <= ship.GetTurnSpeed() * deltaTime) {
                ship.SetRotation(command.targetRotation);
                return CommandResult.ContinueRemove;
            }
            if (localRotation > 0) {
                ship.SetRotation(transform.eulerAngles.z + (ship.GetTurnSpeed() * deltaTime));
            } else {
                ship.SetRotation(transform.eulerAngles.z - (ship.GetTurnSpeed() * deltaTime));
            }
            return CommandResult.Stop;
        }
        //Rptates towards position, Stop until turned to angle, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.TurnToPosition) {
            float localRotation = Calculator.GetAngleOutOfTwoPositions(ship.transform.position, command.targetPosition);
            CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.TurnToRotation, localRotation), deltaTime);
            return result;
        }
        //Rotates towards position then moves towards position, Stop until moved to postion, ContinueRemoveOnce Finished.
        if (command.commandType == UnitAICommand.CommandType.Move) {
            if (ship.dockedStation != null) {
                ship.UndockShip(command.targetPosition);
            }
            float distance = Calculator.GetDistanceToPosition((Vector2)ship.transform.position - command.targetPosition);
            float thrust = ship.GetThrust();
            if (distance <= thrust * deltaTime / ship.GetMass()) {
                ship.transform.position = command.targetPosition;
                ship.SetThrusters(false);
                return CommandResult.ContinueRemove;
            }
            CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.TurnToPosition, command.targetPosition), deltaTime);
            if (result == CommandResult.ContinueRemove || result == CommandResult.Continue) {
                if (distance <= thrust * deltaTime / ship.GetMass()) {
                    ship.transform.position = command.targetPosition;
                    ship.SetThrusters(false);
                    return CommandResult.ContinueRemove;
                }
                transform.Translate(Vector2.up * thrust * deltaTime / ship.GetMass());
                ship.SetThrusters(true);
                return CommandResult.Stop;
            }
            return result;
        }
        //Follows closest enemy ship then goes to target position, Stop until all nearby enemy ships are removed and at target position, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.AttackMove) {
            Unit targetUnit = command.targetUnit;
            if (targetUnit == null || (targetUnit.IsShip() && !((Ship)targetUnit).IsCombatShip()) || !targetUnit.IsSpawned()) {
                targetUnit = GetClosestEnemyUnitInRadius(ship.GetMaxTurretRange() * 2);
                if (commands[index].commandType == UnitAICommand.CommandType.AttackMove) {
                    commands[index] = new UnitAICommand(UnitAICommand.CommandType.AttackMove, targetUnit, command.targetPosition);
                }
                if (targetUnit == null || !targetUnit.IsSpawned()) {
                    CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.Move, command.targetPosition), deltaTime);
                    return result;
                }
            }
            float distance = Vector2.Distance(ship.transform.position, targetUnit.transform.position) - (ship.GetSize() + targetUnit.GetSize());
            if (distance > ship.GetMinTurretRange() / 2) {
                CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.Move, Vector3.MoveTowards(ship.transform.position, targetUnit.transform.position, distance)), deltaTime);
                if (result == CommandResult.ContinueRemove)
                    ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.TurnToRotation, Calculator.GetAngleOutOfTwoPositions(ship.GetPosition(), targetUnit.GetPosition()) + ship.GetCombatRotation()), deltaTime);
                return CommandResult.Stop;
            }
            ship.SetThrusters(false);
            ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.TurnToRotation, Calculator.GetAngleOutOfTwoPositions(ship.GetPosition(), targetUnit.GetPosition()) + ship.GetCombatRotation()), deltaTime);
            return CommandResult.Stop;
        }
        //Follows enemy ship, Stop until enemy ship is destroyed, ContinueRemove once Finished.
        if (command.commandType == UnitAICommand.CommandType.AttackMoveUnit) {
            if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
                if (command.useAlternateCommandOnceDone) {
                    commands[index] = new UnitAICommand(UnitAICommand.CommandType.AttackMove, command.targetPosition);
                    return CommandResult.Stop;
                } else {
                    return CommandResult.ContinueRemove;

                }
            }
            commands[index] = new UnitAICommand(UnitAICommand.CommandType.AttackMoveUnit, command.targetUnit, true);
            float distance = Vector2.Distance(ship.transform.position, command.targetUnit.transform.position) - (ship.GetSize() + command.targetUnit.GetSize());
            if (distance > ship.GetMinTurretRange() / 2) {
                CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.Move, Vector3.MoveTowards(ship.transform.position, command.targetUnit.transform.position, distance)), deltaTime);
                if (result == CommandResult.ContinueRemove) {
                    ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.TurnToRotation, Calculator.GetAngleOutOfTwoPositions(ship.GetPosition(), command.targetUnit.GetPosition()) + ship.GetCombatRotation()), deltaTime);
                }
                return CommandResult.Stop;
            }
            ship.SetThrusters(false);
            ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.TurnToRotation, Calculator.GetAngleOutOfTwoPositions(ship.GetPosition(), command.targetUnit.GetPosition()) + ship.GetCombatRotation()), deltaTime);
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
            if (command.targetUnit == null || !command.targetUnit.IsStation() || (!command.targetUnit.IsSpawned() && ((Station)command.targetUnit).IsBuilt())) {
                ship.SetThrusters(false);
                return CommandResult.ContinueRemove;
            }
            float distance = Vector2.Distance(ship.transform.position, (Vector2)command.targetUnit.transform.position);
            if (distance > ship.GetSize() + command.targetUnit.GetSize()) {
                CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.Move, (Vector2)command.targetUnit.transform.position), deltaTime);
                if (result == CommandResult.Stop)
                    return CommandResult.Stop;
            }
            ship.DockShip((Station)command.targetUnit);
            ship.SetThrusters(false);
            return CommandResult.StopRemove;
        }
        //AttackMove to the star, do reasearch, then remove command.
        if (command.commandType == UnitAICommand.CommandType.Reserch) {
            if (command.targetStar == null) {
                return CommandResult.StopRemove;
            }
            float distance = Vector2.Distance(ship.transform.position, command.targetStar.GetPosition());
            if (distance > ship.GetSize() + command.targetStar.GetSize() * 2) {
                CommandResult result = ResolveCommand(new UnitAICommand(UnitAICommand.CommandType.AttackMove, (Vector2)command.targetStar.GetPosition()), deltaTime, index);
                if (result == CommandResult.Stop) {
                    return CommandResult.Stop;
                }
            }
            ship.SetThrusters(false);
            if (ship.GetResearchEquiptment().GatherData(command.targetStar, deltaTime)) {
                return CommandResult.StopRemove;
            }
            return CommandResult.Stop;
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
