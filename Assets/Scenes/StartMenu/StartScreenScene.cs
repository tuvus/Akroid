using UnityEngine;

public class StartScreenScene : MonoBehaviour {
    [SerializeField] private Transform station;
    [SerializeField] private Transform planet;

    private void Start() {
        station.Rotate(new Vector3(0, 0, Random.Range(0, 360)));
        planet.Rotate(new Vector3(0, 0, Random.Range(0, 360)));
    }

    private void Update() {
        station.Rotate(new Vector3(0, 0, Time.deltaTime * 5));
        planet.Rotate(new Vector3(0, 0, Time.deltaTime / 5));
    }
}
