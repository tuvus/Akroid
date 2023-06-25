using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/Thruster", menuName = "Components/Thruster", order = 3)]
class ThrusterScriptableObject : ComponentScriptableObject {
    public float thrustSpeed;
    public Color color;
    public Color startThrustColor;
    public Color endThrustColor;

    public override Type GetComponentType() {
        return typeof(Thruster);
    }
}
