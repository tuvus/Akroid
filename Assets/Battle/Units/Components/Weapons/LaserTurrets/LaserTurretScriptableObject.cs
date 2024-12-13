using System;
using UnityEngine;
using static Turret;

[CreateAssetMenu(fileName = "Resources/Components/LaserTurretScriptableObject", menuName = "Components/LaserTurret", order = 1)]
public class LaserTurretScriptableObject : TurretScriptableObject {
    public float laserDamagePerSecond;
    public float fireDuration;
    public float fadeDuration;
    public float laserRange;
    public float laserSize;
    public GameObject laserPrefab;

    public override float GetDamagePerSecond() {
        float time = reloadSpeed;
        if (maxAmmo > 1) {
            time += maxAmmo * fireSpeed;
        }

        float damage = laserDamagePerSecond * (fireDuration + fadeDuration / 2) * maxAmmo;
        return damage / time;
    }

    public void Awake() {
        targeting = TargetingBehaviors.closest;
        if (laserPrefab == null)
            laserPrefab = Resources.Load<GameObject>("Prefabs/Laser");
    }

    public override Type GetComponentType() {
        return typeof(LaserTurret);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(GetDamagePerSecond() * 26);
        AddResourceCost(CargoBay.CargoTypes.Metal, (long)(GetDamagePerSecond() * 8));
    }
}
