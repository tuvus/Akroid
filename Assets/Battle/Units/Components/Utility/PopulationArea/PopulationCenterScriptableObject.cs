using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/PopulationCenterScriptableObject",
    menuName = "Components/PopulationCenter", order = 28)]
public class PopulationCenterScriptableObject : HabitationAreaScriptableObject {
    public override Type GetComponentType() {
        return typeof(PopulationCenter);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += populationSpace * 10;
        AddResourceCost(CargoBay.CargoType.Metal, populationSpace * 4);
        AddResourceCost(CargoBay.CargoType.Gas, populationSpace);
    }
}
