using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thruster : MonoBehaviour {
    ParticleSystem particle;

    public float thrustSpeed;
    public void SetupThruster() {
        particle = GetComponent<ParticleSystem>();
        EndThrust();
    }

    public void BeginThrust() {
        if (BattleManager.Instance.GetParticlesShown())
            particle.Play();
    }

    public void EndThrust() {
        particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
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
