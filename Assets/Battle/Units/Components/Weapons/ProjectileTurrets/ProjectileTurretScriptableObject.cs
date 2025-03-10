using System;
using UnityEngine;
using static Turret;

[CreateAssetMenu(fileName = "Resources/Components/ProjectileTurretScriptableObject", menuName = "Components/ProjectileTurret", order = 1)]
public class ProjectileTurretScriptableObject : TurretScriptableObject {
    [Tooltip("Max at around 150")] public float fireVelocity;
    public float fireAccuracy;
    public int minDamage;
    public int maxDamage;
    public float projectileRange;
    public GameObject turretPrefab;
    public GameObject projectilePrefab;
    public GameObject flashPrefab;
    public float flashSpeed = 0.5f;

    public override float GetDamagePerSecond() {
        float time = reloadSpeed;
        if (maxAmmo > 1) {
            time += maxAmmo * fireSpeed;
        }

        float damage = (minDamage + maxDamage) / 2f * maxAmmo;
        return damage / time;
    }

    public override void Awake() {
        base.Awake();
        targeting = TargetingBehaviors.closest;
        if (turretPrefab == null) turretPrefab = Resources.Load<GameObject>("Prefabs/ProjectileTurret");
        if (projectilePrefab == null) projectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
        if (flashPrefab == null) flashPrefab = Resources.Load<GameObject>("Prefabs/Highlight");
    }

    public override Type GetComponentType() {
        return typeof(ProjectileTurret);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(GetDamagePerSecond() * 15);
        AddResourceCost(CargoBay.CargoTypes.Metal, (long)(GetDamagePerSecond() * 8));
        AddResourceCost(CargoBay.CargoTypes.Gas, (long)(GetDamagePerSecond() * 7));
    }
}
