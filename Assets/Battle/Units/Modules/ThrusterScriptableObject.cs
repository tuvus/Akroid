using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/Thruster", menuName = "Components/Thruster", order = 3)]
class ThrusterScriptableObject : ComponentScriptableObject {
    public float thrustSpeed;
    public Color color;
    public Color startThrustColor;
    public Color endThrustColor;
    public GameObject thrustEffect;

    public override Type GetComponentType() {
        return typeof(Thruster);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(thrustSpeed / 20);
        AddResourceCost(CargoBay.CargoTypes.Metal, (long)(thrustSpeed / 100));
    }
}
