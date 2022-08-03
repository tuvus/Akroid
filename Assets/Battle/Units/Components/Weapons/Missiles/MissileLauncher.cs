using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public Unit targetUnit;

    public void SetupMissileLauncher(Unit unit) {
        this.unit = unit;
        reloadController = GetComponent<ReloadController>();
        reloadController.SetupReloadController();
    }

    public void UpdateMissileLauncher() {
        if (MissileLauncherHibernationStatus())
            return;
        reloadController.UpdateReloadController(Time.fixedDeltaTime * BattleManager.Instance.timeScale, 1);
        if (!reloadController.ReadyToFire())
            return;
        if (!IsTargetViable(targetUnit)) {
            FindNewTarget();
        }
        if (targetUnit != null) {
            Fire();
            targetUnit = null;
        }
    }

    public bool MissileLauncherHibernationStatus() {
        return targetUnit == null && unit.enemyUnitsInRange.Count == 0 && reloadController.ReadyToHibernate();
    }

    private bool IsTargetViable(Unit targetUnit) {
        if (targetUnit == null || !targetUnit.IsTargetable() || Vector2.Distance(transform.position, targetUnit.GetPosition()) > range)
            return false;
        return true;
    }

    private void FindNewTarget() {
        for (int i = 0; i < unit.enemyUnitsInRange.Count; i++) {
            Unit newTarget = unit.enemyUnitsInRange[i];
            if (!IsTargetViable(newTarget))
                continue;
            Vector2 targetLocation;
            float localShipAngle;
            if (IsTargetBetter(newTarget, targetUnit)) {
                targetUnit = newTarget;
            }
        }
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
                if (newTarget.GetTotalHealth() >= oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.weakest) {
                if (newTarget.GetTotalHealth() <= oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.slowest) {
                if (newTarget.GetVelocity().magnitude <= oldTarget.GetVelocity().magnitude) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.biggest) {
                if (newTarget.GetCost() >= oldTarget.GetCost()) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.smallest) {
                if (newTarget.GetCost() <= oldTarget.GetCost()) {
                    return true;
                }
            }
        }
        return false;
    }

    public void Fire() {
        reloadController.Fire();
        Missile missile = BattleManager.Instance.GetNewMissile();
        missile.SetMissile(unit.faction,transform.position,transform.eulerAngles.z, targetUnit, unit.GetVelocity(), missileDamage, missileThrust, missileTurnSpeed, missileFuelRange);
    }

    public float GetRange() {
        return missileFuelRange;
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
}
