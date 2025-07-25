using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Resources/Objects/GasCloud", menuName = "Objects/GasCloud", order = 5)]
public class GasCloudScriptableObject : ScriptableObject {
    public Sprite sprite;
     public CargoBay.CargoType type;
    [field: SerializeField] public Vector2 spriteBounds { get; private set; }
    public GameObject prefab;

    public void Awake() {
        if (prefab == null) prefab = Resources.Load<GameObject>("Prefabs/GasCloud");
    }

    public void OnValidate() {
        if (sprite != null) {
            spriteBounds = Calculator.GetSpriteBounds(sprite);
        }
    }
}
