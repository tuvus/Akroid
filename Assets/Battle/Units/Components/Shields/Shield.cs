using UnityEngine;

public class Shield : BattleObject {
    private Unit unit;
    private ShieldGenerator shieldGenerator;
    public int health { get; private set; }

    public Shield(ShieldGenerator shieldGenerator, Unit unit, int health) {
        this.health = health;
        this.shieldGenerator = shieldGenerator;
        this.unit = unit;
        spawned = true;
    }

    public void SetStrength(int strength) {
        health = strength;
        if (strength > 0) spawned = true;
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
            spawned = false;
            return returnValue;
        } else {
            return 0;
        }
    }

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
