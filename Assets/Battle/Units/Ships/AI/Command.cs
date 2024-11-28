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
        CollectGas,
        DisbandFleet,
        Colonize,
        BuildStation,
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
    public GasCloud targetGasCloud;
    public Planet targetPlanet;
    public bool useAlternateCommandOnceDone;
    public CargoBay.CargoTypes cargoType;

    public Station productionStation;
    public Station destinationStation;

    public float maxSpeed;

    private Command(CommandType commandType) {
        this.commandType = commandType;
        maxSpeed = float.MaxValue;
    }

    public static Command CreateIdleCommand() {
        return new Command(CommandType.Idle);
    }

    public static Command CreateWaitCommand(float waitTime) {
        return new Command(CommandType.Wait) {
            waitTime = waitTime
        };
    }

    public static Command CreateRotationCommand(float rotation) {
        return new Command(CommandType.TurnToRotation) {
            targetRotation = rotation
        };
    }

    public static Command CreateRotationCommand(Vector2 targetPosition) {
        return new Command(CommandType.TurnToPosition) {
            targetPosition = targetPosition
        };
    }

    public static Command CreateFormationCommand(float rotation) {
        return new Command(CommandType.Formation) {
            targetRotation = rotation
        };
    }

    public static Command CreateFormationCommand(Vector2 targetPosition, float rotation) {
        return new Command(CommandType.FormationLocation) {
            targetPosition = targetPosition,
            targetRotation = rotation
        };
    }

    public static Command CreateMoveCommand(Vector2 targetPosition, float maxSpeed = float.MaxValue) {
        return new Command(CommandType.Move) {
            targetPosition = targetPosition,
            maxSpeed = maxSpeed
        };
    }

    public static Command CreateMoveOffsetCommand(Vector2 currentPosition, Vector2 targetPosition, float offset,
        float maxSpeed = float.MaxValue) {
        return new Command(CommandType.Move) {
            targetPosition =
                Vector2.MoveTowards(currentPosition, targetPosition, Vector2.Distance(currentPosition, targetPosition) - offset),
            maxSpeed = maxSpeed
        };
    }

    public static Command CreateAttackMoveCommand(Vector2 targetPosition, float maxSpeed = float.MaxValue) {
        return new Command(CommandType.AttackMove) {
            targetPosition = targetPosition,
            maxSpeed = maxSpeed,
            waitTime = Random.Range(0, 0.2f)
        };
    }

    public static Command CreateAttackMoveCommand(Unit targetUnit, float maxSpeed = float.MaxValue,
        bool useAlternateCommandOnceDone = false) {
        return new Command(CommandType.AttackMoveUnit) {
            targetUnit = targetUnit,
            maxSpeed = maxSpeed,
            useAlternateCommandOnceDone = useAlternateCommandOnceDone,
            waitTime = Random.Range(0, 0.2f)
        };
    }

    public static Command CreateAttackFleetCommand(Fleet targetFleet, Ship targetShip = null) {
        return new Command(CommandType.AttackFleet) {
            targetFleet = targetFleet,
            maxSpeed = float.MaxValue,
            targetUnit = targetShip
        };
    }

    public static Command CreateSkirmishCommand(Fleet targetFleet, Ship targetShip, float maxSpeed = float.MaxValue) {
        return new Command(CommandType.AttackFleet) {
            targetUnit = targetShip,
            targetFleet = targetFleet,
            maxSpeed = maxSpeed
        };
    }

    public static Command CreateFollowCommand(Unit targetUnit, float maxSpeed = float.MaxValue) {
        return new Command(CommandType.Follow) {
            targetUnit = targetUnit,
            maxSpeed = maxSpeed
        };
    }

    public static Command CreateProtectCommand(Unit protectUnit, float maxSpeed = float.MaxValue) {
        return new Command(CommandType.Protect) {
            protectUnit = protectUnit,
            maxSpeed = maxSpeed
        };
    }

    public static Command CreateDockCommand(Station destinationStation, float maxSpeed = float.MaxValue) {
        return new Command(CommandType.Dock) {
            destinationStation = destinationStation,
            maxSpeed = maxSpeed
        };
    }

    public static Command CreateUndockCommand() {
        return CreateUndockCommand(Random.Range(0, 360f));
    }

    public static Command CreateUndockCommand(float rotation) {
        return new Command(CommandType.UndockCommand) {
            targetRotation = rotation
        };
    }

    public static Command CreateTransportCommand(Station productionStation, Station destinationStation, CargoBay.CargoTypes cargoType,
        bool oneTrip = false) {
        return new Command(CommandType.Transport) {
            destinationStation = destinationStation,
            productionStation = productionStation,
            useAlternateCommandOnceDone = oneTrip,
            cargoType = cargoType
        };
    }

    public static Command CreateTransportDelayCommand(Station productionStation, Station destinationStation, CargoBay.CargoTypes cargoType,
        float delay) {
        return new Command(CommandType.TransportDelay) {
            destinationStation = destinationStation,
            productionStation = productionStation,
            waitTime = delay,
            cargoType = cargoType,
            targetRotation = delay
        };
    }

    public static Command CreateResearchCommand(Star targetStar, Station returnStation) {
        return new Command(CommandType.Research) {
            destinationStation = returnStation,
            targetStar = targetStar
        };
    }

    public static Command CreateCollectGasCommand(GasCloud targetGasCloud, Station returnStation) {
        return new Command(CommandType.CollectGas) {
            destinationStation = returnStation,
            targetGasCloud = targetGasCloud
        };
    }

    public static Command CreateDisbandFleetCommand() {
        return new Command(CommandType.DisbandFleet);
    }

    public static Command CreateColonizeCommand(Planet planet) {
        return new Command(CommandType.Colonize) {
            targetPlanet = planet
        };
    }

    public static Command CreateBuildStationCommand(Faction faction, Station.StationType stationType, Vector2 position) {
        return CreateBuildStationCommand(faction.battleManager.CreateNewStation(
            new BattleObject.BattleObjectData(stationType.ToString(), new BattleManager.PositionGiver(position), Random.Range(0, 360),
                faction),
            faction.battleManager.GetStationBlueprint(stationType).stationScriptableObject, false));
    }

    public static Command CreateBuildStationCommand(Station stationToBuild) {
        return new Command(CommandType.BuildStation) {
            destinationStation = stationToBuild
        };
    }

    /// <summary>
    /// Called when the command is no longer the first command in the list.
    /// </summary>
    public void OnCommandNoLongerActive(Ship ship) {
        if (commandType == CommandType.Transport || commandType == CommandType.TransportDelay) {
            // Ships that are doing a transport command go into a special group
            // We need to remove them from this group and add it to the base group once the command is no longer active
            ship.SetGroup(ship.faction.baseGroup);
        }
    }

    public void OnRemoveCommand(Ship ship, bool wasActiveCommand) {
        if (commandType == CommandType.Transport || commandType == CommandType.TransportDelay) {
            if (wasActiveCommand) ship.SetGroup(ship.faction.baseGroup);
        } else if (commandType == CommandType.BuildStation && !destinationStation.IsBuilt()) {
            // The unbuilt station needs to be destroyed once the command is destroyed since unbuilt stations are actual objects
            destinationStation.Explode();
        }
    }

    public bool IsAttackCommand() {
        return commandType == CommandType.AttackMove || commandType == CommandType.AttackMoveUnit || commandType == CommandType.AttackFleet;
    }

    public IObject GetTargetObject() {
        if (targetFleet != null) return targetFleet;
        if (targetUnit != null) return targetUnit;
        return null;
    }
}
