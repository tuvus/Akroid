using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Objects/Star", menuName = "Objects/Star", order = 3)]
public class StarScriptableObject : ScriptableObject {
    public Sprite sprite;
    [field: SerializeField] public Vector2 spriteBounds { get; private set; }
    public GameObject prefab;

    public void Awake() {
        if (prefab == null) prefab = Resources.Load<GameObject>("Prefabs/Star");
    }

    public void OnValidate() {
        if (sprite != null) {
            spriteBounds = Calculator.GetSpriteBounds(sprite);
        }
    }
}
