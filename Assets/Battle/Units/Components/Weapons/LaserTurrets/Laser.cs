using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour {

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer startHighlight;
    [SerializeField] private SpriteRenderer endHighlight;
    LaserTurret laserTurret;
    bool fireing;

    float fireTime;
    float fadeTime;

    float translateAmount;

    RaycastHit2D? hitPoint;
    RaycastHit2D[] contacts = new RaycastHit2D[20];
    float extraDamage;

    public void SetLaser(LaserTurret laserTurret, float offset, float laserSize) {
        transform.localScale = new Vector2(laserSize, 1);
        this.translateAmount = offset;
        this.laserTurret = laserTurret;

        fireing = false;
        fireTime = 0;
        fadeTime = 0;

        spriteRenderer.enabled = false;
        startHighlight.enabled = false;
        endHighlight.enabled = false;
        startHighlight.transform.localScale = new Vector2(1 / laserSize, 1);
        endHighlight.transform.localScale = new Vector2(1 / laserSize, 1);
        extraDamage = 0;
    }

    public void FireLaser() {
        fireing = true;
        fireTime = laserTurret.GetFireDuration();
        fadeTime = laserTurret.GetFadeDuration();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.b, spriteRenderer.color.g, 1);
        startHighlight.color = new Color(startHighlight.color.r, startHighlight.color.b, startHighlight.color.g, 1);
        endHighlight.color = new Color(endHighlight.color.r, endHighlight.color.b, endHighlight.color.g, 1);
    }

    void ExpireLaser() {
        spriteRenderer.enabled = false;
        startHighlight.enabled = false;
        endHighlight.enabled = false;
        fireing = false;
    }


    public void UpdateLaser(float deltaTime) {
        if (fireing) {
            spriteRenderer.enabled = true;
            startHighlight.enabled = BattleManager.Instance.GetEffectsShown();
            endHighlight.enabled = BattleManager.Instance.GetEffectsShown();
            transform.localPosition = new Vector2(0, 0);
            transform.rotation = transform.parent.rotation;

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
            ExpireLaser();
        } else {
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.b, spriteRenderer.color.g, fadeTime / laserTurret.GetFireDuration());
            startHighlight.color = new Color(startHighlight.color.r, startHighlight.color.b, startHighlight.color.g, fadeTime / laserTurret.GetFadeDuration());
            endHighlight.color = new Color(endHighlight.color.r, endHighlight.color.b, endHighlight.color.g, fadeTime / laserTurret.GetFadeDuration());
        }
    }

    void UpdateDamageAndCollision(float deltaTime) {
        hitPoint = null;
        Physics2D.RaycastNonAlloc(transform.position, transform.up, contacts, GetLaserRange() + translateAmount);
        Shield hitShield = null;
        Unit hitUnit = null;

        int contactLength = -1;
        for (int i = 0; i < contacts.Length; i++) {
            if (contacts[i].collider == null) {
                contactLength = i + 1;
                break;
            }
            Unit unit = contacts[i].collider.GetComponent<Unit>();
            if (unit != null && unit.faction != laserTurret.GetUnit().faction) {
                if (!hitPoint.HasValue || contacts[i].distance < hitPoint.Value.distance) {
                    hitUnit = unit;
                    hitShield = null;
                    hitPoint = contacts[i];
                }
                continue;
            }
            Shield shield = contacts[i].collider.GetComponent<Shield>();
            if (shield != null && shield.GetUnit().faction != laserTurret.GetUnit().faction) {
                if (!hitPoint.HasValue || contacts[i].distance < hitPoint.Value.distance) {
                    hitUnit = null;
                    hitShield = shield;
                    hitPoint = contacts[i];
                }
                continue;
            }
        }
        for (int i = 0; i < contactLength; i++) {
            contacts[i] = new RaycastHit2D();
        }
        if (hitUnit != null) {
            hitUnit.TakeDamage(GetDamage(deltaTime, false));
            return;
        }
        if (hitShield != null) {
            hitShield.TakeDamage(GetDamage(deltaTime, true));
            return;
        }
    }

    int GetDamage(float deltaTime, bool hitShield) {
        float damage = laserTurret.GetLaserDamagePerSecond() * deltaTime * laserTurret.GetUnit().faction.GetImprovementModifier(Faction.ImprovementAreas.LaserDamage);
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
        transform.Translate(Vector2.up * translateAmount * laserTurret.transform.localScale.y);
        if (hitPoint.HasValue) {
            spriteRenderer.size = new Vector2(spriteRenderer.size.x, (hitPoint.Value.distance / laserTurret.transform.localScale.y - translateAmount) / laserTurret.GetUnitScale());
            endHighlight.transform.localPosition = new Vector2(0, spriteRenderer.size.y / 2);
            endHighlight.enabled = BattleManager.Instance.GetEffectsShown();
        } else {
            spriteRenderer.size = new Vector2(spriteRenderer.size.x, (GetLaserRange() / laserTurret.transform.localScale.y - translateAmount) / laserTurret.GetUnitScale());
            endHighlight.enabled = false;
        }
        transform.Translate(Vector2.up * spriteRenderer.size / 2 * laserTurret.transform.localScale.y * laserTurret.GetUnitScale());
        startHighlight.transform.localPosition = new Vector2(0, -spriteRenderer.size.y / 2);
    }

    public bool IsFireing() {
        return fireing;
    }

    public float GetLaserRange() {
        return laserTurret.GetLaserRange() * laserTurret.GetUnit().faction.GetImprovementModifier(Faction.ImprovementAreas.LaserRange);
    }

    public void ShowEffects(bool shown) { 
        startHighlight.enabled = shown;
        endHighlight.enabled = shown;
    }
}