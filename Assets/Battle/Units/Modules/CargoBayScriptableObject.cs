using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/CargoBayScriptableObject", menuName = "Components/CargoBay", order = 1)]
public class CargoBayScriptableObject : ComponentScriptableObject {
    public int maxCargoBays;
    public long cargoBaySize;

    public override Type GetComponentType() {
        return typeof(CargoBay);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += maxCargoBays * cargoBaySize / 50;
        AddResourceCost(CargoBay.CargoTypes.Metal, maxCargoBays * cargoBaySize / 20);
    }
}
