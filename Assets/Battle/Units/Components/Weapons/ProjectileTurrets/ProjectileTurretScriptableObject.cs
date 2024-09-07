using System;
using UnityEngine;
using static Turret;

[CreateAssetMenu(fileName = "Resources/Components/ProjectileTurretScriptableObject", menuName = "Components/ProjectileTurret", order = 1)]
class ProjectileTurretScriptableObject : TurretScriptableObject {
    [Tooltip("Max at around 150")]
    public float fireVelocity;
    public float fireAccuracy;
    public int minDamage;
    public int maxDamage;
    public float projectileRange;
    public GameObject projectilePrefab;

    public override float GetDamagePerSecond() {
        float time = reloadSpeed;
        if (maxAmmo > 1) {
            time += maxAmmo * fireSpeed;
        }
        float damage = (minDamage + maxDamage) / 2f * maxAmmo;
        return damage / time;
    }

    public void Awake() {
        targeting = TargetingBehaviors.closest;
        if (projectilePrefab == null)
            projectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
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
