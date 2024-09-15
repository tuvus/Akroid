using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/ResearchEquipmentScriptableObject", menuName = "Components/ResearchEquipment",
    order = 29)]
public class ResearchEquipmentScriptableObject : ComponentScriptableObject {
    public int maxData;
    public int researchAmount;
    public float researchSpeed;

    public override Type GetComponentType() {
        return typeof(ResearchEquipment);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(researchAmount / researchSpeed * 30);
        AddResourceCost(CargoBay.CargoTypes.Metal, maxData);
    }
}
