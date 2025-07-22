using System;
using System.ComponentModel;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/MiningBayScriptableObject",
    menuName = "Components/MiningBay", order = 35)]
public class MiningBayScriptableObject : ComponentScriptableObject {

    public long miningAmount;
    public float miningSpeed;
    public int miningRange;

    public long engineersRequired;

    public override Type GetComponentType() {
        return typeof(MiningBay);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(miningAmount * miningRange / miningSpeed / 10 );
        AddResourceCost(CargoBay.CargoType.Metal, (long)(miningAmount * 3 / miningSpeed));
    }

    [ContextMenu("SetSuggestedEngineers")]
    void SetSuggestedEngineersRequired() {
        engineersRequired = (long)(miningAmount * .8f / miningSpeed);
    }
}
