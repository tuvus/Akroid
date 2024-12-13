using System;
using Unity.Mathematics;
using UnityEngine;

public class Laser : BattleObject {
    public LaserTurret laserTurret { get; private set; }
    public bool fireing { get; private set; }

    public float fireTime { get; private set; }
    public float fadeTime { get; private set; }
    float extraDamage;

    public void SetLaser(LaserTurret laserTurret) {
        // transform.localScale = new Vector2(laserSize, 1);
        this.laserTurret = laserTurret;

        fireing = false;
        fireTime = 0;
        fadeTime = 0;

        extraDamage = 0;
        visible = false;
    }

    public void FireLaser() {
        fireing = true;
        fireTime = laserTurret.GetFireDuration();
        fadeTime = laserTurret.GetFadeDuration();
    }


    public void UpdateLaser(float deltaTime) {
        if (fireing) {
            UpdateDamageAndCollision(deltaTime);
            SetDistance();

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
        }
    }

    void UpdateDamageAndCollision(float deltaTime) {
        Shield hitShield = null;
        Unit hitUnit = null;
        float laserLength = GetLaserRange();
        float distanceToFaction = Vector2.Distance(position, faction.position) + size;
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

                Vector2 closestPoint = Calculator.GetClosestPointToAPointOnALine(firePosition, rotation, targetUnit.position);
                float distanceToClosestPoint = Vector2.Distance(firePosition, closestPoint);
                if (distanceToClosestPoint > targetUnit.size) continue;
                hitUnit = targetUnit;
            }
        }

        if (hitShield != null) {
            hitShield.TakeDamage(GetDamage(deltaTime, true));
        } else if (hitUnit != null) {
            hitUnit.TakeDamage(GetDamage(deltaTime, false));
        }
    }

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

    void SetDistance() {
        // transform.Translate(Vector2.up * translateAmount * laserTurret.scale.y);
        // if (hitPoint.HasValue) {
            // spriteRenderer.size = new Vector2(spriteRenderer.size.x,
            // (hitPoint.Value.distance / laserTurret.scale.y - translateAmount) / laserTurret.scale.y);
            // endHighlight.transform.localPosition = new Vector2(0, spriteRenderer.size.y / 2);
            // endHighlight.enabled = BattleManager.Instance.GetEffectsShown();
        // } else {
            // spriteRenderer.size = new Vector2(spriteRenderer.size.x,
            // (GetLaserRange() / laserTurret.scale.y - translateAmount) / laserTurret.scale.y);
            // endHighlight.enabled = false;
        // }

        // transform.Translate(Vector2.up * spriteRenderer.size / 2 * laserTurret.scale.y * laserTurret.scale.y);
        // startHighlight.transform.localPosition = new Vector2(0, -spriteRenderer.size.y / 2);
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
