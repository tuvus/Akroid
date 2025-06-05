using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MoveShipsToObject : EventCondition {
    public MoveShipsToObject(Ship ship, IObject objectToMoveTo, float distance = 0f, bool visualize = false) :
        base(ConditionType.MoveShipsToObject, visualize) {
        shipsToMove = new List<Ship> { ship };
        this.objectToMoveTo = objectToMoveTo;
        this.distance = distance;
    }

    public MoveShipsToObject(List<Ship> shipsToMove, IObject objectToMoveTo, float distance = 0f,
        bool visualize = false) :
        base(ConditionType.MoveShipsToObject, visualize) {
        this.shipsToMove = shipsToMove;
        this.objectToMoveTo = objectToMoveTo;
        this.distance = distance;
    }
    public List<Ship> shipsToMove { get; }
    public IObject objectToMoveTo { get; }
    public float distance { get; }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return shipsToMove.All(s =>
            Vector2.Distance(s.GetPosition(), objectToMoveTo.GetPosition()) <
            s.GetSize() + objectToMoveTo.GetSize() + distance);
    }
}
