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

    public float fireTimeBettwenShots;
    public float magazineReloadTime;
    public int magazineAmmo;
    float firetime;
    int ammo;
    public override void SetupTurret(Unit unit) {
        base.SetupTurret(unit);
        ammo = magazineAmmo;
    }

    public override void UpdateTurret() {
        base.UpdateTurret();
        if (hibernation && ReadyToFire())
            return;
        if (firetime <= 0f) {
            if (ammo == 0)
                ammo = magazineAmmo;
            firetime = 0f;
        } else if (firetime != 0f) {
            firetime -= Time.fixedDeltaTime * BattleManager.Instance.timeScale * unit.faction.ProjectileReloadModifier;
        }
    }

    public override void Shoot() {
        if (firetime <= 0 && ammo > 0) {
            Projectile projectile = BattleManager.Instance.GetNewProjectile();
            projectile.SetProjectile(unit.faction, transform.position, transform.eulerAngles.z, unit.GetVelocity(), fireVelocity, Mathf.RoundToInt(Random.Range(minDamage, maxDamage) * unit.faction.ProjectileDamageModifier), projectileRange * unit.faction.ProjectileRangeModifier, GetTurretOffSet() * transform.localScale.y, transform.localScale.y * GetUnitScale());
            projectile.transform.Rotate(new Vector3(0, 0, Random.Range(-fireAccuracy, fireAccuracy)));
            ammo--;
            if (ammo == 0) {
                firetime = magazineReloadTime;
            } else {
                firetime += fireTimeBettwenShots;
            }
        }
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

    public override bool ReadyToFire() {
        return firetime <= 0 && ammo != 0;
    }

    public override float GetRange() {
        return base.GetRange() * unit.faction.ProjectileRangeModifier;
    }
}
