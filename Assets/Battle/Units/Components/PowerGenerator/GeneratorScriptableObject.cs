using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/GeneratorScriptableObject", menuName = "Components/Generator", order = 28)]
public class GeneratorScriptableObject : ComponentScriptableObject {
    public float consumptionSpeed;
    public long consumptionAmount;
    public long energyGain;
    public CargoBay.CargoTypes consumptionType;

    public override Type GetComponentType() {
        return typeof(Generator);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(85 * energyGain / (consumptionAmount * consumptionSpeed));
        AddResourceCost(CargoBay.CargoTypes.Metal, consumptionAmount * 5);
    }
}
