using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/HangarScriptableObject", menuName = "Components/Hangar", order = 27)]
public class HangarScriptableObject : ComponentScriptableObject {
    public int maxDockSpace;

    public override Type GetComponentType() {
        return typeof(Hangar);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += maxDockSpace * 10;
        AddResourceCost(CargoBay.CargoType.Metal, maxDockSpace * 30);
    }

    public override ModuleSystem.SystemType GetSystemType() {
        return ModuleSystem.SystemType.Utility;
    }
}
