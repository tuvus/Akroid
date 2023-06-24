using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(ReloadController))]
public class Turret : ModuleComponent {
    public enum TargetingBehaviors {
        closest = 1,
        strongest = 2,
        weakest = 3,
        slowest = 4,
        smallest = 5,
        biggest = 6,
    }
    //the time between checks
    public float targetRotation;
    public float startRotation;
    public float turretOffset;

    public float range;
    public float rotateSpeed;
    public TargetingBehaviors targeting;

    //if minRotate is bigger then maxRotate the turret can point forwards
    //if minRotate is equal to maxRotate then it is a 360Turret
    public float minRotate;
    public float maxRotate;

    protected Unit unit;
    protected ReloadController reloadController;

    public Vector2 targetVector;
    public Unit targetUnit;
    private bool aimed;
    private bool hibernating;
    private float findNewTargetUpdateSpeed = .2f;
    private float findNewTargetUpdateTime;

    public virtual void SetupTurret(Unit unit) {
        base.SetupBattleObject();
        this.unit = unit;
        targetRotation = startRotation;
        reloadController = GetComponent<ReloadController>();
        reloadController.SetupReloadController();
        findNewTargetUpdateTime = Random.Range(0, 0.2f);
    }

    public virtual void UpdateTurret(float deltaTime) {
        if (hibernating && unit.GetEnemyUnitsInRange().Count == 0) {
            return;
        } else if (TurretHibernationStatus()) {
            hibernating = true;
            return;
        }
        hibernating = false;
        Profiler.BeginSample("UpdateTurretAction");
        UpdateTurretReload(deltaTime);
        UpdateTurretAim(deltaTime);
        UpdateTurretWeapon(deltaTime);
        Profiler.EndSample();
    }

    protected virtual bool TurretHibernationStatus() {
        return targetUnit == null && aimed && unit.GetEnemyUnitsInRange().Count == 0 && reloadController.ReadyToHibernate();
    }

    protected virtual void UpdateTurretReload(float deltaTime) {
        reloadController.UpdateReloadController(deltaTime, GetReloadTimeModifier());
    }

    protected void UpdateTurretAim(float deltaTime) {
        float range = GetRange();
        if (findNewTargetUpdateTime > 0)
            findNewTargetUpdateTime -= deltaTime;
        if (IsTargetViable(targetUnit, range) && IsTargetRotationViable(targetUnit, out Vector2 targetLocation, out float localShipAngle)) {
            SetTargetRotation(localShipAngle);
        } else {
            ChangeTargetUnit(null);
            SetTargetRotation(startRotation);
            if (findNewTargetUpdateTime <= 0) {
                FindNewTarget(range, unit.faction);
                findNewTargetUpdateTime += findNewTargetUpdateSpeed;
            }
        }

        if (!aimed)
            RotateTowards(deltaTime);
    }

    protected virtual void UpdateTurretWeapon(float deltaTime) {
        if (aimed && targetUnit != null && ReadyToFire()) {
            if (Fire()) {
                ChangeTargetUnit(null);
            }
        }
    }

    public bool IsTargetViable(Unit targetUnit, float range) {
        if (targetUnit == null || !targetUnit.IsTargetable() || Vector2.Distance(transform.position, targetUnit.GetPosition()) > range)
            return false;
        return true;
    }

    public virtual bool IsTargetRotationViable(Unit targetUnit, out Vector2 targetLocation, out float localShipAngle) {
        targetLocation = GetTargetPosition(targetUnit);
        float realAngle = Calculator.GetAngleOutOfTwoPositions(transform.position, targetLocation);
        localShipAngle = Calculator.ConvertTo360DegRotation(realAngle - unit.transform.localRotation.eulerAngles.z);
        if (minRotate < maxRotate) {
            if (localShipAngle <= maxRotate && localShipAngle >= minRotate) {
                return true;
            }
        } else if (minRotate > maxRotate) {
            if (localShipAngle <= maxRotate || localShipAngle >= minRotate) {
                return true;
            }
        } else {
            return true;
        }
        return false;
    }

