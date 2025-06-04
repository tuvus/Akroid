using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Objects/Planet", menuName = "Objects/Planet", order = 2)]
public class PlanetScriptableObject : ScriptableObject {
    public Sprite sprite;
    public bool hasAtmosphere;
    [field:SerializeField] public Vector2 spriteBounds { get; private set; }
    public GameObject prefab;

    public void Awake() {
        if (prefab == null) prefab = Resources.Load<GameObject>("Prefabs/Planet");
    }
    public void OnValidate() {
        if (sprite != null) {
            spriteBounds = Calculator.GetSpriteBounds(sprite);
        }
    }
}
