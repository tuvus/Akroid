using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartScreenScene : MonoBehaviour {
    [SerializeField] Station station;
    [SerializeField] Planet planet;

    void Update() {
        station.SetRotation(station.GetRotation() + Time.deltaTime * 5);
        planet.SetRotation(planet.GetRotation() + Time.deltaTime / 5);
    }
}
