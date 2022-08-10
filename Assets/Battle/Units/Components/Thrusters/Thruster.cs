using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thruster : MonoBehaviour {
    ParticleSystem particle;
    Rigidbody2D rb;
    Ship ship;

    public float thrustSpeed;

    bool thrusting = false;
    public void SetupThruster() {
        particle = GetComponent<ParticleSystem>();
        ship = GetComponentInParent<Ship>();
        rb = ship.GetComponent<Rigidbody2D>();
        EndThrust();
    }

    public void BeginThrust() {
        if (thrusting == false) {
            thrusting = true;
            particle.Play();
        }
    }

    public void EndThrust() {
        thrusting = false;
        particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    public bool IsThrusting() {
        return thrusting;
    }
}
