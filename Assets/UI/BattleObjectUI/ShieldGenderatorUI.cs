using UnityEngine;

public class ShieldGenderatorUI : ComponentUI {
    private ShieldGenerator shieldGenerator;
    private SpriteRenderer shieldRenderer;

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager, unitUI);
        shieldGenerator = (ShieldGenerator)battleObject;
        shieldRenderer = Instantiate(shieldGenerator.shield.GetPrefab(), transform).GetComponent<SpriteRenderer>();
        shieldRenderer.transform.localScale = new Vector2(unitUI.unit.unitScriptableObject.sprite.bounds.size.x * 1.6f, unitUI.unit.unitScriptableObject.sprite.bounds.size.x * 4f);
        shieldRenderer.enabled = false;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (IsVisible()) {
            float shieldPercent = (float)shieldGenerator.shield.health / shieldGenerator.GetMaxShieldStrength();
            shieldRenderer.color = new Color(0, .4f, 1, .4f * shieldPercent);
            shieldRenderer.enabled = true;
        } else {
            shieldRenderer.enabled = false;
        }
    }

    public override bool IsVisible() {
        return unitUI.IsVisible() && shieldGenerator.shield.visible;
    }

    public override void OnUnitDestroyed() { }
}
