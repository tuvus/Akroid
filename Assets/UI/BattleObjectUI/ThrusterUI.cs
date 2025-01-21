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
        thrusterFlare = transform.GetChild(0).GetChild(1).GetComponent<LensFlare>();
        thrusterFlare.enabled = false;
        localPlayerInput = uIManager.localPlayer.GetInputManager();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.resource = Resources.Load<AudioResource>("Audio/Engine");
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 5;
        audioSource.maxDistance = 50;
        audioSource.dopplerLevel = 0;
        audioSource.volume = 1f;
        audioSource.loop = true;
        thrusting = false;
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
            if (localPlayerInput.ShouldShowCloseUpGraphics() && localPlayerInput.IsObjectInViewingField(shipUI)) {
                BeginThrust();
                thrusterFlare.enabled = true;
            } else if (!localPlayerInput.ShouldShowCloseUpGraphics() || !localPlayerInput.IsObjectInViewingField(shipUI)) {
                EndThrust();
                thrusterFlare.enabled = false;
            }

            if (thrusterFlare.enabled) thrusterFlare.brightness = GetFlareBrightness() * shipUI.ship.thrustSize;
        }
    }

    public override void OnUnitDestroyed() {
        EndThrust();
    }

    public void BeginThrust() {
        if (uIManager.GetParticlesShown() && !particle.isPlaying) particle.Play();
        else if (!uIManager.GetParticlesShown() && particle.isPlaying) particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    public void EndThrust() {
        thrusterFlare.enabled = false;
        if (particle.isPlaying) particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    public void ShowEffects(bool shown) {
        thrusterFlare.enabled = shown;
    }

    public void SetParticleSpeed(float speed) {
        var main = particle.main;
        main.simulationSpeed = speed;
    }

    /// <summary>
    /// If shown == false, stops emitting
    /// If shown == true, assumes the ship is thrusting and begins emitting.
    /// </summary>
    /// <param name="shown"></param>
    public void ShowParticles(bool shown) {
        if (shown) {
            particle.Play();
        } else {
            particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private float GetFlareBrightness() {
        return unitUI.unit.GetSpriteSize() * 5;
    }
}
