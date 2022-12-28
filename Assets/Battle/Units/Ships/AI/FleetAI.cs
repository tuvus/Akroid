using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using static Command;

public class FleetAI : MonoBehaviour {
    Faction faction;
    enum CommandResult {
        Stop = 0,
        StopRemove = 1,
        ContinueRemove = 2,
        Continue = 3,
    }

    string fleetName;
    [SerializeField] List<Ship> ships;
    public List<Command> commands;
    public bool newCommand;
    public CommandType currentCommandType;
    float minFleetSpeed;

    public void SetupFleetAI(Faction faction, string fleetName, Ship ship) {
        SetupFleetAI(faction, fleetName, new List<Ship>() { ship });
    }

    public void SetupFleetAI(Faction faction, string fleetName, List<Ship> ships) {
        this.faction = faction;
        this.fleetName = fleetName;
        this.ships = new List<Ship>(ships.Count * 2);
        commands = new List<Command>(10);
        for (int i = 0; i < ships.Count; i++) {
            AddShip(ships[i], false);
        }
        minFleetSpeed = GetMinShipSpeed();
        SetFormation();
    }

    public void DisbandFleet() {
        foreach (Ship ship in ships) {
            ship.fleet = null;
        }
        faction.RemoveFleet(this);
        Destroy(gameObject);
    }

    public void AddShip(Ship ship, bool setMinSpeed = true) {
        ships.Add(ship);
        if (ship.fleet != null) {
            ship.fleet.RemoveShip(ship);
        }
        ship.fleet = this;
        if (setMinSpeed)
            minFleetSpeed = GetMinShipSpeed();
    }

