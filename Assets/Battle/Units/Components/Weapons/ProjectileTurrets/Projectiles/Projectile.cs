using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : BattleObject, IParticleHolder {
    public int projectileIndex { get; private set; }
    private new ParticleSystem particleSystem;
    private BoxCollider2D boxCollider2D;
    private Faction faction;
    private float speed;
    private int damage;
    private float projectileRange;
    private float distance;
    private Vector2 shipVelocity;
    private Vector2 startingScale;
    private bool hit;

    public void PrespawnProjectile(int projectileIndex, float particleSpeed) {
        base.SetupBattleObject();
        particleSystem = GetComponent<ParticleSystem>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        this.projectileIndex = projectileIndex;
        startingScale = transform.localScale;
        SetParticleSpeed(particleSpeed);
        Activate(false);
    }

    public void SetProjectile(Faction faction, Vector2 position, float rotation, Vector2 shipVelocity, float speed, int damage, float projectileRange, float offset, float scale) {
        this.faction = faction;
        transform.position = position;
        transform.eulerAngles = new Vector3(0, 0, rotation);
        this.speed = speed;
        transform.Translate(Vector2.up * offset);
        this.shipVelocity = shipVelocity;
        this.damage = damage;
        this.projectileRange = projectileRange;
        transform.position = new Vector3(transform.position.x, transform.position.y, -5);
        transform.localScale = this.startingScale;
        transform.localScale *= scale;
        distance = 0;
        hit = false;
        Activate(true);
    }

    public void UpdateProjectile(float deltaTime) {
        if (hit) {
            if (particleSystem.isPlaying == false) {
                RemoveProjectile();
            }
        } else {
            transform.position += new Vector3(shipVelocity.x * deltaTime, shipVelocity.y * deltaTime, 0);
            transform.Translate(Vector2.up * speed * deltaTime);
            distance += speed * deltaTime;
            if (distance >= projectileRange) {
                RemoveProjectile();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D coll) {
        if (hit) {
            return;
        }
        Unit unit = coll.GetComponent<Unit>();
        if (unit != null && unit.IsSpawned() && unit.faction != faction) {
            if (unit.GetShieldGenerator() != null) {
                damage = unit.GetShieldGenerator().GetShield().TakeDamage(damage);
                if (damage < 0)
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
        hit = true;
        transform.position = new Vector3(transform.position.x, transform.position.y, 10);
        transform.localScale = new Vector2(.5f, .5f);
        transform.rotation.eulerAngles.Set(0, 0, 0);
        spriteRenderer.enabled = false;
        boxCollider2D.enabled = false;
        if (BattleManager.Instance.GetParticlesShown())
            particleSystem.Play();
    }

    void Activate(bool activate = true) {
        if (activate) {
            BattleManager.Instance.AddProjectile(this);
        } else {
            BattleManager.Instance.RemoveProjectile(this);
        }
        spriteRenderer.enabled = activate;
        boxCollider2D.enabled = activate;
    }

    public void RemoveProjectile() {
        transform.localScale = startingScale;
        particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        hit = false;
        Activate(false);
    }

    public void SetParticleSpeed(float speed) {
        var main = particleSystem.main;
        main.simulationSpeed = speed;
    }

    public void ShowParticles(bool shown) {
        if (hit && !shown) {
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}