using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MoveShipsToObjectUICondition : UIWrapperEventCondition<MoveShipsToObject> {
    public MoveShipsToObjectUICondition(MoveShipsToObject conditionLogic, LocalPlayer localPlayer, UIBattleManager uiBattleManager,
        bool visualize = false) : base(conditionLogic, localPlayer, uiBattleManager, visualize) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        Predicate<Ship> shipHasMoveToObjectCommand = ship => ship.shipAI.commands.Any((command) =>
            command.commandType == Command.CommandType.Move && Vector2.Distance(command.targetPosition,
                conditionLogic.objectToMoveTo.GetPosition()) <=
            conditionLogic.objectToMoveTo.GetSize() + ship.GetSize() + conditionLogic.distance);

        AddShipsToSelect(conditionLogic.shipsToMove.Select(s => (ShipUI)uiBattleManager.units[s]).Where(
            shipUI => Vector2.Distance(shipUI.ship.GetPosition(), conditionLogic.objectToMoveTo.GetPosition()) >
                conditionLogic.objectToMoveTo.GetSize() + shipUI.ship.GetSize() + conditionLogic.distance
                && !shipHasMoveToObjectCommand(shipUI.ship)).ToList(), objectsToVisualize, buttonsToVisualize);

        if (conditionLogic.shipsToMove.Any(s => Vector2.Distance(s.position, conditionLogic.objectToMoveTo.GetPosition()) >
            conditionLogic.objectToMoveTo.GetSize() + s.GetSize() + conditionLogic.distance || !shipHasMoveToObjectCommand(s))) {
            objectsToVisualize.Add(uiBattleManager.objects[conditionLogic.objectToMoveTo]);
        }
    }
}
