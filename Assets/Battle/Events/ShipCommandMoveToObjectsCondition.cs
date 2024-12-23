using System.Collections.Generic;
using UnityEngine;

public class ShipCommandMoveToObjectsCondition : EventCondition {
    public List<Ship> shipsToMove { get; private set; }
    public List<IObject> objectsToMoveTo { get; private set; }

    public ShipCommandMoveToObjectsCondition(List<Ship> shipsToMove, List<IObject> objectsToMoveTo, bool visualize) :
        base(ConditionType.MoveShipsToObject, visualize) {
        this.shipsToMove = shipsToMove;
        this.objectsToMoveTo = objectsToMoveTo;
    }

    public ShipCommandMoveToObjectsCondition(List<Ship> shipsToMove, IObject iObject, bool visualize) :
        this(shipsToMove, new List<IObject> { iObject }, visualize) { }

    public ShipCommandMoveToObjectsCondition(Ship shipToMove, List<IObject> objectsToMoveTo, bool visualize) :
        this(new List<Ship>() { shipToMove }, objectsToMoveTo, visualize) { }

    public ShipCommandMoveToObjectsCondition(Ship shipToMove, IObject iObject, bool visualize) :
        this(new List<Ship>() { shipToMove }, new List<IObject> { iObject }, visualize) { }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        foreach (var ship in shipsToMove) {
            ShipAI shipAI = ship.shipAI;
            if (shipAI.commands.Count < objectsToMoveTo.Count) return false;
            int objectIndex = 0;
            foreach (var command in shipAI.commands) {
                if (command.commandType != Command.CommandType.Move || !(Vector2.Distance(command.targetPosition,
                        objectsToMoveTo[objectIndex].GetPosition()) <= ship.GetSize() + objectsToMoveTo[objectIndex].GetSize())) continue;

                objectIndex++;
                if (objectIndex == objectsToMoveTo.Count) break;
            }

            if (objectIndex != objectsToMoveTo.Count) return false;
        }

        return true;
    }
}
