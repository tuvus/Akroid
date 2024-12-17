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
        // if (flash.enabled) {
        //     flashTime -= deltaTime;
        //     if (flashTime <= 0) {
        //         flashTime = 0;
        //         flash.enabled = false;
        //     } else {
        //         flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, flashTime / flashSpeed);
        //     }
        // }
    }

    void Fire() {
        // flash.enabled = BattleManager.Instance.GetEffectsShown();
        // flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, 1);
        flashTime = flashSpeed;
    }

    // public override void StopFiring() {
    //     base.StopFiring();
    //     // flash.enabled = false;
    // }
    //
    // public override void ShowEffects(bool shown) {
    //     base.ShowEffects(shown);
    //     // if (flash.enabled) {
    //     // flash.enabled = shown;
    //     // }
    // }

    public override bool IsVisible() {
        return base.IsVisible() && unitUI.IsVisible();
    }

    public override void OnUnitDestroyed() {
        spriteRenderer.enabled = false;
    }
}
