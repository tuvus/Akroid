using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/BridgeScriptableObject", menuName = "Components/Bridge", order = 26)]
public class BridgeScriptableObject : ComponentScriptableObject {
    public long populationSpace;

    public override Type GetComponentType() {
        return typeof(BridgeScriptableObject);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += populationSpace * 10;
        AddResourceCost(CargoBay.CargoTypes.Metal, populationSpace);
    }
}
