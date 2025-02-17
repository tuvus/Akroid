using UnityEngine;
using UnityEngine.Profiling;

public class Projectile : BattleObject {
    private float speed;
    private int damage;
    private float projectileRange;
    private float distance;
    private Vector2 shipVelocity;
    public float particleTime { get; private set; }
    public bool hit { get; private set; }

    public Projectile(BattleManager battleManager) : base(new BattleObjectData("Projectile"), battleManager) { }

    public void SetProjectile(Faction faction, Vector2 position, float rotation, Vector2 shipVelocity, float speed, int damage,
        float projectileRange, float offset, float projectileScale) {
        this.faction = faction;
        this.position = position;
        this.rotation = rotation;
        this.speed = speed;
        this.position += Calculator.GetPositionOutOfAngleAndDistance(rotation, offset);
        this.shipVelocity = shipVelocity;
        this.damage = damage;
        this.projectileRange = projectileRange;
        distance = 0;
        hit = false;
        scale = new Vector2(projectileScale, projectileScale) / 2;

        Activate(true);
        SetSize(SetupSize());
    }

    public void UpdateProjectile(float deltaTime) {
        if (hit) {
            particleTime += deltaTime;
            if (particleTime >= 3) {
                RemoveProjectile();
            } else {
                position += shipVelocity * deltaTime;
            }
        } else {
            if (CheckProjectileCollision(deltaTime)) return;
            Vector2 newPosition = position + shipVelocity * deltaTime +
                Calculator.GetPositionOutOfAngleAndDistance(rotation, deltaTime * speed);
            position = newPosition;
            distance += speed * deltaTime;
            if (distance >= projectileRange) {
                RemoveProjectile();
            }
        }
    }

    private bool CheckProjectileCollision(float deltaTime) {
        float distanceTraveled = (shipVelocity.magnitude + speed) * deltaTime;
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

                // Start checking positions that the projectile traveled across
                int collisionChecks = 10;
                for (int j = 0; j < collisionChecks; j++) {
                    Vector2 tempPosition = position + (shipVelocity * j / collisionChecks) +
                        Calculator.GetPositionOutOfAngleAndDistance(rotation, deltaTime * speed * j / collisionChecks);
                    if (Vector2.Distance(tempPosition, targetUnit.position) > size + targetUnit.size) continue;

                    foreach (var shieldGenerator in targetUnit.moduleSystem.Get<ShieldGenerator>()) {
                        if (shieldGenerator.shield.IsSpawned()) {
                            shieldGenerator.shield.TakeDamage(damage);
                            position = tempPosition;
                            Explode(targetUnit);
                            return true;
                        }
                    }

                    targetUnit.TakeDamage(damage);
                    position = tempPosition;
                    Explode(targetUnit);
                    return true;
                }
            }
        }
        return false;
    }

    public void Explode(Unit unit) {
        hit = true;
        scale = new Vector2(.5f, .5f);
        if (unit != null) {
            shipVelocity = unit.GetVelocity();
        } else {
            shipVelocity = Vector2.zero;
        }

        visible = false;
    }

    void Activate(bool activate = true) {
        if (activate) {
            battleManager.AddProjectile(this);
        } else {
            battleManager.RemoveProjectile(this);
        }

        spawned = activate;
        visible = activate;
    }

    public void RemoveProjectile() {
        hit = false;
        Activate(false);
    }


    public override float GetSpriteSize() {
        return 2;
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/Projectile");
    }
}
