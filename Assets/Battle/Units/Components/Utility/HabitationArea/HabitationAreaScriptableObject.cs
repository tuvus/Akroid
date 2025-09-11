using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/HabitationAreaScriptableObject",
    menuName = "Components/HabitationArea", order = 28)]
public class HabitationAreaScriptableObject : ComponentScriptableObject {
    public long populationSpace;

    public override Type GetComponentType() {
        return typeof(HabitationArea);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += populationSpace * 3;
        AddResourceCost(CargoBay.CargoType.Metal, populationSpace);
    }

    public override ModuleSystem.SystemType GetSystemType() {
        return ModuleSystem.SystemType.Utility;
    }
}
