using System.Collections.Generic;
using System.Linq;

public class IdleShipsCondition : EventCondition {
    public List<Ship> shipsToIdle { get; private set; }

    public IdleShipsCondition(List<Ship> shipsToIdle, bool visualize = false) : base(ConditionType.WaitUntilShipsIdle, visualize) {
        this.shipsToIdle = shipsToIdle;
    }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        if (shipsToIdle.All((ship) => ship.IsIdle())) {
            return true;
        }

        return false;
    }
}