    public void RemoveShip(Ship ship) {
        ships.Remove(ship);
        ship.fleet = null;
        if (ships.Count == 0) {
            DisbandFleet();
        } else {
            minFleetSpeed = GetMinShipSpeed();
        }
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
            Profiler.BeginSample("FleetAI ResolveCommand " + commands[0].commandType);
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
        //Waits for a certain amount of time, Stop until the time is up,  ContinueRemove once finished.
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
        //Rotates towards angle, Stop until turned to rotation, ContinueRemove once Finished
        if (command.commandType == CommandType.TurnToRotation) {
            if (newCommand) {
                currentCommandType = CommandType.TurnToRotation;
                SetFleetRotation(command.targetRotation);
                newCommand = false;
            }
            if (IsFleetIdle()) {
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
            if (IsFleetIdle()) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        //Rotates towards position then moves towards position, Stop until moved to postion, ContinueRemoveOnce Finished.
        if (command.commandType == CommandType.Move) {
            if (newCommand) {
                currentCommandType = CommandType.TurnToPosition;
                SetFleetMovePosition(command.targetPosition);
                newCommand = false;
                return CommandResult.Stop;
            }

            if (currentCommandType == CommandType.TurnToPosition && IsFleetIdle()) {
                NextShipsCommand();
                currentCommandType = CommandType.Move;
                return CommandResult.Stop;
            }

            if (currentCommandType == CommandType.Move && IsFleetIdle()) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }

        //Follows closest enemy ship then goes to target position, Stop until all nearby enemy ships are removed and at target position, ContinueRemove once Finished.
        //Follows closest enemy ship then follows freindly ship, Stop until friendly ship is destroyed, Creates an attackMoveCommand on current position once the friendly ship is destroyed.
        if (command.commandType == CommandType.AttackMove || command.commandType == CommandType.AttackMoveUnit || command.commandType == CommandType.Protect) {
            if (newCommand) {
                if (command.commandType == CommandType.Protect) {
                    Vector2 fleetCenter = GetFleetCenter();
                    command.targetPosition = Vector2.MoveTowards(fleetCenter, command.protectUnit.GetPosition(), Vector2.Distance(fleetCenter, command.protectUnit.GetPosition()) - (GetMaxShipSize() + command.protectUnit.GetSize()) * 2);
                } else if (command.commandType == CommandType.AttackMoveUnit) {
                    Vector2 fleetCenter = GetFleetCenter();
                    command.targetPosition = Vector2.MoveTowards(fleetCenter, command.targetUnit.GetPosition(), Vector2.Distance(fleetCenter, command.targetUnit.GetPosition()) - (GetMaxShipSize() + command.targetUnit.GetSize()) * 2);
                }
                SetFleetAttackMovePosition(command.targetPosition);
                currentCommandType = CommandType.Move;
                newCommand = false;
                return CommandResult.Stop;
            }
            if (currentCommandType == CommandType.Move && Vector2.Distance(GetFleetCenter(), command.targetPosition) <= GetFleetSize()) {
                return CommandResult.ContinueRemove;
            }
            return CommandResult.Stop;
        }
        if (command.commandType == CommandType.Formation) {
            if (newCommand) {
                float size = GetMaxShipSize();
                Vector2 fleetCenter = GetFleetCenter();
                Vector2 startPosition = fleetCenter - Calculator.GetPositionOutOfAngleAndDistance(command.targetRotation - 90, size * ships.Count * 2);
                Vector2 endPosition = fleetCenter - Calculator.GetPositionOutOfAngleAndDistance(command.targetRotation + 90, size * ships.Count * 2);
                for (int i = 0; i < ships.Count; i++) {
                    ships[i].shipAI.AddUnitAICommand(CreateMoveCommand(Vector2.Lerp(startPosition, endPosition, i / (float)(ships.Count - 1))), CommandAction.Replace);
                    ships[i].shipAI.AddUnitAICommand(CreateRotationCommand(command.targetRotation));
                }
                currentCommandType = CommandType.Formation;
                newCommand = false;
                return CommandResult.Stop;
            }
            if (currentCommandType == CommandType.Formation && IsFleetIdle()) {
                return CommandResult.StopRemove;
            }

        }
        if (command.commandType == CommandType.FormationLocation) {
            if (newCommand) {
                float size = GetMaxShipSize();
                Vector2 startPosition = command.targetPosition - Calculator.GetPositionOutOfAngleAndDistance(command.targetRotation - 90, size * ships.Count * 2);
                Vector2 endPosition = command.targetPosition - Calculator.GetPositionOutOfAngleAndDistance(command.targetRotation + 90, size * ships.Count * 2);
                for (int i = 0; i < ships.Count; i++) {
                    ships[i].shipAI.AddUnitAICommand(CreateMoveCommand(Vector2.Lerp(startPosition, endPosition, i / (float)(ships.Count - 1))), CommandAction.Replace);
                    ships[i].shipAI.AddUnitAICommand(CreateRotationCommand(command.targetRotation));
                }
                currentCommandType = CommandType.Formation;
                newCommand = false;
                return CommandResult.Stop;
            }
            if (currentCommandType == CommandType.Formation && IsFleetIdle()) {
                return CommandResult.StopRemove;
            }
        }
        return CommandResult.Stop;
    }

    public void SetFleetIdle() {
        for (int i = 0; i < ships.Count; i++) {
            ships[i].SetIdle();
        }
    }

    public void SetFleetRotation(float rotation) {
        for (int i = 0; i < ships.Count; i++) {
            ships[i].SetTargetRotate(rotation);
        }
    }

    public void SetFleetRotation(Vector2 targetPostion) {
        for (int i = 0; i < ships.Count; i++) {
            ships[i].SetTargetRotate(targetPostion);
        }
    }

    public void SetFleetMovePosition(Vector2 movePosition) {
        for (int i = 0; i < ships.Count; i++) {
            Vector2 shipOffset = GetFleetCenter() - ships[i].GetPosition();
            ships[i].shipAI.AddUnitAICommand(CreateRotationCommand(movePosition - shipOffset), CommandAction.Replace);
            ships[i].shipAI.AddUnitAICommand(CreateIdleCommand());
            ships[i].shipAI.AddUnitAICommand(CreateMoveCommand(movePosition - shipOffset, minFleetSpeed));
        }
    }

    public void SetFleetMovePositionOffset(Vector2 movePosition, float targetSize) {
        for (int i = 0; i < ships.Count; i++) {
            Vector2 shipOffset = GetFleetCenter() - ships[i].GetPosition();
            ships[i].SetMovePosition(movePosition - shipOffset, (GetFleetSize() + targetSize) * 2);
        }
    }

    public void SetFleetAttackMovePosition(Vector2 movePosition) {
        for (int i = 0; i < ships.Count; i++) {
            Vector2 shipOffset = GetFleetCenter() - ships[i].GetPosition();
            ships[i].shipAI.AddUnitAICommand(CreateAttackMoveCommand(movePosition - shipOffset, minFleetSpeed), CommandAction.Replace);
        }
    }

    public void SetFormation(CommandAction commandAction = CommandAction.Replace) {
        AddUnitAICommand(CreateFormationCommand(ships[0].GetRotation()), commandAction);
    }

    public void SetFormation(Vector2 position, CommandAction commandAction = CommandAction.Replace) {
        AddUnitAICommand(CreateFormationCommand(position, ships[0].GetRotation()), commandAction);
    }

    public void SetFormation(Vector2 position, float rotation, CommandAction commandAction = CommandAction.Replace) {
        AddUnitAICommand(CreateFormationCommand(position, rotation), commandAction);
    }

    public void NextShipsCommand() {
        for (int i = 0; i < ships.Count; i++) {
            ships[i].shipAI.NextCommand();
        }
    }

    public bool IsFleetIdle() {
        for (int i = 0; i < ships.Count; i++) {
            if (!ships[i].IsIdle())
                return false;
        }
        return true;
    }

    public Vector2 GetFleetCenter() {
        Vector2 sum = ships[0].GetPosition();
        for (int i = 1; i < ships.Count; i++) {
            sum += ships[i].GetPosition();
        }
        return sum / ships.Count;
    }

    public float GetFleetSize() {
        Vector2 center = GetFleetCenter();
        float size = ships[0].GetSize();
        for (int i = 1; i < ships.Count; i++) {
            size = Math.Max(size, Vector2.Distance(center, ships[i].GetPosition()) + ships[i].GetSize());
        }
        return size;
    }

    public int GetTotalFleetHealth() {
        int totalHealth = 0;
        for (int i = 0; i < ships.Count; i++) {
            totalHealth += ships[i].GetTotalHealth();
        }
        return totalHealth;
    }

    Unit GetClosestEnemyUnitInRadius(float radius) {
        Unit targetUnit = null;
        float distance = 0;
        //for (int i = 0; i < ship.enemyUnitsInRange.Count; i++) {
        //    Unit tempUnit = ship.enemyUnitsInRange[i];
        //    float tempDistance = Vector2.Distance(ship.transform.position, tempUnit.transform.position);
        //    if (tempDistance <= radius && (targetUnit == null || tempDistance < distance)) {
        //        targetUnit = tempUnit;
        //        distance = tempDistance;
        //    }
        //}
        return targetUnit;
    }

    float GetMinShipSpeed() {
        float minSpeed = float.MaxValue;
        for (int i = 0; i < ships.Count; i++) {
            minSpeed = Math.Min(ships[i].GetSpeed(), minSpeed);
        }
        return minSpeed;
    }

    float GetMaxShipSize() {
        float maxShipSize = 0;
        for (int i = 0; i < ships.Count; i++) {
            maxShipSize = Math.Max(maxShipSize, ships[i].GetSize());
        }
        return maxShipSize;
    }

    public List<Ship> GetAllShips() {
        return ships;
    }

    public void SelectFleet(UnitSelection.SelectionStrength strength = UnitSelection.SelectionStrength.Unselected) {
        foreach (Ship ship in ships) {
            ship.SelectUnit(strength);
        }
    }

    public void UnselectFleet() {
        SelectFleet(UnitSelection.SelectionStrength.Unselected);
    }
}