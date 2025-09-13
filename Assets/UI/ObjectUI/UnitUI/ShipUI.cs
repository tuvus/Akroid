public class ShipUI : UnitUI {
    public Ship ship { get; private set; }

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        ship = (Ship)battleObject;
    }

    public override bool IsSelectable() {
        return base.IsSelectable() && ship.dockedStation == null;
    }
}
