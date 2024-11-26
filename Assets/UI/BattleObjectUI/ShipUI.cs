public class ShipUI : UnitUI {
    public Ship ship { get; private set; }

    public override void Setup(BattleObject battleObject) {
        base.Setup(battleObject);
        this.ship = (Ship)battleObject;
    }
}
