using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour {
    public enum MissileType {
        Hermes,
    }
    public int missileIndex { get; private set; }
    private Faction faction;
    private ParticleSystem explodeParticleSystem;
    private ParticleSystem thrustParticleSystem;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider2D;
    public MissileType missileType;
    private MissileLauncher missileLauncher;
    private Unit target;
    public int damage;
    public float thrustSpeed;
    public float maxTurnSpeed;
    private Vector2 velocity;
    float turnSpeed;
    public float fuelRange;
    public bool retarget;
    float distance;
    bool hit;
    bool expired;

    public void PrespawnMissile(int missileIndex, float particleSpeed) {
        explodeParticleSystem = GetComponent<ParticleSystem>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        thrustParticleSystem = transform.GetChild(0).GetComponent<ParticleSystem>();
        this.missileIndex = missileIndex;
        SetParticleSpeed(particleSpeed);
        Activate(false);
    }

    public void SetMissile(Faction faction, MissileLauncher missileLauncher, Vector2 position, float rotation, Unit target, Vector2 shipVelocity, int damage, float thrustSpeed, float maxTurnSpeed, float fuelRange, bool retarget) {
        transform.position = position;
        transform.eulerAngles = new Vector3(0, 0, rotation);
        this.faction = faction;
        this.missileLauncher = missileLauncher;
        this.target = target;
        this.damage = damage;
        this.thrustSpeed = thrustSpeed;
        this.maxTurnSpeed = maxTurnSpeed;
        turnSpeed = 0;
        this.velocity = shipVelocity;
        this.fuelRange = fuelRange;
        this.retarget = retarget;
        hit = false;
        expired = false;
        if (BattleManager.Instance.GetParticlesShown())
            thrustParticleSystem.Play();
        distance = 0;
        Activate(true);
    }

    public void UpdateMissile(float deltaTime) {
        if (hit) {
            if (explodeParticleSystem.isPlaying == false && thrustParticleSystem.isPlaying == false) {
                RemoveMissile();
            } else {
                transform.Translate(velocity * deltaTime);
            }
        } else if (expired) {
            if (thrustParticleSystem.isPlaying == false) {
                RemoveMissile();
            } else {
                transform.Translate(velocity * deltaTime);
            }
        } else {
            turnSpeed = Mathf.Min(maxTurnSpeed, turnSpeed + deltaTime * 50);
            if (target != null && target.IsTargetable()) {
                RotateMissile(deltaTime);
            } else if (retarget && missileLauncher != null && missileLauncher.GetUnit().IsSpawned()) {
                target = missileLauncher.FindNewTarget();
                if (target == null)
                    retarget = false;
            }

            MoveMissile(deltaTime);
        }
    }

    void RotateMissile(float deltaTime) {
        Vector2 targetPosition = Calculator.GetTargetPositionAfterTimeAndVelocity(transform.position, target.GetPosition(), velocity, target.GetVelocity(), thrustSpeed, 0);
        float targetAngle = Calculator.ConvertTo360DegRotation(Calculator.GetAngleOutOfTwoPositions(transform.position, targetPosition));
        float angle = Calculator.ConvertTo180DegRotation(targetAngle - transform.eulerAngles.z);
        float turnAmmont = turnSpeed * deltaTime;
        if (Mathf.Abs(angle) < turnAmmont) {
            transform.eulerAngles = new Vector3(0, 0, targetAngle);
        } else if (angle > turnAmmont) {
            transform.Rotate(Vector3.forward * turnAmmont);
        } else if (angle < turnAmmont) {
            transform.Rotate(Vector3.forward * -turnAmmont);
        }
    }

    void MoveMissile(float deltaTime) {
        transform.Translate(velocity * deltaTime);
        transform.Translate(Vector2.up * thrustSpeed * deltaTime);
        distance += thrustSpeed * deltaTime;
        if (distance >= fuelRange) {
            Expire();
        }
    }

    private void OnTriggerEnter2D(Collider2D coll) {
        if (hit || expired)
            return;
        Unit unit = coll.GetComponent<Unit>();
        if (unit != null && unit.IsSpawned() && unit.faction != faction) {
            if (unit.GetShieldGenerator() != null && unit.GetShieldGenerator().GetShield().health > 0) {
                damage = unit.GetShieldGenerator().GetShield().TakeDamage(damage);
                Explode();
                velocity = unit.GetVelocity();
                return;
            }
            damage = unit.TakeDamage(damage);
            Explode();
            velocity = unit.GetVelocity();
            return;
        }
        Shield shield = coll.GetComponent<Shield>();
        if (shield != null && shield.GetUnit().faction != faction) {
            damage = shield.TakeDamage((int)(damage * 0.5f));
            Explode();
            velocity = shield.GetUnit().GetVelocity();
            return;
        }
    }

    public void Explode() {
        hit = true;
        transform.eulerAngles = Vector3.zero;
        spriteRenderer.enabled = false;
        thrustParticleSystem.Stop(false);
        if (BattleManager.Instance.GetParticlesShown())
            explodeParticleSystem.Play(false);
    }


    public void Expire() {
        thrustParticleSystem.Stop();
        spriteRenderer.enabled = false;
        expired = true;
        boxCollider2D.enabled = false;
    }

    public void RemoveMissile() {
        thrustParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        explodeParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        Activate(false);
    }

    void Activate(bool activate = true) {
        if (activate) {
            BattleManager.Instance.AddMissile(this);
        } else {
            BattleManager.Instance.RemoveMissile(this);
        }
        spriteRenderer.enabled = activate;
        boxCollider2D.enabled = activate;
    }

    public void SetParticleSpeed(float speed) {
        var main = explodeParticleSystem.main;
        main.simulationSpeed = speed;
        main = thrustParticleSystem.main;
        main.simulationSpeed = speed;
    }

    public void ShowParticles(bool shown) {
        if (hit && !shown)
            explodeParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (!shown)
            thrustParticleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        else if (spriteRenderer.enabled)
            thrustParticleSystem.Play();
    }
}