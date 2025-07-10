using Unity.Mathematics;
using UnityEngine;

public class MissileUI : BattleObjectUI, IParticleHolder {
    [SerializeField] private ParticleSystem thrust;
    [SerializeField] private SpriteRenderer highlight;
    [SerializeField] private DestroyEffectUI destroyEffectUI;
    private AudioSource explosionAudioSource;
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
        if (uIManager.GetEffectsShown()) highlight.enabled = true;
        if (uIManager.GetParticlesShown()) thrust.Play();
        ParticleSystem.MainModule main = thrust.main;
        main.simulationSpeed = uIManager.GetParticleSpeed();
        destroyEffectUI.SetupDestroyEffect(this, missile.missileScriptableObject.destroyEffect, uIManager,
            spriteRenderer);
        uIManager.uiBattleManager.objectsToUpdate.Add(this);
        uIManager.uiBattleManager.particleHolders.Add(this);
        explosionAudioSource = gameObject.AddComponent<AudioSource>();
        explosionAudioSource.resource = missile.missileScriptableObject.explosionSound;
        explosionAudioSource.playOnAwake = false;
        explosionAudioSource.spatialBlend = 1;
        explosionAudioSource.rolloffMode = AudioRolloffMode.Linear;
        explosionAudioSource.minDistance = 20;
        explosionAudioSource.maxDistance = 120;
        explosionAudioSource.pitch = missile.missileScriptableObject.explosionPitch;
        explosionAudioSource.dopplerLevel = 0;
        explosionAudioSource.volume = .2f;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (missile.hit && !hit) {
            hit = true;
            destroyEffectUI.Explode(missile.GetDestroyEffect());
            ParticleSystem.EmissionModule emmission = thrust.emission;
            emmission.enabled = false;
            highlight.enabled = false;

            explosionAudioSource.Play();
            destroyEffectUI.UpdateExplosion();
            float cameraZoom = uIManager.localPlayer.GetLocalPlayerInput().mainCamera.orthographicSize;
            explosionAudioSource.volume = (float)math.max(0, math.min(1, math.pow(600 / cameraZoom, .15) - 1)) * .5f;
            explosionAudioSource.minDistance = 5 + 5 * cameraZoom / 10;
            explosionAudioSource.maxDistance = 30 + 5 * cameraZoom / 10;
        } else if (missile.hit) {
            destroyEffectUI.UpdateExplosion();
            float cameraZoom = uIManager.localPlayer.GetLocalPlayerInput().mainCamera.orthographicSize;
            explosionAudioSource.volume = (float)math.max(0, math.min(1, math.pow(600 / cameraZoom, .15) - 1)) * .5f;
            explosionAudioSource.minDistance = 5 + 5 * cameraZoom / 10;
            explosionAudioSource.maxDistance = 30 + 5 * cameraZoom / 10;
        } else if (missile.expired && !expired) {
            expired = true;
            ParticleSystem.EmissionModule emission = thrust.emission;
            emission.enabled = false;
            highlight.enabled = false;
        } else {
            highlight.enabled = uIManager.GetEffectsShown();
            if (thrust.isPlaying && !uIManager.GetParticlesShown()) thrust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
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
