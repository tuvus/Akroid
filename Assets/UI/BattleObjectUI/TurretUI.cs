using UnityEngine;

public class TurretUI : BattleObjectUI {
    private Turret turret;
    private UnitUI unitUI;

    public void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager);
        this.unitUI = unitUI;
        turret = (Turret)battleObject;
        spriteRenderer.sprite = turret.turretScriptableObject.turretSprite;
        spriteRenderer.enabled = true;
    }

    public override Vector2 GetPosition() {
        return turret.GetPosition();
    }

    public override float GetRotation() {
        return turret.rotation;
    }

    public override bool IsVisible() {
        return unitUI.IsVisible();
    }

    public override void SelectObject(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) { }

    public override void UnselectObject() { }
}
