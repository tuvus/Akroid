using UnityEngine;

public class ShieldGenerator : ModuleComponent {
    public ShieldGeneratorScriptableObject shieldGeneratorScriptableObject { get; private set; }

    //RuntimeStats
    private float timeTillShieldCount;
    public Shield shield { get; private set; }

    public ShieldGenerator(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        shieldGeneratorScriptableObject = (ShieldGeneratorScriptableObject)componentScriptableObject;

        shield = new Shield(this, unit, shieldGeneratorScriptableObject.maxShieldHealth);
        // shield = Instantiate(shieldGeneratorScriptableObject.shieldPrefab, transform);
        // shield.transform.localScale = new Vector2(unit.GetSpriteRenderer().sprite.bounds.size.x * 1.6f, unit.GetSpriteRenderer().sprite.bounds.size.x * 4f);
    }

    public void UpdateShieldGenerator(float deltaTime) {
        timeTillShieldCount -= deltaTime * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ShieldRegen);
        if (shield.health == 0) {
            if (timeTillShieldCount <= 0) {
                shield.SetStrength(GetMaxShieldStrength() / 5);
            }
        } else {
            if (timeTillShieldCount <= 0) {
                RegenerateShields();
            }
        }
    }

    public void RegenerateShields() {
        shield.RegenShield(shieldGeneratorScriptableObject.shieldRegenHealth);
        timeTillShieldCount += shieldGeneratorScriptableObject.shieldRegenRate;
    }

    public void DestroyShield() {
        shield.SetVisible(false);
        timeTillShieldCount = shieldGeneratorScriptableObject.shieldRecreateSpeed;
    }

    public int GetShieldStrength() {
        return shield.health;
    }

    public int GetMaxShieldStrength() {
        return Mathf.RoundToInt(shieldGeneratorScriptableObject.maxShieldHealth *
            unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ShieldHealth));
    }
}
