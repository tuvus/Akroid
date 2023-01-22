using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassTurret : Turret {
    private SpriteRenderer flash;
    static float flashSpeed = 0.5f;
    private float flashTime;
    //The Projectiles's start variables
    [Tooltip("Max at around 150")]
    public float fireVelocity;
    public float fireAccuracy;

    //The Projectile's Stats
    public int minDamage;
    public int maxDamage;
    public float projectileRange;

    public GameObject projectilePrefab;

    public override void SetupTurret(Unit unit) {
        base.SetupTurret(unit);
        flash = Instantiate(Resources.Load<GameObject>("Prefabs/Highlight"), transform).GetComponent<SpriteRenderer>();
        flash.transform.localPosition = new Vector2(0, turretOffset);
        flash.enabled = false;
    }

    public override bool Fire() {
        base.Fire();
        Projectile projectile = BattleManager.Instance.GetNewProjectile();
        projectile.SetProjectile(unit.faction, transform.position, transform.eulerAngles.z + Random.Range(-fireAccuracy, fireAccuracy), unit.GetVelocity(), fireVelocity, Mathf.RoundToInt(Random.Range(minDamage, maxDamage) * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileDamage)), projectileRange * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileRange), GetTurretOffSet() * transform.localScale.y, transform.localScale.y * GetUnitScale());
        flashTime = flashSpeed;
        flash.enabled = BattleManager.Instance.GetEffectsShown();
        flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, 1);
        return true;
    }

    public override void UpdateTurret(float deltaTime) {
        base.UpdateTurret(deltaTime);
        if (flash.enabled) {
            flashTime -= deltaTime;
            if (flashTime <= 0) {
                flashTime = 0;
                flash.enabled = false;
            } else {
                flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, flashTime / flashSpeed);
            }
        }
    }
    protected override bool TurretHibernationStatus() {
        return base.TurretHibernationStatus() && !flash.enabled;
    }

    public override Vector2 GetTargetPosition(Unit target) {
        return Calculator.GetTargetPositionAfterTimeAndVelocity(unit.GetPosition(),target.GetPosition(),unit.GetVelocity(),target.GetVelocity(),fireVelocity, GetTurretOffSet());
    }

    public override float GetRange() {
        return base.GetRange() * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileRange);
    }

    public override float GetReloadTimeModifier() {
        return unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileReload);
    }

    public override float GetDamagePerSecond() {
        reloadController = GetComponent<ReloadController>();
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }
        float damage = (minDamage + maxDamage) / 2f * reloadController.maxAmmo;
        return damage / time;
    }

    public override void StopFireing() {
        base.StopFireing();
        flash.enabled = false;
    }

    public override void ShowEffects(bool shown) {
        base.ShowEffects(shown);
        if (flash.enabled) {
            flash.enabled = shown;
        }
    } 

    [ContextMenu("GetDamagePerSecond")]
    public void PrintDamagePerSecond() {
        print(GetDamagePerSecond());
    }
}