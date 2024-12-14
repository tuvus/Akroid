using System;
using UnityEngine;
using static MissileLauncher;

[CreateAssetMenu(fileName = "Resources/Components/MissileLauncherScriptableObject", menuName = "Components/MissileLauncher", order = 3)]
public class MissileLauncherScriptableObject : ComponentScriptableObject {
    public float DPS;
    public float range;
    public TargetingBehaviors targeting;

    public float fireSpeed;
    public float reloadSpeed;
    public int maxAmmo;

    public MissileScriptableObject missile;

    public virtual float GetDamagePerSecond() {
        float time = reloadSpeed;
        if (maxAmmo > 1) {
            time += maxAmmo * fireSpeed;
        }

        float damage = missile.damage / 2f * maxAmmo;
        return damage / time;
    }

    public override void OnValidate() {
        DPS = GetDamagePerSecond();
        base.OnValidate();
    }

    public void Awake() {
        targeting = TargetingBehaviors.closest;
    }

    public override Type GetComponentType() {
        return typeof(MissileLauncher);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(GetDamagePerSecond() * 22);
        AddResourceCost(CargoBay.CargoTypes.Metal, (long)(GetDamagePerSecond() * 4));
        AddResourceCost(CargoBay.CargoTypes.Gas, (long)(GetDamagePerSecond() * 14));
    }
}
