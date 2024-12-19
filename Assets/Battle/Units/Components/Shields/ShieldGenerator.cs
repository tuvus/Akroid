using UnityEngine;

public class ShieldGenerator : ModuleComponent {
    public ShieldGeneratorScriptableObject shieldGeneratorScriptableObject { get; private set; }

    private float timeTillShieldCount;
    public Shield shield { get; private set; }

    public ShieldGenerator(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        shieldGeneratorScriptableObject = (ShieldGeneratorScriptableObject)componentScriptableObject;
        shield = new Shield(this, unit, GetMaxShieldStrength());
    }

    public void UpdateShieldGenerator(float deltaTime) {
        timeTillShieldCount -= deltaTime * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ShieldRegen);
        if (shield.health == 0) {
            if (timeTillShieldCount <= 0) {
                shield.SetStrength(GetMaxShieldStrength() / 5);
                shield.ReactivateShield();
            }
        } else {
            if (timeTillShieldCount <= 0) {
                shield.RegenShield(shieldGeneratorScriptableObject.shieldRegenHealth);
                timeTillShieldCount += shieldGeneratorScriptableObject.shieldRegenRate;
            }
        }
    }

    public void DestroyShield() {
        shield.DestroyShield();
        spawned = false;
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
