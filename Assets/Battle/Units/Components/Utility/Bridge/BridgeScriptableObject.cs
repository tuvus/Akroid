using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/BridgeAreaScriptableObject",
    menuName = "Components/BridgeArea", order = 26)]
public class BridgeScriptableObject : ComponentScriptableObject {
    public long populationSpace;
    public long minCrew;

    public override Type GetComponentType() {
        return typeof(BridgeScriptableObject);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += populationSpace;
        AddResourceCost(CargoBay.CargoTypes.Metal, populationSpace / 10);
    }
}
