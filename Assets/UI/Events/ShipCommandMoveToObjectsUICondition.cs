using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShipCommandMoveToObjectsUICondition : UIWrapperEventCondition<ShipCommandMoveToObjectsCondition> {
    public ShipCommandMoveToObjectsUICondition(ShipCommandMoveToObjectsCondition conditionLogic, LocalPlayer localPlayer,
        UnitSpriteManager unitSpriteManager, bool visualize = false) : base(conditionLogic, localPlayer, unitSpriteManager, visualize) { }

    public override List<ObjectUI> GetVisualizedObjects() {
        List<ObjectUI> objectsToVisualize = new List<ObjectUI>();
        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();
        foreach (var ship in conditionLogic.shipsToMove) {
            ShipUI shipUI = (ShipUI)unitSpriteManager.units[ship];
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
                ObjectUI objectUI = unitSpriteManager.objects[conditionLogic.objectsToMoveTo[objectIndex]];
                if (!objectsToVisualize.Contains(objectUI)) objectsToVisualize.Add(objectUI);
            } else if (ship.dockedStation != null) {
                StationUI dockedStationUI = (StationUI)unitSpriteManager.units[ship.dockedStation];
                if (!objectsToVisualize.Contains(dockedStationUI)) objectsToVisualize.Add(dockedStationUI);
            } else {
                objectsToVisualize.Add(shipUI);
            }
        }

        return objectsToVisualize;
    }
}
