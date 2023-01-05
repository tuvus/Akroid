using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldGenerator : MonoBehaviour {
	private Unit unit;
	//ShieldGenStats
	public float shieldRegenRate;
	public float shieldRecreateSpeed;
	public int shieldRegenHealth;
	//ShieldStats
	public Shield shieldPrefab;
	public Vector2 shieldSize;
	public int maxShieldHealth;

	//RuntimeStats
	private float timeTillShieldCount;
	private Shield shield;


	public void SetupShieldGenerator(Unit unit) {
		this.unit = unit;
		shield = Instantiate(shieldPrefab, transform);
		shield.transform.localScale = new Vector2(shieldSize.x, shieldSize.y);
		shield.SetShield(maxShieldHealth, this, unit);
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
		shield.RegenShield(shieldRegenHealth);
		timeTillShieldCount += shieldRegenRate;
	}

	public void CreateShield(bool fullHealth) {
		ShowShield(true);
		if (fullHealth) {
			shield.SetShield(maxShieldHealth, this, unit);
		} else {
			shield.SetShield(maxShieldHealth / 5, this, unit);
		}
	}

	public void DestroyShield() {
		ShowShield(false);
		timeTillShieldCount = shieldRecreateSpeed;
	}

	public int GetShieldStrength() {
		int shieldHealth = 0;
		if (shield != null)
			shieldHealth = shield.health;
		return shieldHealth;
	}

	public int GetMaxShieldStrenght() {
		return Mathf.RoundToInt(maxShieldHealth * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ShieldHealth));
	}

	public Shield GetShield() {
		return shield;
    }

	public void ShowShield(bool show) {
		shield.ShowSield(show);
    }
}
