using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassTurret : Turret {
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
    }

    public override void Fire() {
        base.Fire();
        Projectile projectile = BattleManager.Instance.GetNewProjectile();
        projectile.SetProjectile(unit.faction, transform.position, transform.eulerAngles.z + Random.Range(-fireAccuracy, fireAccuracy), unit.GetVelocity(), fireVelocity, Mathf.RoundToInt(Random.Range(minDamage, maxDamage) * unit.faction.ProjectileDamageModifier), projectileRange * unit.faction.ProjectileRangeModifier, GetTurretOffSet() * transform.localScale.y, transform.localScale.y * GetUnitScale());
    }

    public override Vector2 GetTargetPosition(Unit target) {
        return Calculator.GetTargetPositionAfterTimeAndVelocity(unit.GetPosition(),target.GetPosition(),unit.GetVelocity(),target.GetVelocity(),fireVelocity, GetTurretOffSet());
    }

    public override float GetRange() {
        return base.GetRange() * unit.faction.ProjectileRangeModifier;
    }

    public override float GetReloadTimeModifier() {
        return unit.faction.ProjectileReloadModifier;
    }

    public float GetDamagePerSecond() {
        reloadController = GetComponent<ReloadController>();
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }
        float damage = (minDamage + maxDamage) / 2f * reloadController.maxAmmo;
        return damage / time;
    }

    [ContextMenu("GetDamagePerSecond")]
    public void PrintDamagePerSecond() {
        print(GetDamagePerSecond());
    }
}
