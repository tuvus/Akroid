using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// The laser's position is the midpoint of both ends of the laser, it's size will reflect how far the laser shoots.
/// </summary>
public class Laser : BattleObject {
    public LaserTurret laserTurret { get; private set; }
    public bool fireing { get; private set; }

    public float fireTime { get; private set; }
    public float fadeTime { get; private set; }
    public Vector2? hitPoint { get; private set; }
    public float laserLength { get; private set; }
    private float extraDamage;

    public Laser(BattleObjectData battleObjectData, BattleManager battleManager, LaserTurret laserTurret) :
        base(battleObjectData, battleManager) {
        this.laserTurret = laserTurret;
        fireing = false;
        fireTime = 0;
        fadeTime = 0;
        extraDamage = 0;
        visible = false;
        hitPoint = null;
        laserLength = 0;
    }

    public void FireLaser() {
        fireing = true;
        fireTime = laserTurret.GetFireDuration();
        fadeTime = laserTurret.GetFadeDuration();
    }


    public void UpdateLaser(float deltaTime) {
        if (fireing) {
            visible = true;
            var hit = FindCollision();
            if (hit != null) {
                DoDamage(hit.Item1, deltaTime);
                SetDistance(hit.Item2);
                hitPoint = hit.Item3;
            } else {
                SetDistance(GetLaserRange());
                hitPoint = null;
            }

            if (fireTime > 0) {
                UpdateFireTime(deltaTime);
            } else {
                UpdateFadeTime(deltaTime);
            }
        }
    }

    void UpdateFireTime(float deltaTime) {
        fireTime = Mathf.Max(0, fireTime - deltaTime);
    }

    void UpdateFadeTime(float deltaTime) {
        fadeTime = Mathf.Max(0, fadeTime - deltaTime);
        if (fadeTime <= 0) {
            fireing = false;
            visible = false;
        }
    }

    [CanBeNull]
    private Tuple<Unit, float, Vector2> FindCollision() {
        Unit hitUnit = null;
        float? hitDistance = null;
        Vector2? hitPosition = null;
        float laserLength = GetLaserRange();
        float distanceToFaction = Vector2.Distance(laserTurret.GetWorldPosition(), faction.position) + laserLength;
        Vector2 firePosition = laserTurret.GetWorldPosition() +
            Calculator.GetPositionOutOfAngleAndDistance(rotation, laserTurret.GetTurretOffSet());
        for (int g = 0; g < faction.closeEnemyGroupsDistance.Count; g++) {
            // If the distance from the faction to the group is greater than our distance to the faction
            // Then there is no chance that we can collide with anything in the group
            // Or any other group farther away from the faction since closeEnemyGroupsDistance is sorted from closest to farthest
            if (faction.closeEnemyGroupsDistance[g] > distanceToFaction + laserLength) break;
            foreach (var targetUnit in faction.closeEnemyGroups[g].battleObjects) {
                float distanceToUnit = Vector2.Distance(position, targetUnit.GetPosition());
                if (distanceToUnit > targetUnit.size + size + laserLength) continue;

                // Check if the laser could possibly hit the unit given the max laserLength
                Vector2 closestPoint =
                    Calculator.GetClosestPointToAPointOnALine(firePosition, laserTurret.GetWorldRotation(), targetUnit.position);
                float closestPointDistanceToTargetUnit = Vector2.Distance(closestPoint, targetUnit.position);
                if (closestPointDistanceToTargetUnit > targetUnit.size) continue;

                // If the distance to the center of the unit is greater than the laserLength plus the target unit's size
                // Then there is no possibility that this can be the closest point hit
                float distanceToClosestPoint = Vector2.Distance(firePosition, closestPoint);
                if (distanceToClosestPoint - targetUnit.size > laserLength) continue;

                // Now we need to find a good approximation of the closest point that hits the unit
                // To do this we binary search for the closest point that still hits the unit
                float? newHitDistance = null;
                Vector2? newHitPosition = null;
                float maxDistance = math.min(laserLength, distanceToClosestPoint);
                float minDistance = distanceToClosestPoint - targetUnit.size;
                while (maxDistance - minDistance > .2f) {
                    float midDistance = (maxDistance + minDistance) / 2;
                    Vector2 midHitPosition = firePosition +
                        Calculator.GetPositionOutOfAngleAndDistance(laserTurret.GetWorldRotation(), midDistance);
                    float distanceToMid = Vector2.Distance(midHitPosition, targetUnit.position);
                    if (distanceToMid > targetUnit.size) {
                        minDistance = midDistance;
                    } else {
                        newHitDistance = midDistance;
                        newHitPosition = midHitPosition;
                        maxDistance = midDistance;
                    }
                }

                // The point of collision with the unit might be farther than a collision with another unit, in this case hitDistance will be null
                if (newHitDistance == null) continue;

                // This is the closest hit so far so save the hitDistance and hitPosition
                hitUnit = targetUnit;
                hitPosition = newHitPosition;
                hitDistance = newHitDistance;
                laserLength = (float)newHitDistance;
            }
        }

        if (hitUnit == null) return null;
        return new Tuple<Unit, float, Vector2>(hitUnit, (float)hitDistance, (Vector2)hitPosition);
    }

    private void DoDamage(Unit hitUnit, float deltaTime) {
        int damage = GetDamage(deltaTime, true);
        if (hitUnit.GetShields() > 0) {
            foreach (var shieldGenerator in hitUnit.moduleSystem.Get<ShieldGenerator>()) {
                damage = shieldGenerator.shield.TakeDamage(damage);
                if (damage <= 0) return;
            }
        }
        hitUnit.TakeDamage(damage);
    }

    /// <summary>
    /// Calculates the damage for this frame.
    /// Since the damage is calculated based off of the duration between the last frame the damage is calculated as a float and then truncated to an int.
    /// Any extra damage will be added to the damage on the next frame.
    /// </summary>
    int GetDamage(float deltaTime, bool hitShield) {
        float damage = laserTurret.GetLaserDamagePerSecond() * deltaTime *
            laserTurret.GetUnit().faction.GetImprovementModifier(Faction.ImprovementAreas.LaserDamage);
        float damageToShield = 0.5f;
        if (hitShield)
            damage *= damageToShield;
        if (fireTime <= 0)
            damage *= fadeTime / laserTurret.GetFadeDuration();
        damage += extraDamage;
        extraDamage = damage - (int)damage;
        return (int)damage;
    }

    private void SetDistance(float laserLength) {
        position = laserTurret.GetWorldPosition() +
            Calculator.GetPositionOutOfAngleAndDistance(rotation, laserTurret.GetTurretOffSet())
            + Calculator.GetPositionOutOfAngleAndDistance(laserTurret.GetWorldRotation(), laserLength);
        this.laserLength = laserLength;
    }

    public bool IsFireing() {
        return fireing;
    }

    public float GetLaserRange() {
        return laserTurret.GetLaserRange() * laserTurret.GetUnit().faction.GetImprovementModifier(Faction.ImprovementAreas.LaserRange);
    }

    public override GameObject GetPrefab() {
        return laserTurret.laserTurretScriptableObject.laserPrefab;
    }
}
