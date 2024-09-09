using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/Thruster", menuName = "Components/Thruster", order = 3)]
public class ThrusterScriptableObject : ComponentScriptableObject {
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
        cost += (long)(thrustSpeed / 10);
        AddResourceCost(CargoBay.CargoTypes.Metal, (long)(thrustSpeed / 50));
        AddResourceCost(CargoBay.CargoTypes.Gas, (long)(thrustSpeed / 50));
    }
}
