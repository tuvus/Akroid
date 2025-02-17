using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

public class ThrusterUI : ComponentUI, IParticleHolder {
    private Thruster thruster;
    private ShipUI shipUI;
    private ParticleSystem particle;
    private LensFlare thrusterFlare;
    private LocalPlayerInput localPlayerInput;
    private AudioSource audioSource;
    private bool thrusting;

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager, unitUI);
        thruster = (Thruster)battleObject;
        shipUI = (ShipUI)unitUI;
        Instantiate(thruster.GetPrefab(), transform);
        particle = transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>();
        var main = particle.main;
        main.simulationSpeed = uIManager.GetParticleSpeed();
        thrusterFlare = transform.GetChild(0).GetChild(1).GetComponent<LensFlare>();
        thrusterFlare.enabled = false;
        localPlayerInput = uIManager.localPlayer.GetInputManager();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.resource = Resources.Load<AudioResource>("Audio/Engine");
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0;
        audioSource.loop = true;
        thrusting = false;
        uIManager.uiBattleManager.particleHolders.Add(this);
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
            audioSource.Stop();
        }

        if (thrusting) {
            // Only show the thrust effects if the ship is being looked at
            // This is called every time when thrusting
            if (uIManager.GetEffectsShown() && localPlayerInput.ShouldShowCloseUpGraphics() && localPlayerInput.IsObjectInViewingField(shipUI)) {
                BeginThrust();
                thrusterFlare.enabled = true;
                thrusterFlare.brightness = GetFlareBrightness() * shipUI.ship.thrustSize;
            } else {
                EndThrust();
                thrusterFlare.enabled = false;
            }

            float cameraZoom = localPlayerInput.mainCamera.orthographicSize;
            audioSource.volume = (float)math.max(0, math.min(1, math.pow(600 / cameraZoom, .15) - 1)) * .2f;
            audioSource.minDistance = 1 + 5 * cameraZoom / 10;
            audioSource.maxDistance = 15 + 5 * cameraZoom / 10;
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
        else if (!uIManager.GetParticlesShown() && particle.isPlaying) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void EndThrust() {
        thrusterFlare.enabled = false;
        if (particle.isPlaying) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void ShowEffects(bool shown) {
        thrusterFlare.enabled = shown;
    }

    public void SetParticleSpeed(float speed) {
        var main = particle.main;
        main.simulationSpeed = speed;
    }

    private float GetFlareBrightness() {
        return unitUI.unit.GetSpriteSize() * 5;
    }
}
