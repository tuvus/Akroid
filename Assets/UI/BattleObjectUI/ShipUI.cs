public class ShipUI : UnitUI {
    public Ship ship { get; private set; }

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
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

    public override void SelectObject(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) {
        unitSelection.SetSelected(selectionStrength);
    }

    public override bool IsSelectable() {
        return base.IsSelectable() && ship.dockedStation == null;
    }
}
