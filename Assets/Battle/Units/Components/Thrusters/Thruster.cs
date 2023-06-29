using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thruster : MonoBehaviour, IParticleHolder {
    [SerializeField] ParticleSystem particle;
    [SerializeField] LensFlare thrusterFlare;
    [field: SerializeField] public float thrustSpeed { get; private set; }
    float targetBrightness;



    public void SetupThruster() {
        targetBrightness = thrusterFlare.brightness;
        thrusterFlare.enabled = BattleManager.Instance.GetEffectsShown();
        EndThrust();
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
}
