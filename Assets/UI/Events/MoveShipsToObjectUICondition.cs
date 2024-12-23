using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveShipsToObjectUICondition : UIWrapperEventCondition<MoveShipsToObject> {
    public MoveShipsToObjectUICondition(MoveShipsToObject conditionLogic, LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager,
        ConditionType conditionType, bool visualize = false) : base(conditionLogic, localPlayer, unitSpriteManager, conditionType,
        visualize) { }

    public override List<ObjectUI> GetVisualizedObjects() {
        //         Unit unitToShow2 = visualizedEvent.unitToSelect;
        //         HashSet<Unit> selectedUnits3 = GetSelectedUnits().GetAllUnits().ToHashSet();
        //         if (selectedUnits3.Contains(unitToShow2) && selectedUnits3.Count == 1) {
        //             VisualizeObjects(new List<IObject> { visualizedEvent.iObject });
        //         } else {
        //             if (((Ship)visualizedEvent.unitToSelect).dockedStation != null) {
        //                 unitToShow2 = ((Ship)visualizedEvent.unitToSelect).dockedStation;
        //             }
        //
        //             VisualizeObjects(new List<IObject> { unitToShow2, visualizedEvent.iObject });
        //         }
        // if (command.commandType == Command.CommandType.Move
        //                     && Vector2.Distance(command.targetPosition, visualizedEvent.iObjects[objectIndex].GetPosition()) <=
        //                     visualizedEvent.unitToSelect.GetSize() + visualizedEvent.iObjects[objectIndex].GetSize()) {
        //                     objectIndex++;
        //                     if (objectIndex == visualizedEvent.iObjects.Count) break;
        //                 }

        // Predicate<Ship> shipHasMoveToObjectCommand = ship => ship.shipAI.commands.Any((command) =>
        //     command.commandType == Command.CommandType.Move && Vector2.Distance(command.targetPosition,
        //         conditionLogic.objectToMoveTo.GetPosition()) <=
        //     conditionLogic.objectToMoveTo.GetSize() + ship.GetSize() + conditionLogic.distance);
        // List<ObjectUI> objectsToVisualize = new List<ObjectUI>();
        // if (conditionLogic.shipsToMove.Any(s => Vector2.Distance(s.position,
        //         conditionLogic.objectToMoveTo.GetPosition()) >
        //     conditionLogic.objectToMoveTo.GetSize() + s.GetSize() + conditionLogic.distance ||
        //     !shipHasMoveToObjectCommand(s))) {
        //     objectsToVisualize.Add(unitSpriteManager.battleObjects[conditionLogic.objectToMoveTo]);
        // }
        // return co
        return new List<ObjectUI>();
    }
}
