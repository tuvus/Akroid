using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/Thruster", menuName = "Components/Thruster", order = 3)]
class ThrusterScriptableObject : ScriptableObject {
    public float thrustSpeed;
    public Color color;
    public Color startThrustColor;
    public Color endThrustColor;
}
