using UnityEngine;

public class Missile : BattleObject {
    public enum MissileType {
        Hermes,
    }

    public MissileScriptableObject missileScriptableObject { get; private set; }
    private MissileLauncher missileLauncher;
    private DestroyEffect destroyEffect;
    private Unit target;
    private Vector2 velocity;
    private float distance;
    public bool hit { get; private set; }
    public bool expired { get; private set; }
    private bool failedToFindRetarget;
    private float timeAfterExpire;

    public Missile(BattleManager battleManager) : base(new BattleObjectData("Missile"), battleManager) { }

    public void SetMissile(Faction faction, MissileLauncher missileLauncher, MissileScriptableObject missileScriptableObject,
        Vector2 position, float rotation, Unit target, Vector2 shipVelocity) {
        this.missileScriptableObject = missileScriptableObject;
        this.position = position;
        this.rotation = rotation;
        this.faction = faction;
        this.missileLauncher = missileLauncher;
        this.target = target;
        this.velocity = shipVelocity;
        destroyEffect = null;
        failedToFindRetarget = false;
        hit = false;
        expired = false;
        distance = 0;
        timeAfterExpire = 0;
        Activate(true);
        SetSize(SetupSize());
    }

    public void UpdateMissile(float deltaTime) {
        if (hit) {
            if (!destroyEffect.UpdateDestroyEffect(deltaTime)) {
                Activate(false);
            }

            position += velocity * deltaTime;
        } else if (expired) {
            timeAfterExpire += deltaTime;
            if (timeAfterExpire >= missileScriptableObject.timeAfterExpire) {
                Activate(false);
            }
        } else {
            if (CheckMissileCollision(deltaTime)) return;
            if (target != null && target.IsTargetable()) {
                RotateMissile(deltaTime);
            } else if (!failedToFindRetarget && missileScriptableObject.retarget && missileLauncher != null &&
                missileLauncher.GetUnit().IsSpawned()) {
                // We can only find a new target if the original ship is still alive to give the missile a new target.
                target = missileLauncher.FindNewTarget(missileLauncher.GetRange());

                // If we have failed to find a new target, give up. We don't want to check every single frame for a new target.
                if (target == null) failedToFindRetarget = true;
            }

            MoveMissile(deltaTime);
        }
    }

    void RotateMissile(float deltaTime) {
        Vector2 targetPosition =
            Calculator.GetTargetPositionAfterTimeAndVelocity(position, target.GetPosition(), velocity, target.GetVelocity(),
                missileScriptableObject.thrust, 0);
        float targetAngle = Calculator.ConvertTo360DegRotation(Calculator.GetAngleOutOfTwoPositions(position, targetPosition));
        float angle = Calculator.ConvertTo180DegRotation(targetAngle - rotation);
        float turnAmmont = missileScriptableObject.turnSpeed * deltaTime;
        if (Mathf.Abs(angle) < turnAmmont) {
            rotation = targetAngle;
        } else if (angle > turnAmmont) {
            rotation += turnAmmont;
        } else if (angle < turnAmmont) {
            rotation -= turnAmmont;
        }
    }

    void MoveMissile(float deltaTime) {
        position += velocity * deltaTime +
            Calculator.GetPositionOutOfAngleAndDistance(rotation, missileScriptableObject.thrust * deltaTime);
        distance += missileScriptableObject.thrust * deltaTime;
        if (distance >= missileScriptableObject.fuelRange) {
            Expire();
        }
    }


    private bool CheckMissileCollision(float deltaTime) {
        float distanceTraveled = (velocity.magnitude + missileScriptableObject.thrust) * deltaTime;
        float distanceToFaction = Vector2.Distance(position, faction.position) + size + distanceTraveled;
        for (int g = 0; g < faction.closeEnemyGroupsDistance.Count; g++) {
            // If the distance from the faction to the group is greater than our distance to the faction
            // Then there is no chance that we can collide with anything in the group
            // Or any other group farther away from the faction since closeEnemyGroupsDistance is sorted from closest to farthest
            if (faction.closeEnemyGroupsDistance[g] > distanceToFaction) break;
            foreach (var targetUnit in faction.closeEnemyGroups[g].battleObjects) {
                if (!targetUnit.IsTargetable()) continue;
                float distanceToUnit = Vector2.Distance(position, targetUnit.GetPosition());
                if (distanceToUnit > targetUnit.size + size + distanceTraveled) continue;

                // Start checking positions that the missile traveled across
                int collisionChecks = 10;
                for (int j = 0; j < collisionChecks; j++) {
                    Vector2 tempPosition = position + (velocity * j / collisionChecks) +
                        Calculator.GetPositionOutOfAngleAndDistance(rotation,
                            deltaTime * missileScriptableObject.thrust * j / collisionChecks);
                    if (Vector2.Distance(tempPosition, targetUnit.position) > size + targetUnit.size) continue;

                    foreach (var shieldGenerator in targetUnit.moduleSystem.Get<ShieldGenerator>()) {
                        shieldGenerator.shield.TakeDamage(missileScriptableObject.damage / 2);
                        position = tempPosition;
                        Explode(targetUnit);
                        return true;
                    }

                    targetUnit.TakeDamage(missileScriptableObject.damage);
                    position = tempPosition;
                    Explode(targetUnit);
                    return true;
                }
            }
        }

        return false;
    }

    public void Explode(Unit targetUnit) {
        if (targetUnit != null) {
            velocity = targetUnit.GetVelocity();
        } else {
            velocity = Vector2.zero;
        }

        hit = true;
        rotation = 0;
        visible = false;
        destroyEffect = new DestroyEffect(missileScriptableObject.destroyEffect);
    }

    public void Expire() {
        visible = false;
        expired = true;
    }

    void Activate(bool activate = true) {
        if (activate) {
            battleManager.AddMissile(this);
        } else {
            battleManager.RemoveMissile(this);
        }

        visible = activate;
    }

    public override float GetSpriteSize() {
        return Calculator.GetSpriteSize(missileScriptableObject.sprite, scale);
    }

    public DestroyEffect GetDestroyEffect() {
        return destroyEffect;
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/Missile");
    }
}
