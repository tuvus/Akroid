using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/ConstructionBayScriptableObject", menuName = "Components/ConstructionBay", order = 1)]
public class ConstructionBayScriptableObject : ComponentScriptableObject {
    public float constructionSpeed;
    public long constructionAmount;
    public int constructionBays;
    public override Type GetComponentType() {
        return typeof(ConstructionBay);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(constructionBays / constructionSpeed * constructionAmount * 10);
        AddResourceCost(CargoBay.CargoTypes.Metal, constructionBays * 300);
    }
}
