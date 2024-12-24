using System;
using System.Collections.Generic;
using System.Linq;

public class ShipsCommandUICondition : UIWrapperEventCondition<ShipsCommandCondition> {
    public ShipsCommandUICondition(ShipsCommandCondition conditionLogic, LocalPlayer localPlayer,
        UnitSpriteManager unitSpriteManager, bool visualize = false) : base(conditionLogic, localPlayer, unitSpriteManager, visualize) { }

    public override List<ObjectUI> GetVisualizedObjects() {
        List<ObjectUI> objectsToVisualize = new List<ObjectUI>();
        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();

        foreach (var ship in conditionLogic.shipsToCommand
            .Where(ship => !selectedUnits.Contains(unitSpriteManager.units[ship]) && !conditionLogic.DoesShipHaveCommand(ship))) {
            if (ship.dockedStation != null) {
                StationUI stationUI = (StationUI)unitSpriteManager.units[ship.dockedStation];
                if (!selectedUnits.Contains(stationUI)) selectedUnits.Add(stationUI);
                continue;
            }
            objectsToVisualize.Add(unitSpriteManager.units[ship]);
        }

        Command command = conditionLogic.commandCondition;
        switch (command.commandType) {
            case Command.CommandType.Idle:
            case Command.CommandType.Wait:
            case Command.CommandType.TurnToRotation:
            case Command.CommandType.TurnToPosition:
            case Command.CommandType.Move:
            case Command.CommandType.AttackMove:
                break;
            case Command.CommandType.AttackMoveUnit:
                objectsToVisualize.Add(unitSpriteManager.units[command.targetUnit]);
                break;
            case Command.CommandType.AttackFleet:
                objectsToVisualize.Add(unitSpriteManager.fleetUIs[command.targetFleet]);
                break;
            case Command.CommandType.Follow:
                objectsToVisualize.Add(unitSpriteManager.units[command.targetUnit]);
                break;
            case Command.CommandType.Protect:
                objectsToVisualize.Add(unitSpriteManager.units[command.protectUnit]);
                break;
            case Command.CommandType.Formation:
            case Command.CommandType.FormationLocation:
                break;
            case Command.CommandType.Dock:
                objectsToVisualize.Add(unitSpriteManager.units[command.destinationStation]);
                break;
            case Command.CommandType.UndockCommand:
                break;
            case Command.CommandType.Transport:
            case Command.CommandType.TransportDelay:
                objectsToVisualize.Add(unitSpriteManager.units[command.productionStation]);
                break;
            case Command.CommandType.Research:
                objectsToVisualize.Add(unitSpriteManager.objects[command.targetStar]);
                break;
            case Command.CommandType.CollectGas:
                objectsToVisualize.Add(unitSpriteManager.objects[command.targetGasCloud]);
                break;
            case Command.CommandType.Colonize:
                objectsToVisualize.Add(unitSpriteManager.objects[command.targetPlanet]);
                break;
            case Command.CommandType.BuildStation:
                break;
            default:
                throw new NotSupportedException("The visualization of the command condition " + command.commandType + " is not supported yet");
        }

        return objectsToVisualize;
    }
}
