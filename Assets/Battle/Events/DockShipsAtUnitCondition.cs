using System.Collections.Generic;
using System.Linq;

public class DockShipsAtUnitCondition : EventCondition {
    public List<Ship> shipsToDock { get; private set; }
    public Station unitToDockTo { get; private set; }

    public DockShipsAtUnitCondition(List<Ship> shipsToDock, Station unitToDockTo, bool visualize = false) :
        base(ConditionType.ShipsDockedAtUnit, visualize) {
        this.shipsToDock = shipsToDock;
        this.unitToDockTo = unitToDockTo;
    }

    public DockShipsAtUnitCondition(Ship shipToDock, Station unitToDockTo, bool visualize = false) :
        this(new List<Ship>() { shipToDock }, unitToDockTo, visualize) { }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        return shipsToDock.All(s => s.dockedStation == unitToDockTo);
    }
}
