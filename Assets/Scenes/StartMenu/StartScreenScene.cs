using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartScreenScene : MonoBehaviour {
    [SerializeField] Station station;
    [SerializeField] Planet planet;

    private void Start() {
        station.SetRotation(Random.Range(0, 360));
        planet.SetRotation(Random.Range(0, 360));
    }

    void Update() {
        station.SetRotation(station.rotation + Time.deltaTime * 5);
        planet.SetRotation(planet.rotation + Time.deltaTime / 5);
    }
}
