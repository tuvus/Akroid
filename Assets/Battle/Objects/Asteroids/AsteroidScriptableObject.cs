using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Objects/Asteroid", menuName = "Objects/Asteroid", order = 4)]
public class AsteroidScriptableObject : ScriptableObject {
    public Sprite sprite;
    public CargoBay.CargoTypes type;
    [field: SerializeField] public Vector2 spriteBounds { get; private set; }
    public GameObject prefab;

    public void Awake() {
        if (prefab == null) prefab = Resources.Load<GameObject>("Prefabs/Asteroid");
    }
    public void OnValidate() {
        if (sprite != null) {
            spriteBounds = Calculator.GetSpriteBounds(sprite);
        }
    }
}
