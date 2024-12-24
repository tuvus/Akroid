using System;
using System.Collections.Generic;
using System.Linq;

public class ShipsCommandCondition : EventCondition {
    public List<Ship> shipsToCommand { get; private set; }
    public Command commandCondition { get; private set; }

    public ShipsCommandCondition(List<Ship> shipsToCommand, Command commandCondition, bool visualize = false) :
        base(ConditionType.CommandShip, visualize) {
        this.shipsToCommand = shipsToCommand;
        this.commandCondition = commandCondition;
    }

    public ShipsCommandCondition(Ship shipToCommand, Command commandCondition, bool visualize = false) :
        this(new List<Ship>() { shipToCommand }, commandCondition, visualize) { }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return shipsToCommand.All(DoesShipHaveCommand);
    }

    public bool DoesShipHaveCommand(Ship ship) {
        switch (commandCondition.commandType) {
            case Command.CommandType.Idle:
                throw new NotSupportedException("The command condition " + commandCondition.commandType + " is not supported yet");
            case Command.CommandType.Wait:
                return ship.shipAI.commands.Any(c => c.commandType == Command.CommandType.Wait);
            case Command.CommandType.TurnToRotation:
            case Command.CommandType.TurnToPosition:
            case Command.CommandType.Move:
            case Command.CommandType.AttackMove:
            case Command.CommandType.AttackMoveUnit:
            case Command.CommandType.AttackFleet:
            case Command.CommandType.Follow:
            case Command.CommandType.Protect:
            case Command.CommandType.Formation:
            case Command.CommandType.FormationLocation:
                throw new NotSupportedException("The command condition " + commandCondition.commandType + " is not supported yet");
            case Command.CommandType.Dock:
                return ship.shipAI.commands.Any(c =>
                    c.commandType == Command.CommandType.Dock && c.destinationStation == commandCondition.destinationStation);
            case Command.CommandType.UndockCommand:
            case Command.CommandType.Transport:
            case Command.CommandType.TransportDelay:
                throw new NotSupportedException("The command condition " + commandCondition.commandType + " is not supported yet");
            case Command.CommandType.Research:
                return ship.shipAI.commands.Any(c =>
                    c.commandType == Command.CommandType.Research &&
                    EqualsOrNull(commandCondition.destinationStation, c.destinationStation) &&
                    EqualsOrNull(commandCondition.targetStar, c.targetStar));
            case Command.CommandType.CollectGas:
                return ship.shipAI.commands.Any(c =>
                    c.commandType == Command.CommandType.CollectGas &&
                    EqualsOrNull(commandCondition.destinationStation, c.destinationStation) &&
                    EqualsOrNull(commandCondition.targetGasCloud, c.targetGasCloud));
            case Command.CommandType.DisbandFleet:
                throw new NotSupportedException("The command condition " + commandCondition.commandType + " is not supported yet");
            case Command.CommandType.Colonize:
                return ship.shipAI.commands.Any(c => c.commandType == Command.CommandType.CollectGas &&
                    EqualsOrNull(commandCondition.targetPlanet, c.targetPlanet));
            default:
                throw new NotSupportedException("The command condition " + commandCondition.commandType + " is not supported yet");
        }

        return false;
    }

    private bool EqualsOrNull(object expected, object actual) {
        if (expected == null) return true;
        return expected == actual;
    }
}
