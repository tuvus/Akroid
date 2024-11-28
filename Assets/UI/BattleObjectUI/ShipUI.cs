public class ShipUI : UnitUI {
    public Ship ship { get; private set; }

    public override void Setup(BattleObject battleObject) {
        base.Setup(battleObject);
        this.ship = (Ship)battleObject;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (ship.visible) {
            if (!spriteRenderer.enabled) spriteRenderer.enabled = true;
        } else {
            if (spriteRenderer.enabled) spriteRenderer.enabled = false;
        }
    }

    public override bool IsSelectable() {
        return base.IsSelectable() && ship.dockedStation == null;
    }
}
