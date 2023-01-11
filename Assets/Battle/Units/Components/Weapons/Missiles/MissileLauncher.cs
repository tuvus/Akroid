using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(ReloadController))]
public class MissileLauncher : MonoBehaviour {
    public enum TargetingBehaviors {
        closest = 1,
        strongest = 2,
        weakest = 3,
        slowest = 4,
        smallest = 5,
        biggest = 6,
    }
    protected Unit unit;
    private ReloadController reloadController;
    public float range;
    public TargetingBehaviors targeting;

    public int missileDamage;
    public float missileThrust;
    public float missileTurnSpeed;
    public float missileFuelRange;
    public bool missileRetarget;

    public Unit targetUnit;
    private bool hibernating;

    private static float findNewTargetUpdateSpeed = .2f;
    private float findNewTargetUpdateTime;

    public void SetupMissileLauncher(Unit unit) {
        this.unit = unit;
        reloadController = GetComponent<ReloadController>();
        reloadController.SetupReloadController();
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
            if (targeting == TargetingBehaviors.closest) {
                if (Vector2.Distance(newTarget.transform.position, transform.position) <= Vector2.Distance(oldTarget.transform.position, transform.position)) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.strongest) {
                if (newTarget.GetTotalHealth() > oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.weakest) {
                if (newTarget.GetTotalHealth() < oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.slowest) {
                if (newTarget.GetVelocity().magnitude < oldTarget.GetVelocity().magnitude) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.biggest) {
                if (newTarget.GetSize() > oldTarget.GetSize()) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.smallest) {
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
        missile.SetMissile(unit.faction, this, transform.position, transform.eulerAngles.z, targetUnit, unit.GetVelocity(), GetDamage(), missileThrust, missileTurnSpeed, GetFuelRange(), missileRetarget);
    }

    public int GetDamage() {
        return Mathf.RoundToInt(missileDamage * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileDamage));
    }

    public float GetRange() {
        return range * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileRange);
    }

    public float GetFuelRange() {
        return missileFuelRange * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.MissileRange);
    }

    public float GetDamagePerSecond() {
        reloadController = GetComponent<ReloadController>();
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }
        float damage = missileDamage / 2f * reloadController.maxAmmo;
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
