using Unity.Mathematics;
using UnityEngine;

public class ProjectileUI : BattleObjectUI, IParticleHolder {
    [SerializeField] private SpriteRenderer highlight;
    [SerializeField] private new ParticleSystem particleSystem;
    private AudioSource explosionAudioSource;
    private bool hit;
    private LocalPlayerInput localPlayerInput;

    private Projectile projectile;

    public void ShowEffects(bool shown) { }

    public void SetParticleSpeed(float speed) {
        ParticleSystem.MainModule main = particleSystem.main;
        main.simulationSpeed = speed;
    }

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        projectile = (Projectile)battleObject;
        spriteRenderer.enabled = true;
        hit = false;
        highlight.enabled = uIManager.GetEffectsShown();
        localPlayerInput = uIManager.localPlayer.GetInputManager();
        uIManager.uiBattleManager.objectsToUpdate.Add(this);
        uIManager.uiBattleManager.particleHolders.Add(this);
        ParticleSystem.MainModule main = particleSystem.main;
        main.simulationSpeed = uIManager.GetParticleSpeed();
        explosionAudioSource = gameObject.AddComponent<AudioSource>();
        explosionAudioSource.resource = projectile.explosionSound;
        explosionAudioSource.playOnAwake = false;
        explosionAudioSource.spatialBlend = 1;
        explosionAudioSource.rolloffMode = AudioRolloffMode.Linear;
        explosionAudioSource.minDistance = 20;
        explosionAudioSource.maxDistance = 120;
        explosionAudioSource.pitch = 1.4f;
        explosionAudioSource.dopplerLevel = 0;
        explosionAudioSource.volume = .2f;
    }

    public override void UpdateObject() {
        if (!projectile.spawned) {
            spriteRenderer.enabled = false;
            highlight.enabled = false;
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            uIManager.uiBattleManager.objectsToUpdate.Remove(this);
        }
        base.UpdateObject();
        if (projectile.hit && !hit && localPlayerInput.ShouldShowCloseUpGraphics() &&
            localPlayerInput.IsObjectInViewingField(this, 120)) {
            hit = true;
            if (uIManager.GetParticlesShown()) particleSystem.Play();
            highlight.enabled = false;
            explosionAudioSource.Play();
        }
        if (hit) {
            float cameraZoom = uIManager.localPlayer.GetLocalPlayerInput().mainCamera.orthographicSize;
            explosionAudioSource.volume = (float)math.max(0, math.min(1, math.pow(200 / cameraZoom, .15) - 1)) * .2f;
            explosionAudioSource.minDistance = 5 + 5 * cameraZoom / 10;
            explosionAudioSource.maxDistance = 30 + 5 * cameraZoom / 10;
        }
    }

    public override void OnBattleObjectRemoved() {
        base.OnBattleObjectRemoved();
        uIManager.uiBattleManager.particleHolders.Remove(this);
        highlight.enabled = false;
        particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        spriteRenderer.enabled = false;
        explosionAudioSource.Stop();
    }

    public void ShowParticles(bool shown) { }
}
