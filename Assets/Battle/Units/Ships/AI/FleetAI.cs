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

    public void DestroyFleet() {
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
            DestroyFleet();
        } else {
            minFleetSpeed = GetMinShipSpeed();
        }
    }

    public void AddUnitAICommand(Command command, CommandAction commandAction = CommandAction.AddToEnd) {
        if ((command.commandType == CommandType.AttackMove || command.commandType == CommandType.AttackMoveUnit || command.commandType == CommandType.Protect)) {
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
        if (command.commandType == CommandType.AttackMove || command.commandType == CommandType.Protect) {
            //if (command.commandType == CommandType.Protect && command.protectUnit == null) {
            //    command.commandType = CommandType.AttackMove;
            //    command.protectUnit = null;
            //}
            //if (currentCommandType != CommandType.Move && (command.targetUnit == null || !command.targetUnit.IsSpawned() || Vector2.Distance(GetFleetCenter(), command.targetUnit.GetPosition()) > GetFleetSize() * 1.5f) {
            //    newCommand = true;
            //    command.targetUnit = null;
            //}
            //if (newCommand) {
            //    if (command.commandType == CommandType.Protect) {
            //        SetFleetMovePositionOffset(command.protectUnit.GetPosition(), command.protectUnit.GetSize());;
            //        command.targetPosition = command.protectUnit.GetPosition();
            //    } else {
            //        SetFleetMovePosition(command.targetPosition);
            //    }
            //    currentCommandType = CommandType.Move;
            //    newCommand = false;
            //}

            //if (currentCommandType == CommandType.Move) {
            //    command.targetUnit = GetClosestEnemyUnitInRadius(GetFleetSize() * 1.5f);
            //    if (command.targetUnit != null) {
            //        if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) < ship.GetMinWeaponRange()) {
            //            currentCommandType = CommandType.TurnToRotation;
            //        } else {
            //            currentCommandType = CommandType.AttackMove;
            //            SetFleetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
            //        }
            //    } else if (IsFleetIdle()) {
            //        if (command.commandType == CommandType.Protect) {
            //            if (Vector2.Distance(ship.GetPosition(), command.protectUnit.GetPosition()) > (ship.GetSize() + command.protectUnit.GetSize()) * 3)
            //                SetFleetMovePositionOffset(command.protectUnit.GetPosition(), command.protectUnit.GetSize());;
            //            return CommandResult.Stop;
            //        }
            //        return CommandResult.ContinueRemove;
            //    } else {
            //        if (command.commandType == CommandType.Protect) {
            //            SetFleetMovePositionOffset(command.protectUnit.GetPosition(), command.protectUnit.GetSize());;
            //            command.targetPosition = ship.GetTargetMovePosition();
            //        }
            //        return CommandResult.Stop;
            //    }
            //}

            //if (currentCommandType == CommandType.AttackMove) {
            //    if (IsFleetIdle() || Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) <= ship.GetMinWeaponRange() * .8f) {
            //        currentCommandType = CommandType.TurnToRotation;
            //    } else {
            //        SetFleetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
            //        return CommandResult.Stop;
            //    }
            //}

            //if (currentCommandType == CommandType.TurnToRotation) {
            //    if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMinWeaponRange()) {
            //        currentCommandType = CommandType.AttackMove;
            //    } else {
            //        SetFleetRotation(command.targetUnit.GetPosition(), ship.GetCombatRotation());
            //    }
            //}
            //return CommandResult.Stop;
        }
        //Follows enemy ship, Stop until enemy ship is destroyed, ContinueRemove once Finished.
        if (command.commandType == CommandType.AttackMoveUnit) {
            //    if (command.targetUnit == null || !command.targetUnit.IsSpawned()) {
            //        command.commandType = CommandType.AttackMove;
            //        if (newCommand) {
            //            command.targetPosition = ship.GetPosition();
            //        }
            //        newCommand = true;
            //        return CommandResult.Stop;
            //    }
            //    if (newCommand) {
            //        currentCommandType = CommandType.Move;
            //        SetFleetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
            //        command.targetPosition = command.targetUnit.GetPosition();
            //        newCommand = false;
            //    }

            //    if (currentCommandType == CommandType.Move) {
            //        if (IsFleetIdle() || Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) <= ship.GetMinWeaponRange() * .8f) {
            //            currentCommandType = CommandType.TurnToRotation;
            //        } else {
            //            SetFleetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
            //            return CommandResult.Stop;
            //        }
            //    }

            //    if (currentCommandType == CommandType.TurnToRotation) {
            //        if (Vector2.Distance(ship.GetPosition(), command.targetUnit.GetPosition()) > ship.GetMinWeaponRange()) {
            //            currentCommandType = CommandType.Move;
            //            SetFleetMovePosition(command.targetUnit.GetPosition(), ship.GetMinWeaponRange() * .8f);
            //        } else {
            //            SetFleetRotation(command.targetUnit.GetPosition(), ship.GetCombatRotation());
            //        }
            //    }
            //    return CommandResult.Stop;
            //}
            ////Follows friendly ship, Continue until friendly ship is destroyed, ContinueRemove once Finished.
            //if (command.commandType == CommandType.Follow) {
            //    if (newCommand) {
            //        SetFleetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
            //        newCommand = false;
            //        return CommandResult.Stop;
            //    }
            //    if (command.targetUnit == null) {
            //        ship.SetIdle();
            //        return CommandResult.ContinueRemove;
            //    }
            //    SetFleetMovePosition(command.targetUnit.GetPosition(), ship.GetSize() + command.targetUnit.GetSize());
            //    return CommandResult.Stop;
            //}
            ////Follows the friendly ship in a formation, Continue until friendly ship formation leader is destroyed, ContinueRemove once Finished.
            //if (command.commandType == CommandType.Formation) {
            //    if (command.targetUnit == null) {
            //        return CommandResult.ContinueRemove;
            //    }
            //    float distance = Vector2.Distance(ship.transform.position, (Vector2)command.targetUnit.transform.position + command.targetPosition);
            //    if (distance > ship.GetTurnSpeed() * deltaTime / 10) {
            //        CommandResult result = ResolveCommand(new Command(CommandType.Move, Vector3.MoveTowards(ship.transform.position, (Vector2)command.targetUnit.transform.position + command.targetPosition, distance)), deltaTime);
            //        if (result == CommandResult.ContinueRemove || result == CommandResult.Continue) {
            //            return CommandResult.Continue;
            //        }
            //    }
            //    transform.position = (Vector2)command.targetUnit.transform.position + command.targetPosition;
            //    return CommandResult.Continue;
            //}
            ////Follows the friendly ship in a formation relative to thier rotation, Continue until friendly ship formation leader is destroyed, ContinueRemove once Finished.
            //if (command.commandType == CommandType.FormationRotation) {
            //    if (command.targetUnit == null) {
            //        return CommandResult.ContinueRemove;
            //    }

            //    float targetAngle = command.targetRotation - command.targetUnit.GetRotation();
            //    float distanceToTargetAngle = Calculator.GetDistanceToPosition(command.targetPosition);
            //    Vector2 targetOffsetPosition = Calculator.GetPositionOutOfAngleAndDistance(targetAngle + Calculator.GetAngleOutOfPosition(command.targetPosition), distanceToTargetAngle);
            //    float distance = Vector2.Distance(ship.transform.position, (Vector2)command.targetUnit.transform.position + targetOffsetPosition);
            //    if (distance > ship.GetThrust() * deltaTime / 10) {
            //        CommandResult result = ResolveCommand(new Command(CommandType.Move, (Vector2)command.targetUnit.transform.position + targetOffsetPosition), deltaTime);
            //        if (result == CommandResult.Stop || result == CommandResult.StopRemove) {
            //            return CommandResult.Stop;
            //        }
            //    }
            //    ship.transform.position = (Vector2)command.targetUnit.transform.position + targetOffsetPosition;
            //    CommandResult rotationResult = ResolveCommand(new Command(CommandType.TurnToRotation, command.targetUnit.GetRotation()), deltaTime);
            //    if (rotationResult == CommandResult.ContinueRemove || rotationResult == CommandResult.Continue) {
            //        return CommandResult.Continue;
            //    }
            //    return CommandResult.Stop;
            //}
            ////Goes to then docks at the station.
            //if (command.commandType == CommandType.Dock) {
            //    if (newCommand) {
            //        if (command.destinationStation != null) {
            //            ship.SetDockTarget(command.destinationStation);
            //        } else {
            //            ship.SetIdle();
            //            return CommandResult.ContinueRemove;
            //        }
            //        newCommand = false;
            //    }
            //    if (command.destinationStation == null || (IsFleetIdle() && ship.dockedStation == command.destinationStation)) {
            //        ship.SetIdle();
            //        return CommandResult.StopRemove;
            //    }
            //    return CommandResult.Stop;
            //}
            ////AttackMove to the star, do reasearch, then remove command.
            //if (command.commandType == CommandType.Research) {
            //    if (command.targetStar == null || command.destinationStation == null) {
            //        return CommandResult.StopRemove;
            //    }
            //    if (newCommand) {
            //        if (ship.GetResearchEquiptment().WantMoreData()) {
            //            SetFleetMovePosition(command.targetStar.GetPosition(), ship.GetSize() + command.targetStar.GetSize() * 2);
            //            currentCommandType = CommandType.Move;
            //        } else {
            //            ship.SetDockTarget(command.destinationStation);
            //            currentCommandType = CommandType.Dock;
            //        }
            //        newCommand = false;
            //    }
            //    if (IsFleetIdle()) {
            //        if (currentCommandType == CommandType.Move) {
            //            currentCommandType = CommandType.Research;
            //            return CommandResult.Stop;
            //        } else if (currentCommandType == CommandType.Research) {
            //            ship.GetResearchEquiptment().GatherData(command.targetStar, deltaTime);
            //            if (!ship.GetResearchEquiptment().WantMoreData()) {
            //                ship.SetDockTarget(command.destinationStation);
            //                currentCommandType = CommandType.Dock;
            //            }
            //            return CommandResult.Stop;
            //        } else if (currentCommandType == CommandType.Dock) {
            //            currentCommandType = CommandType.Wait;
            //            return CommandResult.Stop;
            //        } else if (currentCommandType == CommandType.Wait) {
            //            if (ship.GetResearchEquiptment().WantMoreData()) {
            //                SetFleetMovePosition(command.targetStar.GetPosition(), ship.GetSize() + command.targetStar.GetSize() * 2);
            //                currentCommandType = CommandType.Move;
            //            }
            //            return CommandResult.Stop;
            //        }
            //    }
            //    return CommandResult.Stop;
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

    public void SetFormation(CommandAction commandAction = CommandAction.Replace) {
        AddUnitAICommand(CreateFormationCommand(ships[0].GetRotation()), commandAction);
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