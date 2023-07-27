using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(ReloadController))]
public class MissileLauncher : ModuleComponent {
    public enum TargetingBehaviors {
        closest = 1,
        strongest = 2,
        weakest = 3,
        slowest = 4,
        smallest = 5,
        biggest = 6,
    }
    MissileLauncherScriptableObject missileLauncherScriptableObject;

    protected Unit unit;
    private ReloadController reloadController;
     public float range;
    public TargetingBehaviors targeting;

    public Unit targetUnit;
    private bool hibernating;

    private static float findNewTargetUpdateSpeed = .2f;
    private float findNewTargetUpdateTime;

    public override void SetupComponent(Module module, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, componentScriptableObject);
        missileLauncherScriptableObject = (MissileLauncherScriptableObject)componentScriptableObject;
    }

    public void SetupMissileLauncher(Unit unit) {
        this.unit = unit;
        reloadController = GetComponent<ReloadController>();
        reloadController.SetupReloadController(missileLauncherScriptableObject.fireSpeed,missileLauncherScriptableObject.reloadSpeed,missileLauncherScriptableObject.maxAmmo);
        findNewTargetUpdateTime = Random.Range(0, 0.2f);
    }

    public void UpdateMissileLauncher(float deltaTime) {
        if (hibernating && unit.GetEnemyUnitsInRange().Count == 0) {
            return;
        } else if (MissileLauncherHibernationStatus()) {
            hibernating = true;
            return;
        }
        hibernating = false;
        Profiler.BeginSample("UpdateMissileLauncher");
        reloadController.UpdateReloadController(deltaTime, unit.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileReload));
        if (!reloadController.ReadyToFire()) {
            Profiler.EndSample();
            return;
        }
        if (findNewTargetUpdateTime > 0)
            findNewTargetUpdateTime -= deltaTime;
        if (findNewTargetUpdateTime <= 0 && !IsTargetViable(targetUnit, GetRange())) {
            targetUnit = FindNewTarget(GetRange());
            findNewTargetUpdateTime += findNewTargetUpdateSpeed;
        }
        if (targetUnit != null) {
            Fire();
            targetUnit = null;
        }
        Profiler.EndSample();
    }

    public bool MissileLauncherHibernationStatus() {
        return targetUnit == null && unit.GetEnemyUnitsInRange().Count == 0 && reloadController.ReadyToHibernate();
    }

    private bool IsTargetViable(Unit targetUnit, float range) {
        if (targetUnit == null || !targetUnit.IsTargetable() || Vector2.Distance(transform.position, targetUnit.GetPosition()) > range)
            return false;
        return true;
    }

    public Unit FindNewTarget(float range) {
        Unit target = null;
        for (int i = 0; i < unit.GetEnemyUnitsInRange().Count; i++) {
            Unit newTarget = unit.GetEnemyUnitsInRange()[i];
            if (!IsTargetViable(newTarget, range))
                continue;
            if (IsTargetBetter(newTarget, targetUnit)) {
                target = newTarget;
            }
        }
        return target;
    }

    private bool IsTargetBetter(Unit newTarget, Unit oldTarget) {
        if (oldTarget == null)
            return true;
        //Targeting: close, strongest, weakest, slowest, biggest, smallest
        if (newTarget != null) {
            if (missileLauncherScriptableObject.targeting== TargetingBehaviors.closest) {
                if (Vector2.Distance(newTarget.transform.position, transform.position) <= Vector2.Distance(oldTarget.transform.position, transform.position)) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting== TargetingBehaviors.strongest) {
                if (newTarget.GetTotalHealth() > oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting== TargetingBehaviors.weakest) {
                if (newTarget.GetTotalHealth() < oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting== TargetingBehaviors.slowest) {
                if (newTarget.GetVelocity().magnitude < oldTarget.GetVelocity().magnitude) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting== TargetingBehaviors.biggest) {
                if (newTarget.GetSize() > oldTarget.GetSize()) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting== TargetingBehaviors.smallest) {
                if (newTarget.GetSize() < oldTarget.GetSize()) {
                    return true;
                }
            }
        }
        return false;
    }

    public void Fire() {
        reloadController.Fire();
        Missile missile = BattleManager.Instance.GetNewMissile();
        missile.SetMissile(unit.faction, this, transform.position, transform.eulerAngles.z, targetUnit, unit.GetVelocity(), GetDamage(), missileLauncherScriptableObject.missileThrust, missileLauncherScriptableObject.missileTurnSpeed, GetFuelRange(), missileLauncherScriptableObject.missileRetarget);
    }

    public int GetDamage() {
        return Mathf.RoundToInt(missileLauncherScriptableObject.missileDamage * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileDamage));
    }

    public float GetRange() {
        return missileLauncherScriptableObject.range * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileRange);
    }

    public float GetFuelRange() {
        return missileLauncherScriptableObject.missileFuelRange * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileRange);
    }

    public float GetDamagePerSecond() {
        reloadController = GetComponent<ReloadController>();
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }
        float damage = missileLauncherScriptableObject.missileDamage / 2f * reloadController.maxAmmo;
        return damage / time;
    }

    [ContextMenu("GetDamagePerSecond")]
    public void PrintDamagePerSecond() {
        print(GetDamagePerSecond());
    }

    public Unit GetUnit() {
        return unit;
    }
}
