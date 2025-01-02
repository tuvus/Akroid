using UnityEngine;

public class TurretUI : ComponentUI {
    private Turret turret;
    // private SpriteRenderer flash;
    static float flashSpeed = 0.5f;
    private float flashTime;

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager, unitUI);
        turret = (Turret)battleObject;
        spriteRenderer.sprite = turret.turretScriptableObject.turretSprite;
        spriteRenderer.enabled = true;

        // flash = Instantiate(Resources.Load<GameObject>("Prefabs/Highlight"), transform).GetComponent<SpriteRenderer>();
        // flash.transform.localScale = new Vector2(.2f,.2f);
        // flash.transform.localPosition = new Vector2(0, projectileTurretScriptableObject.turretOffset);
        // flash.enabled = false;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (uIManager.GetFactionColoringShown()) spriteRenderer.color = unitUI.unit.faction.GetColorTint();
        else spriteRenderer.color = Color.white;
    }

    public override void OnUnitDestroyed() {
        spriteRenderer.enabled = false;
    }
}
