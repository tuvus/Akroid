using Unity.Mathematics;
using UnityEngine;

public class ThrusterUI : ComponentUI, IParticleHolder {
    private AudioSource audioSource;
    private LocalPlayerInput localPlayerInput;
    private ParticleSystem particle;
    private ShipUI shipUI;
    private Thruster thruster;
    private LensFlare thrusterFlare;
    private bool thrusting;
    private float volumeDropoff;

    public void ShowEffects(bool shown) {
        thrusterFlare.enabled = shown;
    }

    public void SetParticleSpeed(float speed) {
        ParticleSystem.MainModule main = particle.main;
        main.simulationSpeed = speed;
    }

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI, int componentIndex) {
        base.Setup(battleObject, uIManager, unitUI, componentIndex);
        thruster = (Thruster)battleObject;
        shipUI = (ShipUI)unitUI;
        Instantiate(thruster.GetPrefab(), transform);
        particle = transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particle.main;
        main.simulationSpeed = uIManager.GetParticleSpeed();
        thrusterFlare = transform.GetChild(0).GetChild(1).GetComponent<LensFlare>();
        thrusterFlare.enabled = false;
        localPlayerInput = uIManager.localPlayer.GetInputManager();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.resource = thruster.thrusterScriptableObject.thrustSound;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0;
        audioSource.pitch = .2f;
        audioSource.loop = true;
        thrusting = false;
        uIManager.uiBattleManager.particleHolders.Add(this);
    }

    public override void RemoveComponent() {
        DestroyImmediate(gameObject.GetComponent<AudioSource>());
        DestroyImmediate(transform.GetChild(0).gameObject);
        spriteRenderer.enabled = false;
    }

    public override void UpdateObject() {
        base.UpdateObject();

        // Handle starting and stopping thrusting
        if (IsVisible() && shipUI.ship.thrusting && !thrusting) {
            thrusting = true;
            audioSource.Play();
        } else if (thrusting && (!IsVisible() || !shipUI.ship.thrusting)) {
            thrusting = false;
            EndThrust();
        }

        if (thrusting) {
            // Only show the thrust effects if the ship is being looked at
            // This is called every time when thrusting
            if (uIManager.GetEffectsShown() && localPlayerInput.ShouldShowCloseUpGraphics() &&
                localPlayerInput.IsObjectInViewingField(shipUI)) {
                BeginThrust();
                thrusterFlare.enabled = true;
                thrusterFlare.brightness = GetFlareBrightness() * shipUI.ship.thrustSize;
            } else {
                EndThrust();
                thrusterFlare.enabled = false;
            }
        }

        if (thrusting || volumeDropoff > 0) {
            float cameraZoom = localPlayerInput.mainCamera.orthographicSize;
            audioSource.volume = (float)math.max(0, math.min(1, math.pow(200 / cameraZoom, .15) - 1)) * .2f *
                uIManager.playerUI.soundEffectsVolume;
            audioSource.minDistance = 1 + 5 * cameraZoom / 10;
            audioSource.maxDistance = 15 + 5 * cameraZoom / 10;
            if (thrusting)
                volumeDropoff = math.min(1, volumeDropoff + Time.deltaTime * thruster.battleManager.timeScale * 3);
            else volumeDropoff = math.max(0, volumeDropoff - Time.deltaTime * thruster.battleManager.timeScale * 3);
            audioSource.volume *= volumeDropoff;
            if (volumeDropoff == 0)
                audioSource.Stop();
        }
    }

    public override void OnUnitDestroyed() {
        EndThrust();
    }

    public override void OnUnitRemoved() {
        uIManager.uiBattleManager.particleHolders.Remove(this);
    }

    public void BeginThrust() {
        if (uIManager.GetParticlesShown() && !particle.isPlaying) particle.Play();
        else if (!uIManager.GetParticlesShown() && particle.isPlaying)
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void EndThrust() {
        thrusterFlare.enabled = false;
        if (particle.isPlaying) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private float GetFlareBrightness() {
        return unitUI.unit.GetSpriteSize() * 5;
    }
}
