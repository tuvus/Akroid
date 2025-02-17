using UnityEngine;

public class MissileUI : BattleObjectUI, IParticleHolder {
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
        var main = thrust.main;
        main.simulationSpeed = uIManager.GetParticleSpeed();
        destroyEffectUI.SetupDestroyEffect(this, missile.missileScriptableObject.destroyEffect, uIManager, spriteRenderer);
        uIManager.uiBattleManager.objectsToUpdate.Add(this);
        uIManager.uiBattleManager.particleHolders.Add(this);
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

    public override void OnBattleObjectRemoved() {
        base.OnBattleObjectRemoved();
        uIManager.uiBattleManager.particleHolders.Remove(this);
        destroyEffectUI.OnBattleObjectRemoved();
    }

    public void ShowEffects(bool shown) { }

    public void SetParticleSpeed(float speed) {
        var main = thrust.main;
        main.simulationSpeed = speed;
        destroyEffectUI.SetParticleSpeed(speed);
    }

    public void ShowParticles(bool shown) { }
}
