using UnityEngine;

public class ProjectileUI : BattleObjectUI {
    [SerializeField] private SpriteRenderer highlight;
    [SerializeField] private new ParticleSystem particleSystem;

    private Projectile projectile;
    private bool hit;

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        projectile = (Projectile)battleObject;
        spriteRenderer.enabled = true;
        highlight.enabled = uIManager.GetEffectsShown();
        uIManager.unitSpriteManager.objectsToUpdate.Add(this);
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (projectile.hit && !hit && uIManager.localPlayer.GetInputManager().IsObjectInViewingField(this)) {
            hit = true;
            if (uIManager.GetParticlesShown()) particleSystem.Play();
            highlight.enabled = false;
        }
    }

    public override void OnBattleObjectRemoved() {
        spriteRenderer.enabled = false;
        highlight.enabled = false;
        particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
