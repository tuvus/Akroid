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
        particle.Play();
    }

    public void EndThrust() {
        particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }
}
