using Unity.Mathematics;
using UnityEngine;

public class ShieldGenerator : ModuleComponent {
    private float timeTillShieldCount;

    public ShieldGenerator(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        shieldGeneratorScriptableObject = (ShieldGeneratorScriptableObject)componentScriptableObject;
        shield = new Shield(this, unit, GetMaxShieldStrength());
    }
    public ShieldGeneratorScriptableObject shieldGeneratorScriptableObject { get; }
    public Shield shield { get; }

    public void UpdateShieldGenerator(float deltaTime) {
        if (shield.spawned && shield.health <= 0) DestroyShield();
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

    /// <summary>
    /// Finds if a point is in the shield
    /// </summary>
    /// <param name="worldPosition">The point to check in world coordinates</param>
    /// <returns>True if the point is inside the shield, false otherwise</returns>
    public bool IsPointInShield(Vector2 worldPosition) {
        Vector2 localPosition = Calculator.ConvertWorldPositionToLocal(position, rotation, worldPosition);
        return math.pow(localPosition.x, 2) / math.pow(unit.unitScriptableObject.sprite.bounds.size.x * 1.6f, 2)
            + math.pow(localPosition.y, 2) / math.pow(unit.unitScriptableObject.sprite.bounds.size.x * 4f, 2) <= 0;
    }
}
