using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(ReloadController))]
public class Turret : MonoBehaviour {
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

    private SpriteRenderer spriteRenderer;
    protected Unit unit;
    protected ReloadController reloadController;

    public Vector2 targetVector;
    public Unit targetUnit;
    private bool aimed;

    public virtual void SetupTurret(Unit unit) {
        this.unit = unit;
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetRotation = startRotation;
        reloadController = GetComponent<ReloadController>();
        reloadController.SetupReloadController();
    }

    public virtual void UpdateTurret(float deltaTime) {
        if (TurretHibernationStatus())
            return;
        Profiler.BeginSample(name);
        UpdateTurretReload(deltaTime);
        UpdateTurretAim(deltaTime);
        UpdateTurretWeapon(deltaTime);
        Profiler.EndSample();
    }

    protected virtual bool TurretHibernationStatus() {
        return targetUnit == null && aimed && unit.enemyUnitsInRange.Count == 0 && reloadController.ReadyToHibernate();
    }

    protected virtual void UpdateTurretReload(float deltaTime) {
        reloadController.UpdateReloadController(deltaTime, GetReloadTimeModifier());
    }

    protected void UpdateTurretAim(float deltaTime) {
        if (IsTargetViable(targetUnit) && IsTargetRotationViable(targetUnit, out Vector2 targetLocation, out float localShipAngle)) {
            SetTargetRotation(localShipAngle);
        } else {
            ChangeTargetUnit(null);
            SetTargetRotation(startRotation);
            FindNewTarget(GetRange(), unit.faction);
        }

        if (!aimed)
            RotateTowards(deltaTime);
    }

    protected virtual void UpdateTurretWeapon(float deltaTime) {
        if (aimed && targetUnit != null && ReadyToFire()) {
            Fire();
            ChangeTargetUnit(null);
        }
    }

    public bool IsTargetViable(Unit targetUnit) {
        if (targetUnit == null || !targetUnit.IsTargetable() || Vector2.Distance(transform.position, targetUnit.GetPosition()) > GetRange())
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
        for (int i = 0; i < unit.enemyUnitsInRange.Count; i++) {
            Unit targetUnit = unit.enemyUnitsInRange[i];
            if (!IsTargetViable(targetUnit))
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

    public virtual void Fire() {
        reloadController.Fire();
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
}
