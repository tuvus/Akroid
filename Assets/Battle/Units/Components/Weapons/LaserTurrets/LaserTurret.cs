using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserTurret : Turret {
    [field: SerializeField] public float fireDuration { get; private set; }
    [field: SerializeField] public float fadeDuration { get; private set; }
    [field: SerializeField] public float laserDamagePerSecond { get; private set; }
    [field: SerializeField] public float laserRange { get; private set; }
    [field: SerializeField] public float laserSize { get; private set; }
    LaserTurretScriptableObject laserTurretScriptableObject;
    Laser laser;

    public override void SetupComponent(Module module, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, componentScriptableObject);
        laserTurretScriptableObject = (LaserTurretScriptableObject)componentScriptableObject;
    }

    public override void SetupTurret(Unit unit) {
        base.SetupTurret(unit);
        laser = Instantiate(laserTurretScriptableObject.laserPrefab, transform.position, transform.rotation, transform).GetComponent<Laser>();
        laser.SetLaser(this, GetTurretOffSet(), laserTurretScriptableObject.laserSize);
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

    public override bool Fire() {
        base.Fire();
        laser.FireLaser();
        return false;
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

    public float GetFireDuration() {
        return laserTurretScriptableObject.fireDuration;
    }

    public float GetFadeDuration() {
        return laserTurretScriptableObject.fadeDuration;
    }

    public float GetLaserRange() {
        return laserTurretScriptableObject.laserRange;
    }

    public float GetLaserDamagePerSecond() {
        return laserTurretScriptableObject.laserDamagePerSecond;
    }


    public override float GetDamagePerSecond() {
        reloadController = GetComponent<ReloadController>();
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }
        float damage = laserTurretScriptableObject.laserDamagePerSecond * (laserTurretScriptableObject.fireDuration + laserTurretScriptableObject.fadeDuration / 2) * reloadController.maxAmmo;
        return damage / time;
    }

    public override void StopFiring() {
        base.StopFiring();
        Destroy(laser.gameObject);
    }
    public override void ShowEffects(bool shown) {
        base.ShowEffects(shown);
        laser.ShowEffects(shown);
    }

    [ContextMenu("GetDamagePerSecond")]
    public void PrintDamagePerSecond() {
        print(GetDamagePerSecond());
    }
}