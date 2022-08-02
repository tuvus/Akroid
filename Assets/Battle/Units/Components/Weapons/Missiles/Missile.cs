using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour {
    public enum MissileType {
        Hermes,
    }
    private Faction faction;
    private new ParticleSystem explodeParticleSystem;
    private ParticleSystem thrustParticleSystem;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider2D;
    public MissileType missileType;
    private Unit target;
    public int damage;
    public float speed;
    public float maxTurnSpeed;
    float turnSpeed;
    public float lifetime;
    bool hit;

    public void PrespawnMissile() {
        explodeParticleSystem = GetComponent<ParticleSystem>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        thrustParticleSystem = transform.GetChild(0).GetComponent<ParticleSystem>();
    }

    public void SetupMissile(Faction faction, Unit target, Vector2 shipVelocity, int damage, float speed, float maxTurnSpeed, float lifetime) {
        this.faction = faction;
        this.target = target;
        this.damage = damage;
        this.speed = speed;
        this.maxTurnSpeed = maxTurnSpeed;
        this.lifetime = lifetime;
        hit = faction;
        boxCollider2D.enabled = true;
        spriteRenderer.enabled = true;
        explodeParticleSystem.Play();
        thrustParticleSystem.Play();
    }

    public void UpdateMissile() {

    }

    private void OnTriggerEnter2D(Collider2D coll) {
        Unit unit = coll.GetComponent<Unit>();
        if (unit != null && unit.faction != faction) {
            if (unit.GetShieldGenerator() != null) {
                damage = unit.GetShieldGenerator().GetShield().TakeDamage(damage);
                return;
            }
            damage = unit.TakeDamage(damage);
            Explode();
            return;
        }
        Shield shield = coll.GetComponent<Shield>();
        if (shield != null && shield.GetUnit().faction != faction) {
            damage = shield.TakeDamage(damage);
            if (damage == 0)
                Explode();
            return;
        }
    }

    public void Explode() {
        thrustParticleSystem.Stop(false);
        explodeParticleSystem.Play(false);

    }
}
