using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Objects/GasCloud", menuName = "Objects/GasCloud", order = 5)]
public class GasCloudScriptableObject : ScriptableObject {
    public Sprite sprite;
    public CargoBay.CargoTypes type;
    public Vector2 spriteBounds { get; private set; }

    public void OnValidate() {
        if (sprite != null) {
            spriteBounds = Calculator.GetSpriteBounds(sprite);
        }
    }
}
