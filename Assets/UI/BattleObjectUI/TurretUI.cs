using UnityEngine;

public class TurretUI : ComponentUI {
    private Turret turret;

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager, unitUI);
        turret = (Turret)battleObject;
        spriteRenderer.sprite = turret.turretScriptableObject.turretSprite;
        spriteRenderer.enabled = true;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (uIManager.GetFactionColoringShown()) spriteRenderer.color = unitUI.unit.faction.GetColorTint();
        else spriteRenderer.color = Color.white;
    }

    public override void OnUnitDestroyed() {
        spriteRenderer.enabled = false;
    }

    public override bool IsVisible() {
        return unitUI.IsVisible();
    }
}
