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

    private ReloadController reloadController;
    public float range;
    public TargetingBehaviors targeting;

    public Unit targetUnit;
    private static float findNewTargetUpdateSpeed = .2f;
    private float findNewTargetUpdateTime;

    public MissileLauncher(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        missileLauncherScriptableObject = (MissileLauncherScriptableObject)componentScriptableObject;

        reloadController = new ReloadController(missileLauncherScriptableObject.fireSpeed, missileLauncherScriptableObject.reloadSpeed,
            missileLauncherScriptableObject.maxAmmo);
        findNewTargetUpdateTime = Random.Range(0, 0.2f);
    }

    /// <returns>True if the turret is hibernating, false otherwise </returns>
    public bool UpdateMissileLauncher(float deltaTime) {
        if (MissileLauncherHibernationStatus()) return true;

        Profiler.BeginSample("UpdateMissileLauncher");
        reloadController.UpdateReloadController(deltaTime, faction.GetImprovementModifier(Faction.ImprovementAreas.MissileReload));
        if (!reloadController.ReadyToFire()) {
            Profiler.EndSample();
            return false;
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
        return false;
    }

    public bool MissileLauncherHibernationStatus() {
        return targetUnit == null && unit.GetEnemyUnitsInRange().Count == 0 && reloadController.ReadyToHibernate();
    }

    private bool IsTargetViable(Unit targetUnit, float range) {
        if (targetUnit == null || !targetUnit.IsTargetable() || Vector2.Distance(GetWorldPosition(), targetUnit.GetPosition()) > range)
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
            if (missileLauncherScriptableObject.targeting == TargetingBehaviors.closest) {
                if (Vector2.Distance(newTarget.position, GetWorldPosition()) <= Vector2.Distance(oldTarget.position, GetWorldPosition())) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting == TargetingBehaviors.strongest) {
                if (newTarget.GetTotalHealth() > oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting == TargetingBehaviors.weakest) {
                if (newTarget.GetTotalHealth() < oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting == TargetingBehaviors.slowest) {
                if (newTarget.GetVelocity().magnitude < oldTarget.GetVelocity().magnitude) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting == TargetingBehaviors.biggest) {
                if (newTarget.GetSize() > oldTarget.GetSize()) {
                    return true;
                }
            } else if (missileLauncherScriptableObject.targeting == TargetingBehaviors.smallest) {
                if (newTarget.GetSize() < oldTarget.GetSize()) {
                    return true;
                }
            }
        }

        return false;
    }

    public void Fire() {
        reloadController.Fire();
        if (!battleManager.instantHit) {
            Missile missile = battleManager.GetNewMissile();
            missile.SetMissile(faction, this, missileLauncherScriptableObject.missile, GetWorldPosition(), GetWorldRotation(),
                targetUnit, unit.GetVelocity());
        } else {
            targetUnit.TakeDamage(GetDamage());
        }
    }

    public int GetDamage() {
        return Mathf.RoundToInt(missileLauncherScriptableObject.missile.damage *
            faction.GetImprovementModifier(Faction.ImprovementAreas.MissileDamage));
    }

    public float GetRange() {
        return missileLauncherScriptableObject.range * faction.GetImprovementModifier(Faction.ImprovementAreas.MissileRange);
    }

    public float GetFuelRange() {
        return missileLauncherScriptableObject.missile.fuelRange * faction.GetImprovementModifier(Faction.ImprovementAreas.MissileRange);
    }

    public float GetDamagePerSecond() {
        reloadController = new ReloadController(missileLauncherScriptableObject.fireSpeed, missileLauncherScriptableObject.reloadSpeed,
            missileLauncherScriptableObject.maxAmmo);
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }

        float damage = missileLauncherScriptableObject.missile.damage / 2f * reloadController.maxAmmo;
        return damage / time;
    }

    [ContextMenu("GetDamagePerSecond")]
    public void PrintDamagePerSecond() {
        Debug.Log(GetDamagePerSecond());
    }

    public Unit GetUnit() {
        return unit;
    }
}
