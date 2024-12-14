using UnityEngine;

public class MissileUI : BattleObjectUI {
    [SerializeField] private ParticleSystem thrust;
    [SerializeField] private SpriteRenderer highlight;
    [SerializeField] private DestroyEffectUI destroyEffectUI;

    private Missile missile;
    private bool hit;
    private bool expired;

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        missile = (Missile)battleObject;
        spriteRenderer.enabled = true;
        if (uIManager.GetParticlesShown()) thrust.Play();
        destroyEffectUI.SetupDestroyEffect(this, missile.missileScriptableObject.destroyEffect, uIManager, spriteRenderer);
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (missile.hit && !hit) {
            hit = true;
            destroyEffectUI.Explode(missile.GetDestroyEffect());
            var emmission = thrust.emission;
            emmission.enabled = false;
            highlight.enabled = false;
        } else if (missile.hit) {
            destroyEffectUI.UpdateExplosion();
            highlight.enabled = false;
        } else if (missile.expired && !expired) {
            expired = true;
            var emmission = thrust.emission;
            emmission.enabled = false;
        } else {
            highlight.enabled = uIManager.GetEffectsShown();
        }
    }
}
