using Unity.Mathematics;
using UnityEngine;

public class MissileUI : BattleObjectUI, IParticleHolder {
    [SerializeField] private ParticleSystem thrust;
    [SerializeField] private SpriteRenderer highlight;
    [SerializeField] private DestroyEffectUI destroyEffectUI;
    private bool expired;
    private bool hit;

    private Missile missile;

    public void ShowEffects(bool shown) { }

    public void SetParticleSpeed(float speed) {
        ParticleSystem.MainModule main = thrust.main;
        main.simulationSpeed = speed;
        destroyEffectUI.SetParticleSpeed(speed);
    }
    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        missile = (Missile)battleObject;
        spriteRenderer.enabled = true;
        hit = false;
        expired = false;
        if (uIManager.GetEffectsShown()) highlight.enabled = true;
        if (uIManager.GetParticlesShown()) thrust.Play();
        ParticleSystem.MainModule main = thrust.main;
        main.simulationSpeed = uIManager.GetParticleSpeed();
        destroyEffectUI.SetupDestroyEffect(this, missile.missileScriptableObject.destroyEffect, uIManager,
            spriteRenderer, missile.missileScriptableObject.explosionSound, .20f, .8f,
            missile.missileScriptableObject.explosionPitch);
        uIManager.uiBattleManager.objectsToUpdate.Add(this);
        uIManager.uiBattleManager.particleHolders.Add(this);
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (missile.hit && !hit) {
            hit = true;
            destroyEffectUI.Explode(missile.GetDestroyEffect());
            ParticleSystem.EmissionModule emmission = thrust.emission;
            emmission.enabled = false;
            highlight.enabled = false;
        } else if (missile.expired && !expired) {
            expired = true;
            ParticleSystem.EmissionModule emission = thrust.emission;
            emission.enabled = false;
            highlight.enabled = false;
        } else if (hit) {
            destroyEffectUI.UpdateExplosion();
        } else {
            highlight.enabled = uIManager.GetEffectsShown();
            if (thrust.isPlaying && !uIManager.GetParticlesShown())
                thrust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public override void OnBattleObjectRemoved() {
        base.OnBattleObjectRemoved();
        uIManager.uiBattleManager.particleHolders.Remove(this);
        destroyEffectUI.OnBattleObjectRemoved();
        ParticleSystem.EmissionModule emission = thrust.emission;
        emission.enabled = false;
        highlight.enabled = false;
    }
}
