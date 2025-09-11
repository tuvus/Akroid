using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/BridgeScriptableObject", menuName = "Components/Bridge", order = 26)]
public class BridgeScriptableObject : HabitationAreaScriptableObject {
    public override Type GetComponentType() {
        return typeof(Bridge);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += populationSpace * 2;
        AddResourceCost(CargoBay.CargoType.Metal, populationSpace);
    }

    public override ModuleSystem.SystemType GetSystemType() {
        return ModuleSystem.SystemType.Bridge;
    }
}
