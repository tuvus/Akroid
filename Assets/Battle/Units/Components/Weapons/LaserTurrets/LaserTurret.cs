using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserTurret : Turret {
    public float chargeDuration;
    public float fireDuration;
    public float fadeDuration;
    public float damagePerSeccond;
    public float laserRange;
    public float laserSize;

    float chargeTime;

    Laser laser;
    public GameObject laserPrefab;

    public override void SetupTurret(Unit unit) {
        base.SetupTurret(unit);
        laser = Instantiate(laserPrefab, transform.position, transform.rotation, transform).GetComponent<Laser>();
        laser.SetLaser(this, GetTurretOffSet(), laserRange, laserSize);
        turretOffset *= transform.localScale.y;
    }

    public override void UpdateTurret() {
        base.UpdateTurret();
        if (hibernation && ReadyToFire())
            return;
        if (!laser.IsFireing() && chargeTime > 0)
            chargeTime = Mathf.Max(0, chargeTime - Time.fixedDeltaTime * BattleManager.Instance.timeScale * unit.faction.LaserChargeModifier);
        laser.UpdateLaser();
    }

    public override void Shoot() {
        if (ReadyToFire()) {
            laser.FireLaser();
            chargeTime = chargeDuration;
        }
    }

    public override bool ReadyToFire() {
        return chargeTime <= 0 && !laser.IsFireing();
    }

    public void ExpireLaser() {
        chargeTime = chargeDuration;
    }

    public override float GetRange() {
        return base.GetRange() * unit.faction.LaserRangeModifier;
    }
}
