using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Objects/Asteroid", menuName = "Objects/Asteroid", order = 4)]
public class AsteroidScriptableObject : ScriptableObject {
    public Sprite sprite;
    public CargoBay.CargoTypes type;
}
