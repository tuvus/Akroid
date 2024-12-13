using UnityEngine;

public class LaserTurret : Turret {
    public LaserTurretScriptableObject laserTurretScriptableObject { get; private set; }
    public Laser laser { get; private set; }

    public LaserTurret(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        laserTurretScriptableObject = (LaserTurretScriptableObject)componentScriptableObject;

        laser = new Laser();
        laser.SetLaser(this);
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
        return base.GetRange() * faction.GetImprovementModifier(Faction.ImprovementAreas.LaserRange);
    }

    public override float GetReloadTimeModifier() {
        return faction.GetImprovementModifier(Faction.ImprovementAreas.LaserReload);
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
        reloadController = new ReloadController(laserTurretScriptableObject.fireSpeed, laserTurretScriptableObject.reloadSpeed,
            laserTurretScriptableObject.maxAmmo);
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }

        float damage = laserTurretScriptableObject.laserDamagePerSecond *
                       (laserTurretScriptableObject.fireDuration + laserTurretScriptableObject.fadeDuration / 2) * reloadController.maxAmmo;
        return damage / time;
    }

    [ContextMenu("GetDamagePerSecond")]
    public void PrintDamagePerSecond() {
        Debug.Log(GetDamagePerSecond());
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/LaserTurret");
    }
}
