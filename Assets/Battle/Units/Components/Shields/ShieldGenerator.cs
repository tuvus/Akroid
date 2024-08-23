using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldGenerator : ModuleComponent {
	ShieldGeneratorScriptableObject shieldGeneratorScriptableObject;
	private Unit unit;
    //ShieldStats
    public Shield shieldPrefab;

    //RuntimeStats
    private float timeTillShieldCount;
	private Shield shield;

    public override void SetupComponent(Module module, Faction faction, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, faction, componentScriptableObject);
		shieldGeneratorScriptableObject = (ShieldGeneratorScriptableObject)componentScriptableObject;
	}

	public void SetupShieldGenerator(Unit unit) {
		this.unit = unit;
		shield = Instantiate(shieldGeneratorScriptableObject.shieldPrefab, transform);
		shield.transform.localScale = new Vector2(unit.GetSpriteRenderer().sprite.bounds.size.x * 1.6f, unit.GetSpriteRenderer().sprite.bounds.size.x * 4f);
		shield.SetShield(shieldGeneratorScriptableObject.maxShieldHealth, this, unit);
		CreateShield(true);
	}

	public void UpdateShieldGenerator(float deltaTime) {
		timeTillShieldCount -= deltaTime * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ShieldRegen);
		if (shield.health == 0) {
			if (timeTillShieldCount <= 0) {
				CreateShield(false);
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

	public void CreateShield(bool fullHealth) {
		ShowShield(true);
		if (fullHealth) {
			shield.SetShield(shieldGeneratorScriptableObject.maxShieldHealth, this, unit);
		} else {
			shield.SetShield(shieldGeneratorScriptableObject.maxShieldHealth / 5, this, unit);
		}
	}

	public void DestroyShield() {
		ShowShield(false);
		timeTillShieldCount = shieldGeneratorScriptableObject.shieldRecreateSpeed;
	}

	public int GetShieldStrength() {
		int shieldHealth = 0;
		if (shield != null)
			shieldHealth = shield.health;
		return shieldHealth;
	}

	public int GetMaxShieldStrength() {
		return Mathf.RoundToInt(shieldGeneratorScriptableObject.maxShieldHealth * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ShieldHealth));
	}

	public Shield GetShield() {
		return shield;
    }

	public void ShowShield(bool show) {
		shield.ShowSield(show);
    }
}
