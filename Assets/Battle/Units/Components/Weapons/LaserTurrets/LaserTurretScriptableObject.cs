using System;
using UnityEngine;
using UnityEngine.Audio;
using static Turret;

[CreateAssetMenu(fileName = "Resources/Components/LaserTurretScriptableObject", menuName = "Components/LaserTurret", order = 1)]
public class LaserTurretScriptableObject : TurretScriptableObject {
    public float laserDamagePerSecond;
    public float fireDuration;
    public float fadeDuration;
    public float laserRange;
    public float laserSize;
    public GameObject turretPrefab;
    public GameObject laserPrefab;
    public AudioResource laserSound;

    public override float GetDamagePerSecond() {
        float time = reloadSpeed;
        if (maxAmmo > 1) {
            time += maxAmmo * fireSpeed;
        }

        float damage = laserDamagePerSecond * (fireDuration + fadeDuration / 2) * maxAmmo;
        return damage / time;
    }

    public override void Awake() {
        base.Awake();
        targeting = TargetingBehaviors.closest;
        if (turretPrefab == null) turretPrefab = Resources.Load<GameObject>("Prefabs/LaserTurret");
        if (laserPrefab == null) laserPrefab = Resources.Load<GameObject>("Prefabs/Laser");
        if (laserSound == null) laserSound = Resources.Load<AudioResource>("Prefabs/Audio/Laser");
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
