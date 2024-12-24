using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveShipsToObjectUICondition : UIWrapperEventCondition<MoveShipsToObject> {
    public MoveShipsToObjectUICondition(MoveShipsToObject conditionLogic, LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager,
        bool visualize = false) : base(conditionLogic, localPlayer, unitSpriteManager, visualize) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize) {
        Predicate<Ship> shipHasMoveToObjectCommand = ship => ship.shipAI.commands.Any((command) =>
            command.commandType == Command.CommandType.Move && Vector2.Distance(command.targetPosition,
                conditionLogic.objectToMoveTo.GetPosition()) <=
            conditionLogic.objectToMoveTo.GetSize() + ship.GetSize() + conditionLogic.distance);

        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();
        conditionLogic.shipsToMove.Select(s => (ShipUI)unitSpriteManager.units[s]).ToList().ForEach(shipUI => {
            if (selectedUnits.Contains(shipUI)) return;
            if (Vector2.Distance(shipUI.ship.GetPosition(), conditionLogic.objectToMoveTo.GetPosition()) <=
                conditionLogic.objectToMoveTo.GetSize() + shipUI.ship.GetSize() + conditionLogic.distance) return;
            if (shipHasMoveToObjectCommand(shipUI.ship)) return;

            // If the unit is docked at a station, we need to show the station instead
            if (shipUI.ship.dockedStation != null) {
                StationUI dockedStationUI = (StationUI)unitSpriteManager.units[shipUI.ship.dockedStation];
                if (!objectsToVisualize.Contains(dockedStationUI)) objectsToVisualize.Add(dockedStationUI);
                return;
            }

            objectsToVisualize.Add(shipUI);
        });

        if (conditionLogic.shipsToMove.Any(s => Vector2.Distance(s.position,
                conditionLogic.objectToMoveTo.GetPosition()) >
            conditionLogic.objectToMoveTo.GetSize() + s.GetSize() + conditionLogic.distance || !shipHasMoveToObjectCommand(s))) {
            objectsToVisualize.Add(unitSpriteManager.objects[conditionLogic.objectToMoveTo]);
        }
    }
}
