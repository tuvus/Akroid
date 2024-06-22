using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Command {
    public enum CommandType {
        Idle,
        Wait,
        TurnToRotation,
        TurnToPosition,
        Move,
        AttackMove,
        AttackMoveUnit,
        AttackFleet,
        Follow,
        Protect,
        Formation,
        FormationLocation,
        Dock,
        UndockCommand,
        Transport,
        TransportDelay,
        Research,
        DisbandFleet,
    }

    public enum CommandAction {
        AddToBegining = -1,
        Replace = 0,
        AddToEnd = 1,
    }

    public CommandType commandType;

    public float waitTime;
    public float targetRotation;
    public Vector2 targetPosition;
    public Fleet targetFleet;
    public Unit targetUnit;
    public Unit protectUnit;
    public Star targetStar;
    public bool useAlternateCommandOnceDone;

    public Station productionStation;
    public Station destinationStation;
    public string cargoType;

    public float maxSpeed;

    private Command(CommandType commandType) {
        this.commandType = commandType;
        maxSpeed = float.MaxValue;
    }

    public static Command CreateIdleCommand() {
        Command newCommand = new Command(CommandType.Idle);
        return newCommand;
    }

    public static Command CreateWaitCommand(float waitTime) {
        Command newCommand = new Command(CommandType.Wait);
        newCommand.waitTime = waitTime;
        return newCommand;
    }

    public static Command CreateRotationCommand(float rotation) {
        Command newCommand = new Command(CommandType.TurnToRotation);
        newCommand.targetRotation = rotation;
        return newCommand;
    }

    public static Command CreateRotationCommand(Vector2 targetPosition) {
        Command newCommand = new Command(CommandType.TurnToPosition);
        newCommand.targetPosition = targetPosition;
        return newCommand;
    }

    public static Command CreateFormationCommand(float rotation) {
        Command newCommand = new Command(CommandType.Formation);
        newCommand.targetRotation = rotation;
        return newCommand;
    }

    public static Command CreateFormationCommand(Vector2 targetPosition, float rotation) {
        Command newCommand = new Command(CommandType.FormationLocation);
        newCommand.targetPosition = targetPosition;
        newCommand.targetRotation = rotation;
        return newCommand;
    }

    public static Command CreateMoveCommand(Vector2 targetPosition, float maxSpeed = float.MaxValue) {
        Command newCommand = new Command(CommandType.Move);
        newCommand.targetPosition = targetPosition;
        newCommand.maxSpeed = maxSpeed;
        return newCommand;
    }

    public static Command CreateMoveOffsetCommand(Vector2 currentPosition, Vector2 targetPosition, float offset, float maxSpeed = float.MaxValue) {
        Command newCommand = new Command(CommandType.Move);
        newCommand.targetPosition = Vector2.MoveTowards(currentPosition, targetPosition, Vector2.Distance(currentPosition, targetPosition) - offset);
        newCommand.maxSpeed = maxSpeed;
        return newCommand;
    }

    public static Command CreateAttackMoveCommand(Vector2 targetPosition, float maxSpeed = float.MaxValue) {
        Command newCommand = new Command(CommandType.AttackMove);
        newCommand.targetPosition = targetPosition;
        newCommand.maxSpeed = maxSpeed;
        newCommand.waitTime = Random.Range(0, 0.2f);
        return newCommand;
    }

    public static Command CreateAttackMoveCommand(Unit targetUnit, float maxSpeed = float.MaxValue, bool useAlternateCommandOnceDone = false) {
        Command newCommand = new Command(CommandType.AttackMoveUnit);
        newCommand.targetUnit = targetUnit;
        newCommand.maxSpeed = maxSpeed;
        newCommand.useAlternateCommandOnceDone = useAlternateCommandOnceDone;
        newCommand.waitTime = Random.Range(0, 0.2f);
        return newCommand;
    }

    public static Command CreateAttackFleetCommand(Fleet targetFleet, Ship targetShip = null) {
        Command newCommand = new Command(CommandType.AttackFleet);
        newCommand.targetFleet = targetFleet;
        newCommand.maxSpeed = float.MaxValue;
        newCommand.targetUnit = targetShip;
        return newCommand;
    }

    public static Command CreateSkirmishCommand(Fleet targetFleet, Ship targetShip) {
        Command newCommand = new Command(CommandType.AttackFleet);
        newCommand.targetUnit = targetShip;
        newCommand.targetFleet = targetFleet;
        newCommand.maxSpeed = float.MaxValue;
        return newCommand;
    }

    public static Command CreateFollowCommand(Unit targetUnit, float maxSpeed = float.MaxValue) {
        Command newCommand = new Command(CommandType.Follow);
        newCommand.targetUnit = targetUnit;
        newCommand.maxSpeed = maxSpeed;
        return newCommand;
    }

    public static Command CreateProtectCommand(Unit protectUnit, float maxSpeed = float.MaxValue) {
        Command newCommand = new Command(CommandType.Protect);
        newCommand.protectUnit = protectUnit;
        newCommand.maxSpeed = maxSpeed;
        return newCommand;
    }

    public static Command CreateDockCommand(Station destinationStation, float maxSpeed = float.MaxValue) {
        Command newCommand = new Command(CommandType.Dock);
        newCommand.destinationStation = destinationStation;
        newCommand.maxSpeed = maxSpeed;
        return newCommand;
    }

    public static Command CreateUndockCommand() {
        return CreateUndockCommand(Random.Range(0, 360f));
    }

    public static Command CreateUndockCommand(float rotation) {
        Command newCommand = new Command(CommandType.UndockCommand);
        newCommand.targetRotation = rotation;
        return newCommand;
    }

    public static Command CreateTransportCommand(Station productionStation, Station destinationStation, bool oneTrip = false) {
        Command newCommand = new Command(CommandType.Transport);
        newCommand.destinationStation = destinationStation;
        newCommand.productionStation = productionStation;
        newCommand.useAlternateCommandOnceDone = oneTrip;
        return newCommand;
    }

    public static Command CreateTransportDelayCommand(Station productionStation, Station destinationStation, float delay) {
        Command newCommand = new Command(CommandType.TransportDelay);
        newCommand.destinationStation = destinationStation;
        newCommand.productionStation = productionStation;
        newCommand.waitTime = delay;
        newCommand.targetRotation = delay;
        return newCommand;
    }

    public static Command CreateResearchCommand(Star targetStar, Station returnStation) {
        Command newCommand = new Command(CommandType.Research);
        newCommand.destinationStation = returnStation;
        newCommand.targetStar = targetStar;
        return newCommand;
    }

    public static Command CreateDisbandFleetCommand() {
        Command newCommand = new Command(CommandType.DisbandFleet);
        return newCommand;
    }

    public bool IsAttackCommand() {
        return commandType == CommandType.AttackMove || commandType == CommandType.AttackMoveUnit || commandType == CommandType.AttackFleet;
    }
}
