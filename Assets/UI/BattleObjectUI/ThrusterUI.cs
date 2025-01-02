using UnityEngine;

public class ThrusterUI : ComponentUI, IParticleHolder {
    private Thruster thruster;
    private ShipUI shipUI;
    private ParticleSystem particle;
    private LensFlare thrusterFlare;

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager, unitUI);
        thruster = (Thruster)battleObject;
        shipUI = (ShipUI)unitUI;
        Instantiate(thruster.GetPrefab(), transform);
        particle = transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>();
        thrusterFlare = transform.GetChild(0).GetChild(1).GetComponent<LensFlare>();
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (shipUI.ship.thrusting && uIManager.GetEffectsShown() && uIManager.localPlayer.GetInputManager().IsObjectInViewingField(shipUI)) {
            BeginThrust();
            thrusterFlare.brightness = GetFlareBrightness() * shipUI.ship.thrustSize;
        } else if (particle.isPlaying) {
            EndThrust();
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
        thrusterFlare.brightness = 0;
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
