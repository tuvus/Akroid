using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
	public int projectileIndex { get; private set; }
	private Faction faction;
	private ProjectileCollision projectileCollision;
	private float speed;
	private int damage;
	private float projectileRange;
	private float distance;
	private Vector2 shipVelocity;
	private Vector2 scale;

	public void PrespawnProjectile(int projectileIndex) {
		projectileCollision = GetComponent<ProjectileCollision>();
		this.projectileIndex = projectileIndex;
		projectileCollision.PrespawnProjectile(this);
		scale = transform.localScale;
		Activate(false);
	}

	public void SetProjectile(Faction faction, Vector2 position, float rotation, Vector2 shipVelocity, float speed, int damage, float projectileRange, float offset, float scale) {
		Activate(true);
		transform.position = position;
		transform.eulerAngles = new Vector3(0, 0, rotation);
		this.speed = speed;
		transform.Translate(Vector2.up * offset);
		this.shipVelocity = shipVelocity;
		this.faction = faction;
		this.damage = damage;
		this.projectileRange = projectileRange;
		transform.position = new Vector3(transform.position.x, transform.position.y, -5);
		transform.localScale *= scale;
		projectileCollision.SetProjectile();
		distance = 0;
	}

	public void UpdateProjectile() {
		if (projectileCollision.HasHit()) {
			projectileCollision.UpdateProjectile();
		} else {
			transform.position += new Vector3(shipVelocity.x * Time.fixedDeltaTime * BattleManager.Instance.timeScale, shipVelocity.y * Time.fixedDeltaTime * BattleManager.Instance.timeScale, 0);
			transform.Translate(Vector2.up * speed * Time.fixedDeltaTime * BattleManager.Instance.timeScale);
			distance += speed * Time.fixedDeltaTime * BattleManager.Instance.timeScale;
			if (distance >= projectileRange) {
				RemoveProjectile();
			}
		}
	}

	private void OnTriggerEnter2D(Collider2D coll) {
		Unit unit = coll.GetComponent<Unit>();
		if (unit != null && unit.faction != faction) {
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

	void Explode() {
		projectileCollision.enabled = true;
		projectileCollision.Hit(this);
	}

	void Activate(bool activate = true) {
		projectileCollision.Activate(activate);
	}

	public void RemoveProjectile() {
		projectileCollision.RemoveProjectile();
		BattleManager.Instance.RemoveProjectile(this);
		transform.localScale = scale;
		Activate(false);
	}
}
