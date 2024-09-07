using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/GasCollectorScriptableObject", menuName = "Components/GasCollector", order = 30)]
public class GasCollectorScriptableObject : ComponentScriptableObject {
    public int collectionAmount;
    public float collectionSpeed;

    public override Type GetComponentType() {
        return typeof(GasCollector);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(collectionAmount / collectionSpeed * 2);
        AddResourceCost(CargoBay.CargoTypes.Metal, collectionAmount / 4);
    }
}
