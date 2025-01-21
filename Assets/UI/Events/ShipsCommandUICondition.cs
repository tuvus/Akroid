using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class ShipsCommandUICondition : UIWrapperEventCondition<ShipsCommandCondition> {
    public ShipsCommandUICondition(ShipsCommandCondition conditionLogic, LocalPlayer localPlayer,
        UIBattleManager uiBattleManager, bool visualize = false) : base(conditionLogic, localPlayer, uiBattleManager, visualize) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        AddShipsToSelect(
            conditionLogic.shipsToCommand.Where(ship => !conditionLogic.DoesShipHaveCommand(ship))
                .Select(s => (ShipUI)uiBattleManager.units[s]).ToList(), objectsToVisualize, buttonsToVisualize);

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
                objectsToVisualize.Add(uiBattleManager.units[command.targetUnit]);
                break;
            case Command.CommandType.AttackFleet:
                objectsToVisualize.Add(uiBattleManager.fleetUIs[command.targetFleet]);
                break;
            case Command.CommandType.Follow:
                objectsToVisualize.Add(uiBattleManager.units[command.targetUnit]);
                break;
            case Command.CommandType.Protect:
                objectsToVisualize.Add(uiBattleManager.units[command.protectUnit]);
                break;
            case Command.CommandType.Formation:
            case Command.CommandType.FormationLocation:
                break;
            case Command.CommandType.Dock:
                objectsToVisualize.Add(uiBattleManager.units[command.destinationStation]);
                break;
            case Command.CommandType.UndockCommand:
                break;
            case Command.CommandType.Transport:
            case Command.CommandType.TransportDelay:
                objectsToVisualize.Add(uiBattleManager.units[command.productionStation]);
                break;
            case Command.CommandType.Research:
                if (command.targetStar != null) objectsToVisualize.Add(uiBattleManager.objects[command.targetStar]);
                else objectsToVisualize.AddRange(uiBattleManager.objects.Values.Where(o => o is StarUI));
                break;
            case Command.CommandType.CollectGas:
                if (command.targetGasCloud != null) objectsToVisualize.Add(uiBattleManager.objects[command.targetGasCloud]);
                else objectsToVisualize.AddRange(uiBattleManager.objects.Values.Where(o => o is GasCloudUI));
                break;
            case Command.CommandType.Colonize:
                objectsToVisualize.Add(uiBattleManager.objects[command.targetPlanet]);
                break;
            case Command.CommandType.BuildStation:
                break;
            default:
                throw new NotSupportedException("The visualization of the command condition " + command.commandType +
                    " is not supported yet");
        }
    }
}
