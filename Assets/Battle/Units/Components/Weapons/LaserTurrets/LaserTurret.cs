using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserTurret : Turret {
    public float fireDuration;
    public float fadeDuration;
    public float laserDamagePerSecond;
    public float laserRange;
    public float laserSize;

    Laser laser;
    public GameObject laserPrefab;

    public override void SetupTurret(Unit unit) {
        base.SetupTurret(unit);
        laser = Instantiate(laserPrefab, transform.position, transform.rotation, transform).GetComponent<Laser>();
        laser.SetLaser(this, GetTurretOffSet(), laserSize);
        turretOffset *= transform.localScale.y;
    }

    protected override void UpdateTurretReload(float deltaTime) {
        if (!laser.IsFireing())
            base.UpdateTurretReload(deltaTime);
    }

    protected override bool TurretHibernationStatus() {
        return base.TurretHibernationStatus() && !laser.IsFireing();
    }

    protected override void UpdateTurretWeapon(float deltaTime) {
        base.UpdateTurretWeapon(deltaTime);
        laser.UpdateLaser(deltaTime);
    }

    public override void Fire() {
        base.Fire();
        laser.FireLaser();
    }

    public override bool ReadyToFire() {
        return base.ReadyToFire() && !laser.IsFireing();
    }

    public override float GetRange() {
        return base.GetRange() * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.LaserRange);
    }

    public override float GetReloadTimeModifier() {
        return unit.faction.GetImprovementModifier(Faction.ImprovementAreas.LaserReload);
    }

    public float GetDamagePerSecond() {
        reloadController = GetComponent<ReloadController>();
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }
        float damage = laserDamagePerSecond * (fireDuration + fadeDuration / 2) * reloadController.maxAmmo;
        return damage / time;
    }

    [ContextMenu("GetDamagePerSecond")]
    public void PrintDamagePerSecond() {
        print(GetDamagePerSecond());
    }
}
