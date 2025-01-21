using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShipCommandMoveToObjectsUICondition : UIWrapperEventCondition<ShipCommandMoveToObjectsCondition> {
    public ShipCommandMoveToObjectsUICondition(ShipCommandMoveToObjectsCondition conditionLogic, LocalPlayer localPlayer,
        UIBattleManager uiBattleManager, bool visualize = false) : base(conditionLogic, localPlayer, uiBattleManager, visualize) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();
        List<ShipUI> shipsToSelect = new List<ShipUI>();

        foreach (var ship in conditionLogic.shipsToMove) {
            ShipUI shipUI = (ShipUI)uiBattleManager.units[ship];
            ShipAI shipAI = ship.shipAI;
            int objectIndex = 0;
            foreach (var command in shipAI.commands) {
                if (command.commandType == Command.CommandType.Move
                    && Vector2.Distance(command.targetPosition, conditionLogic.objectsToMoveTo[objectIndex].GetPosition()) <=
                    ship.GetSize() + conditionLogic.objectsToMoveTo[objectIndex].GetSize()) {
                    objectIndex++;
                    if (objectIndex == conditionLogic.objectsToMoveTo.Count) break;
                }
            }

            // If the ship already has the movement commands made then we don't have any visualization to do for this ship
            if (objectIndex >= conditionLogic.objectsToMoveTo.Count) continue;

            if (selectedUnits.Contains(shipUI)) {
                ObjectUI objectUI = uiBattleManager.objects[conditionLogic.objectsToMoveTo[objectIndex]];
                if (!objectsToVisualize.Contains(objectUI)) objectsToVisualize.Add(objectUI);
            } else {
                shipsToSelect.Add(shipUI);
            }
        }
        AddShipsToSelect(shipsToSelect, objectsToVisualize, buttonsToVisualize);
    }
}
