using UnityEngine;

public class Thruster : ModuleComponent, IParticleHolder {
    ThrusterScriptableObject thrusterScriptableObject;
    [SerializeField] ParticleSystem particle;
    [SerializeField] LensFlare thrusterFlare;
    float targetBrightness;
    float baseThrustEmissionRate;

    // public Thruster(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
    //     base(battleManager, module, unit, componentScriptableObject) {
    //     this.module = module;
    //     this.unit = unit;
    //     this.componentScriptableObject = componentScriptableObject;
    // }
    public Thruster(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        thrusterScriptableObject = (ThrusterScriptableObject)componentScriptableObject;

        // baseThrustEmissionRate = particle.emission.rateOverTime.constant;

        // thrusterFlare.brightness = 5 * unit.GetSpriteSize();
        // targetBrightness = thrusterFlare.brightness;
        // thrusterFlare.enabled = BattleManager.Instance.GetEffectsShown();
        // EndThrust();
    }

    public void BeginThrust() {
        if (BattleManager.Instance.GetParticlesShown())
            particle.Play();
        thrusterFlare.brightness = targetBrightness;
    }

    public void EndThrust() {
        thrusterFlare.brightness = 0;
        particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    public void ShowEffects(bool shown) {
        thrusterFlare.enabled = shown;
    }

    /// <summary>
    /// Sets the size of the thruster flare
    /// </summary>
    /// <param name="modifier">A value between 0 and 1</param>
    public void SetThrustSize(float modifier) {
        thrusterFlare.brightness = targetBrightness * modifier;
        var emission = particle.emission;
        emission.rateOverTime = baseThrustEmissionRate * modifier;
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

    public float GetThrust() {
        return thrusterScriptableObject.thrustSpeed;
    }
}
