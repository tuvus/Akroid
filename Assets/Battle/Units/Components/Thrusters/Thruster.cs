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
        if (CanThrust() && thrusting == false) {
            thrusting = true;
            particle.Play();
        }
    }

    public void UpdateThruster() {
        //if (thrusting && CanThrust())
        //	Thrust();
        //else if (!CanThrust())
        //	EndThrust();
    }

    public bool CanThrust() {
        return true;
    }
    public void Thrust() {
        //if (ship.UseFuel(fuelConsumption * Time.deltaTime, fuelType) <= 0)
        //	rb.AddForce(transform.up * thrustSpeed * Time.deltaTime * 10);
    }
    public void EndThrust() {
        thrusting = false;
        particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    public bool IsThrusting() {
        return thrusting;
    }
}
