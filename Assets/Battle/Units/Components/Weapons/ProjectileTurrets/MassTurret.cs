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
        projectile.SetProjectile(unit.faction, transform.position, transform.eulerAngles.z, unit.GetVelocity(), fireVelocity, Mathf.RoundToInt(Random.Range(minDamage, maxDamage) * unit.faction.ProjectileDamageModifier), projectileRange * unit.faction.ProjectileRangeModifier, GetTurretOffSet() * transform.localScale.y, transform.localScale.y * GetUnitScale());
        projectile.transform.Rotate(new Vector3(0, 0, Random.Range(-fireAccuracy, fireAccuracy)));

    }

    public override Vector2 GetTargetPosition(Unit target) {
        Vector2 targetLocalPosition = (Vector2)transform.position - target.GetPosition();
        Vector2 localVelocity = unit.GetVelocity() - target.GetVelocity();
        if (localVelocity.magnitude < 1)
            return target.GetPosition();

        float calculatedTime = targetLocalPosition.magnitude / fireVelocity;
        Vector2 localTargetPos = targetLocalPosition;
        for (int i = 0; i < 6; i++) {
            localTargetPos = FindLocalPosAfterTime(targetLocalPosition, localVelocity, calculatedTime);
            float targetDist = localTargetPos.magnitude;
            float bulletDist = fireVelocity * calculatedTime;
            if (Mathf.Abs(targetDist - bulletDist) <= .01) {
                return (Vector2)transform.position - localTargetPos;
            }
            calculatedTime = localTargetPos.magnitude / fireVelocity;
        }
        return (Vector2)transform.position - localTargetPos;
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
