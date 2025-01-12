using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Objects/Star", menuName = "Objects/Star", order = 3)]
public class StarScriptableObject : ScriptableObject {
    public Sprite sprite;
    public Vector2 spriteBounds { get; private set; }

    public void OnValidate() {
        if (sprite != null) {
            spriteBounds = Calculator.GetSpriteBounds(sprite);
        }
    }
}