    public virtual void FindNewTarget(float range, Faction faction) {
        Unit newTarget = null;
        for (int i = 0; i < unit.GetEnemyUnitsInRange().Count; i++) {
            Unit targetUnit = unit.GetEnemyUnitsInRange()[i];
            if (!IsTargetViable(targetUnit, range))
                continue;
            Vector2 targetLocation;
            float localShipAngle;
            if (IsTargetRotationViable(targetUnit, out targetLocation, out localShipAngle)) {
                if (IsTargetBetter(targetUnit, newTarget)) {
                    targetVector = targetLocation;
                    SetTargetRotation(localShipAngle);
                    newTarget = targetUnit;
                    if (targeting == TargetingBehaviors.closest)
                        break;
                }
            }
        }
        if (newTarget == null) {
            SetTargetRotation(startRotation);
        }
        ChangeTargetUnit(newTarget);
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
                if (newTarget.GetSize() >= oldTarget.GetSize()) {
                    return true;
                }
            } else if (targeting == TargetingBehaviors.smallest) {
                if (newTarget.GetSize() <= oldTarget.GetSize()) {
                    return true;
                }
            }
        }
        return false;
    }

    void SetTargetRotation(float newTargetRotation) {
        if (targetRotation == newTargetRotation && aimed)
            return;
        targetRotation = newTargetRotation;
        if (newTargetRotation == transform.localRotation.eulerAngles.z) {
            aimed = true;

        } else {
            aimed = false;
        }
    }

    void RotateTowards(float deltaTime) {
        float localRotateSpeed = rotateSpeed * deltaTime;

        float rotation = transform.localRotation.eulerAngles.z;
        float target = Calculator.ConvertTo180DegRotation(targetRotation - rotation);
        rotation = Calculator.ConvertTo180DegRotation(rotation);

        float localMin = Calculator.ConvertTo180DegRotation(maxRotate - rotation);
        float localMax = Calculator.ConvertTo180DegRotation(minRotate - rotation);

        //Changes the path towards the target if the target is on the other side of the deadzone.
        if (minRotate < maxRotate) {
            if (localMax > 0 && localMin > 0) {
                if (localMax < target && target > 0) {
                    target = -180 + (-target - 180);
                }
            }
            if (localMax < 0 && localMin < 0) {
                if (localMin > target && target < 0) {
                    target = 180 + (-target + 180);
                }
            }
        } else if (minRotate > maxRotate) {
            if (localMax > 0 && localMin > 0) {
                if (localMin < target && target > 0) {
                    target = -180 + (-target - 180);
                }
            }
            if (localMax < 0 && localMin < 0) {
                if (localMax > target && target < 0) {
                    target = 180 + (-target + 180);
                }
            }
        }
        //If target is greater than zero turn left, if target is less than zero turn right.
        //If target equals zero the turret is aimed.
        if (0 < target) {
            if (0 > target - localRotateSpeed) {
                transform.localEulerAngles = new Vector3(0, 0, targetRotation);
                aimed = true;
            } else {
                transform.Rotate(Vector3.forward * localRotateSpeed);
                aimed = false;
            }
        } else if (0 > target) {
            if (0 < target + localRotateSpeed) {
                transform.localEulerAngles = new Vector3(0, 0, targetRotation);
                aimed = true;
            } else {
                transform.Rotate(Vector3.forward * -localRotateSpeed);
                aimed = false;
            }
        } else {
            aimed = true;
        }
    }

    public virtual bool ReadyToFire() {
        return reloadController.ReadyToFire();
    }

    /// <summary>
    /// Fire the turret, returns true if a new unit should be targeted.
    /// Returns false if a new unit should not be target.
    /// </summary>
    /// <returns>Should a new unit be targeted or not?</returns>
    public virtual bool Fire() {
        reloadController.Fire();
        return true;
    }

    public virtual float GetRange() {
        return range;
    }

    public virtual Vector2 GetTargetPosition(Unit target) {
        return target.GetPosition();
    }

    public void ChangeTargetUnit(Unit targetUnit) {
        this.targetUnit = targetUnit;
    }

    public void ShowTurret(bool show) {
        spriteRenderer.enabled = show;
    }

    public Unit GetUnit() {
        return unit;
    }

    public float GetUnitScale() {
        return unit.transform.localScale.y;
    }

    public float GetTurretOffSet() {
        return turretOffset * GetUnitScale();
    }

    public virtual float GetReloadTimeModifier() {
        return 1f;
    }

    public virtual float GetDamagePerSecond() {
        return 0;
    }

    public virtual void ShowEffects(bool shown) { }

    public virtual void StopFiring() {

    }

    protected class TurretScriptableObject : ScriptableObject {
        public float DPS;
        public Sprite turretSprite;
        public float turretOffset;

        public float range;
        public float rotateSpeed;
        public TargetingBehaviors targeting;
        private float findNewTargetUpdateSpeed;
        public float fireSpeed;
        public float reloadSpeed;
        public int maxAmmo;

        public virtual float GetDamagePerSecond() {
            return 0;
        }

        public void OnValidate() {
            DPS = GetDamagePerSecond();
        }
    }
}