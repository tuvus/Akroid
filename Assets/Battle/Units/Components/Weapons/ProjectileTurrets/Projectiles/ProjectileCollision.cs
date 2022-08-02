using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileCollision : MonoBehaviour {
	private Projectile projectile;
	private new ParticleSystem particleSystem;
	private SpriteRenderer spriteRenderer;
	private BoxCollider2D boxCollider2D;

	private bool hit;

	public void PrespawnProjectile(Projectile projectile) {
		this.projectile = projectile;
		particleSystem = GetComponent<ParticleSystem>();
		boxCollider2D = GetComponent<BoxCollider2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
	}

	public void SetProjectile() {
		spriteRenderer.enabled = true;
		boxCollider2D.enabled = true;
	}

	public void UpdateProjectile() {
		if (particleSystem.isPlaying == false && hit) {
			projectile.RemoveProjectile();
		}
	}

	public void Hit(Projectile projectile) {
		this.projectile = projectile;
		transform.position = new Vector3(transform.position.x, transform.position.y, 10);
		transform.localScale = new Vector2(.5f, .5f);
		transform.rotation.eulerAngles.Set(0, 0, 0);
		spriteRenderer.enabled = false;
		boxCollider2D.enabled = false;
		particleSystem.Play();
		hit = true;
	}

	public bool HasHit() {
		return hit;
    }

	public void RemoveProjectile() {
		spriteRenderer.enabled = false;
		boxCollider2D.enabled = false;
		particleSystem.Stop();
		particleSystem.Clear();
		hit = false;
	}

	public void Activate(bool activate) {
		spriteRenderer.enabled = activate;
		boxCollider2D.enabled = activate;
    }
}
