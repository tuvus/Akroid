using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Resources/Components/GeneratorScriptableObject", menuName = "Components/Generator",
    order = 28)]
public class GeneratorScriptableObject : ComponentScriptableObject {
    public float consumptionSpeed;
    public long consumptionAmount;
    public long energyGain;
    [FormerlySerializedAs("consumptionType")] public CargoBay.CargoType consumptionType;

    public override Type GetComponentType() {
        return typeof(Generator);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(85 * energyGain / (consumptionAmount * consumptionSpeed));
        AddResourceCost(CargoBay.CargoType.Metal, consumptionAmount * 5);
    }
}
