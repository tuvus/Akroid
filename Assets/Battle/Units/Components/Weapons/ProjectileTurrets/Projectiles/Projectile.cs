using UnityEngine;

public class Projectile : BattleObject {
    [SerializeField] private SpriteRenderer highlight;
    [SerializeField] private new ParticleSystem particleSystem;
    [SerializeField] private BoxCollider2D boxCollider2D;
    private float speed;
    private int damage;
    private float projectileRange;
    private float distance;
    private Vector2 shipVelocity;
    private Vector2 startingScale;
    private bool hit;

    public Projectile(BattleManager battleManager) : base(new BattleObjectData("Projectile"), battleManager) {
        Activate(false);
    }

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

        // highlight.enabled = BattleManager.Instance.GetEffectsShown();
        Activate(true);
    }

    public void UpdateProjectile(float deltaTime) {
        if (hit) {
            if (particleSystem.isPlaying == false) {
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
                damage = shieldGenerator.GetShield().TakeDamage(damage);
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
        highlight.enabled = false;
        position = new Vector3(position.x, position.y, 10);
        scale = new Vector2(.5f, .5f);
        if (unit != null) {
            shipVelocity = unit.GetVelocity();
        } else {
            shipVelocity = Vector2.zero;
        }

        visible = false;
        boxCollider2D.enabled = false;
        // if (BattleManager.Instance.GetParticlesShown())
            // particleSystem.Play();
    }

    void Activate(bool activate = true) {
        if (activate) {
            BattleManager.Instance.AddProjectile(this);
        } else {
            BattleManager.Instance.RemoveProjectile(this);
        }

        visible = activate;
        // boxCollider2D.enabled = activate;
    }

    public void RemoveProjectile() {
        scale = startingScale;
        particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        hit = false;
        highlight.enabled = false;
        Activate(false);
    }
}
