using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Objects/Planet", menuName = "Objects/Planet", order = 2)]
public class PlanetScriptableObject : ScriptableObject {
    public Sprite sprite;
    public bool hasAtmosphere;
}
