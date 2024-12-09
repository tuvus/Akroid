using UnityEngine;

public class Projectile : BattleObject {
    private float speed;
    private int damage;
    private float projectileRange;
    private float distance;
    private Vector2 shipVelocity;
    private Vector2 startingScale;
    public float particleTime { get; private set; }
    public bool hit { get; private set; }

    public Projectile(BattleManager battleManager) : base(new BattleObjectData("Projectile"), battleManager) { }

    public void SetProjectile(Faction faction, Vector2 position, float rotation, Vector2 shipVelocity, float speed, int damage,
        float projectileRange, float offset, float scale) {
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
            position += shipVelocity * deltaTime;
            position += Calculator.GetPositionOutOfAngleAndDistance(rotation, deltaTime * speed);
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
            foreach (var shieldGenerator in unit.moduleSystem.Get<ShieldGenerator>()) {
                damage = shieldGenerator.shield.TakeDamage(damage);
                if (damage < 0) {
                    Explode(unit);
                    return;
                }
            }

            damage = unit.TakeDamage(damage);
            Explode(unit);
            return;
        }

        Shield shield = coll.GetComponent<Shield>();
        if (shield != null && shield.GetUnit().faction != faction) {
            damage = shield.TakeDamage(damage);
            if (damage == 0)
                Explode(unit);
            return;
        }
    }

    public void Explode(Unit unit) {
        hit = true;
        position = new Vector3(position.x, position.y, 10);
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

        visible = activate;
    }

    public void RemoveProjectile() {
        scale = startingScale;
        hit = false;
        Activate(false);
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/Projectile");
    }
}
