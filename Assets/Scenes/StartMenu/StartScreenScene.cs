using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartScreenScene : MonoBehaviour {
    [SerializeField] Transform station;
    [SerializeField] Transform planet;

    private void Start() {
        station.Rotate(new Vector3(0, 0, Random.Range(0, 360)));
        planet.Rotate(new Vector3(0, 0, Random.Range(0, 360)));
    }

    void Update() {
        station.Rotate(new Vector3(0, 0, Time.deltaTime * 5));
        planet.Rotate(new Vector3(0, 0, Time.deltaTime / 5));
    }
}
