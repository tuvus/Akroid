using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/HangerScriptableObject", menuName = "Components/Hanger", order = 27)]
public class HangerScriptableObject : ComponentScriptableObject {
    public int maxDockSpace;

    public override Type GetComponentType() {
        return typeof(Hanger);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += maxDockSpace * 10;
        AddResourceCost(CargoBay.CargoTypes.Metal, maxDockSpace * 30);
    }
}
