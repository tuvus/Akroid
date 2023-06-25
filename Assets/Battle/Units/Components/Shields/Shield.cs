using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour {
    Unit unit;
    ShieldGenerator shieldGenerator;
    private SpriteRenderer spriteRenderer;
    private Collider2D shieldCollider;
    public int health;
    public void SetShield(int health, ShieldGenerator shieldGenerator, Unit unit) {
        this.health = health;
        this.shieldGenerator = shieldGenerator;
        this.unit = unit;
        spriteRenderer = GetComponent<SpriteRenderer>();
        shieldCollider = GetComponent<Collider2D>();
        RefreshSheild();
    }

    public void RegenShield(int regenAmount) {
        health += regenAmount;
        if (health > shieldGenerator.GetMaxShieldStrength())
            health = shieldGenerator.GetMaxShieldStrength();
        RefreshSheild();
    }

    public int TakeDamage(int takeDamage) {
        health -= takeDamage;
        if (health <= 0) {
            int returnValue = -health;
            shieldGenerator.DestroyShield();
            health = 0;
            return returnValue;
        } else { 
            RefreshSheild();
            return 0;
        }
    }

    public void RefreshSheild() {
        float shieldPercent = (float)health / shieldGenerator.GetMaxShieldStrength();
        spriteRenderer.color = new Color(0, .4f, 1, .4f * shieldPercent);
    }

    public void ShowSield(bool show) {
        spriteRenderer.enabled = show;
        shieldCollider.enabled = show;
    }

    public Unit GetUnit() {
        return unit;
    }
}
