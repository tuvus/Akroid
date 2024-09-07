using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : BattleObject {
    public enum MissileType {
        Hermes,
    }
    public int missileIndex { get; private set; }
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

    public Missile(BattleManager battleManager) : base(new BattleObjectData("Missile"), battleManager) {
        Activate(false);
    }

    public void SetMissile(Faction faction, MissileLauncher missileLauncher, Vector2 position, float rotation, Unit target, Vector2 shipVelocity, int damage, float thrustSpeed, float maxTurnSpeed, float fuelRange, bool retarget) {
        this.position = position;
        this.rotation = rotation;
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
        distance = 0;
        Activate(true);
    }

    public void UpdateMissile(float deltaTime) {
        if (hit) {
            // if (!destroyEffect.IsPlaying() && thrustParticleSystem.isPlaying == false) {
            //     RemoveMissile();
            // } else {
            //     destroyEffect.UpdateExplosion(deltaTime);
            //     transform.Translate(velocity * deltaTime);
            // }
        } else if (expired) {
            // if (thrustParticleSystem.isPlaying == false) {
            //     RemoveMissile();
            // } else {
            //     transform.Translate(velocity * deltaTime);
            // }
        } else {
            turnSpeed = Mathf.Min(maxTurnSpeed, turnSpeed + deltaTime * 50);
            if (target != null && target.IsTargetable()) {
                RotateMissile(deltaTime);
            } else if (retarget && missileLauncher != null && missileLauncher.GetUnit().IsSpawned()) {
                target = missileLauncher.FindNewTarget(missileLauncher.GetRange());
                if (target == null)
                    retarget = false;
            }

            MoveMissile(deltaTime);
        }
    }

    void RotateMissile(float deltaTime) {
        Vector2 targetPosition = Calculator.GetTargetPositionAfterTimeAndVelocity(position, target.GetPosition(), velocity, target.GetVelocity(), thrustSpeed, 0);
        float targetAngle = Calculator.ConvertTo360DegRotation(Calculator.GetAngleOutOfTwoPositions(position, targetPosition));
        float angle = Calculator.ConvertTo180DegRotation(targetAngle - rotation);
        float turnAmmont = turnSpeed * deltaTime;
        if (Mathf.Abs(angle) < turnAmmont) {
            rotation = targetAngle;
        } else if (angle > turnAmmont) {
            rotation += turnAmmont;
        } else if (angle < turnAmmont) {
            rotation -= turnAmmont;
        }
    }

    void MoveMissile(float deltaTime) {
        position += velocity * deltaTime;
        position += Calculator.GetPositionOutOfAngleAndDistance(rotation, deltaTime * thrustSpeed);
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
            foreach (var shieldGenerator in unit.moduleSystem.Get<ShieldGenerator>()) {
                if (shieldGenerator.GetShield().health > 0) {
                    damage = shieldGenerator.GetShield().TakeDamage(damage);
                    Explode();
                    velocity = unit.GetVelocity();
                    return;
                }
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
        rotation = 0;
        visible = false;
    }


    public void Expire() {
        visible = false;
        expired = true;
    }

    public void RemoveMissile() {
        Activate(false);
    }

    void Activate(bool activate = true) {
        if (activate) {
            BattleManager.Instance.AddMissile(this);
        } else {
            BattleManager.Instance.RemoveMissile(this);
        }

        visible = activate;
    }
}