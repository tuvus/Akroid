using UnityEngine;

public class Shield : BattleObject {
    private Unit unit;
    private ShieldGenerator shieldGenerator;
    public int health { get; private set; }

    public Shield(ShieldGenerator shieldGenerator, Unit unit, int health) {
        this.health = health;
        this.shieldGenerator = shieldGenerator;
        this.unit = unit;
    }

    public void SetStrength(int strength) {
        health = strength;
    }

    public void RegenShield(int regenAmount) {
        health += regenAmount;
        if (health > shieldGenerator.GetMaxShieldStrength())
            health = shieldGenerator.GetMaxShieldStrength();
    }

    public int TakeDamage(int takeDamage) {
        health -= takeDamage;
        if (health <= 0) {
            int returnValue = -health;
            shieldGenerator.DestroyShield();
            health = 0;
            SetVisible(false);
            return returnValue;
        } else {
            return 0;
        }
    }

    // public void RefreshSheild() {
    //     float shieldPercent = (float)health / shieldGenerator.GetMaxShieldStrength();
    //     spriteRenderer.color = new Color(0, .4f, 1, .4f * shieldPercent);
    // }

    public Unit GetUnit() {
        return unit;
    }

    public void SetVisible(bool visible) {
        this.visible = visible;
    }

    public override GameObject GetPrefab() {
        return shieldGenerator.shieldGeneratorScriptableObject.shieldPrefab;
    }
}
