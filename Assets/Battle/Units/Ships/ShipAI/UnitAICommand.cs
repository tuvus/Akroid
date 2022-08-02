using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct UnitAICommand {
    public enum CommandType {
        Idle,
        Wait,
        TurnToRotation,
        TurnToPosition,
        Move,
        AttackMove,
        AttackMoveUnit,
        Follow,
        Protect,
        Formation,
        FormationRotation,
        Dock,
        Transport,
        Reserch,
    }
    public CommandType commandType;

    public float waitTime;
    public float targetRotation;
    public Vector2 targetPosition;
    public Unit targetUnit;
    public Star targetStar;
    //public Station.StationData targetStation;


    //public Station.StationData productionStation;
    //public Station.StationData destinationStation;
    public string cargoType;
    public UnitAICommand(CommandType idle) {
        this.commandType = idle;
        waitTime = 0;
        targetRotation = 0;
        targetPosition = Vector3.zero;
        targetUnit = null;
        cargoType = null;
        targetStar = null;
    }

    /// <param name="commandType">Type of command</param>
    /// <param name="value">A 0 to 360 degree rotation or a wait time</param>
    public UnitAICommand(CommandType waitOrRotate, float value) {
        commandType = waitOrRotate;
        waitTime = 0;
        targetRotation = 0;
        targetPosition = Vector3.zero;
        targetUnit = null;
        cargoType = null;
        if (commandType == CommandType.Idle)
            waitTime = value;
        if (commandType == CommandType.TurnToRotation)
            targetRotation = value;
        targetStar = null;
    }

    public UnitAICommand(CommandType research, Star star) {
        commandType = research;
        waitTime = 0;
        targetRotation = 0;
        targetPosition = Vector3.zero;
        targetUnit = null;
        cargoType = null;
        waitTime = 0;
        targetRotation = 0;
        targetStar = star;
    }

    public UnitAICommand(CommandType moveOrRotateTowardsOrAttackMoveLocation, Vector2 value) {
        this.commandType = moveOrRotateTowardsOrAttackMoveLocation;
        waitTime = 0;
        targetRotation = 0;
        targetPosition = value;
        targetUnit = null;
        cargoType = null;
        targetStar = null;
    }

    public UnitAICommand(CommandType followOrAttackMoveUnitOrProtectOrDock, Unit unit) {
        this.commandType = followOrAttackMoveUnitOrProtectOrDock;
        waitTime = 0;
        targetRotation = 0;
        targetPosition = Vector2.zero;
        targetUnit = unit;
        cargoType = null;
        targetStar = null;
    }

    public UnitAICommand(CommandType formationOrAttackMovePosition, Unit unit, Vector2 offset) {
        this.commandType = formationOrAttackMovePosition;
        waitTime = 0;
        targetRotation = 0;
        targetPosition = offset;
        targetUnit = unit;
        cargoType = null;
        targetStar = null;
    }

    public UnitAICommand(CommandType formationRotation, Unit unit, float rotation, Vector2 offset) {
        this.commandType = formationRotation;
        waitTime = 0;
        targetRotation = rotation;
        targetPosition = offset;
        targetUnit = unit;
        cargoType = null;
        targetStar = null;
    }

}
