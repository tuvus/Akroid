﻿using System;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public abstract class Turret : ModuleComponent {
    public enum TargetingBehaviors {
        closest = 1,
        strongest = 2,
        weakest = 3,
        slowest = 4,
        smallest = 5,
        biggest = 6,
    }

    public TurretScriptableObject turretScriptableObject { get; private set; }

    protected ReloadController reloadController;

    public float targetRotation;
    public Vector2 targetVector;
    public Unit targetUnit;
    private bool aimed;
    private float findNewTargetUpdateSpeed = .2f;
    private float findNewTargetUpdateTime;
    private Random random;
    private float turretOffset;

    public event Action OnFire = delegate { };

    public Turret(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        turretScriptableObject = (TurretScriptableObject)base.componentScriptableObject;
        scale *= turretScriptableObject.baseScale;
        SetSize(GetSpriteSize());
        reloadController = new ReloadController(turretScriptableObject.fireSpeed, turretScriptableObject.reloadSpeed,
            turretScriptableObject.maxAmmo);
        random = new Random((uint)battleManager.units.Count + 1);
        findNewTargetUpdateTime = random.NextFloat(0, 0.2f);
        visible = true;
        turretOffset = turretScriptableObject.turretOffset * scale.y;
        SetSize(SetupSize());
    }

    /// <returns>True if the turret is hibernating, false otherwise </returns>
    public virtual bool UpdateTurret(float deltaTime) {
        if (TurretHibernationStatus()) return true;

        UpdateTurretReload(deltaTime);
        UpdateTurretAim(deltaTime);
        UpdateTurretWeapon(deltaTime);
        return false;
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
            SetTargetRotation(module.GetRotation());
            if (findNewTargetUpdateTime <= 0) {
                FindNewTarget(range);
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
        if (targetUnit == null || !targetUnit.IsTargetable() || Vector2.Distance(GetWorldPosition(), targetUnit.GetPosition()) > range)
            return false;
        return true;
    }

    public virtual bool IsTargetRotationViable(Unit targetUnit, out Vector2 targetLocation, out float localShipAngle) {
        targetLocation = GetTargetPosition(targetUnit);
        float realAngle = Calculator.GetAngleOutOfTwoPositions(GetWorldPosition(), targetLocation);
        localShipAngle = Calculator.ConvertTo360DegRotation(realAngle - unit.rotation);
        if (module.GetMinRotation() < module.GetMaxRotation()) {
            if (localShipAngle <= module.GetMaxRotation() && localShipAngle >= module.GetMinRotation()) {
                return true;
            }
        } else if (module.GetMinRotation() > module.GetMaxRotation()) {
            if (localShipAngle <= module.GetMaxRotation() || localShipAngle >= module.GetMinRotation()) {
                return true;
            }
        } else {
            return true;
        }

        return false;
    }

    public virtual void FindNewTarget(float range) {
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
                    if (turretScriptableObject.targeting == TargetingBehaviors.closest)
                        break;
                }
            }
        }

        if (newTarget == null) {
            SetTargetRotation(module.GetRotation());
        }

        ChangeTargetUnit(newTarget);
    }

    private bool IsTargetBetter(Unit newTarget, Unit oldTarget) {
        if (oldTarget == null)
            return true;
        if (random.NextInt(0, 10) < 3) return true;
        //Targeting: close, strongest, weakest, slowest, biggest, smallest
        if (newTarget != null) {
            if (turretScriptableObject.targeting == TargetingBehaviors.closest) {
                if (Vector2.Distance(newTarget.position, GetWorldPosition()) <= Vector2.Distance(oldTarget.position, GetWorldPosition())) {
                    return true;
                }
            } else if (turretScriptableObject.targeting == TargetingBehaviors.strongest) {
                if (newTarget.GetTotalHealth() >= oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (turretScriptableObject.targeting == TargetingBehaviors.weakest) {
                if (newTarget.GetTotalHealth() <= oldTarget.GetTotalHealth()) {
                    return true;
                }
            } else if (turretScriptableObject.targeting == TargetingBehaviors.slowest) {
                if (newTarget.GetVelocity().magnitude <= oldTarget.GetVelocity().magnitude) {
                    return true;
                }
            } else if (turretScriptableObject.targeting == TargetingBehaviors.biggest) {
                if (newTarget.GetSize() >= oldTarget.GetSize()) {
                    return true;
                }
            } else if (turretScriptableObject.targeting == TargetingBehaviors.smallest) {
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
        if (newTargetRotation == rotation) {
            aimed = true;
        } else {
            aimed = false;
        }
    }

    void RotateTowards(float deltaTime) {
        float localRotateSpeed = turretScriptableObject.rotateSpeed * deltaTime;

        float tempRotation = rotation;
        float target = Calculator.ConvertTo180DegRotation(targetRotation - tempRotation);
        tempRotation = Calculator.ConvertTo180DegRotation(tempRotation);

        float localMin = Calculator.ConvertTo180DegRotation(module.GetMaxRotation() - tempRotation);
        float localMax = Calculator.ConvertTo180DegRotation(module.GetMinRotation() - tempRotation);

        //Changes the path towards the target if the target is on the other side of the deadzone.
        if (module.GetMinRotation() < module.GetMaxRotation()) {
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
        } else if (module.GetMinRotation() > module.GetMaxRotation()) {
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
                rotation = targetRotation;
                aimed = true;
            } else {
                rotation += localRotateSpeed;
                aimed = false;
            }
        } else if (0 > target) {
            if (0 < target + localRotateSpeed) {
                rotation = targetRotation;
                aimed = true;
            } else {
                rotation -= localRotateSpeed;
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
        OnFire();
        return true;
    }

    public virtual float GetRange() {
        return turretScriptableObject.range;
    }

    public virtual Vector2 GetTargetPosition(Unit target) {
        return target.GetPosition();
    }

    public void ChangeTargetUnit(Unit targetUnit) {
        this.targetUnit = targetUnit;
    }

    public void ShowTurret(bool show) {
        visible = show;
    }

    public Unit GetUnit() {
        return unit;
    }

    public float GetTurretOffSet() {
        return turretOffset;
    }

    public virtual float GetReloadTimeModifier() {
        return 1f;
    }

    public virtual float GetDamagePerSecond() {
        return 0;
    }

    public virtual void ShowEffects(bool shown) { }

    public virtual void StopFiring() { }

    public override float GetSpriteSize() {
        return Calculator.GetSpriteSizeFromBounds(turretScriptableObject.spriteBounds, scale);
    }
}
