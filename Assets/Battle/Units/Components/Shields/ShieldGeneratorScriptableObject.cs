using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/ShieldGenerator", menuName = "Components/ShieldGenerator", order = 2)]
class ShieldGeneratorScriptableObject : ComponentScriptableObject {
    //ShieldGenStats
    public float shieldRegenRate;
    public float shieldRecreateSpeed;

    public int shieldRegenHealth;

    //ShieldStats
    public Shield shieldPrefab;
    public int maxShieldHealth;

    public void Awake() {
        if (shieldPrefab == null)
            shieldPrefab = Resources.Load<GameObject>("Prefabs/Shield").GetComponent<Shield>();
    }

    public override Type GetComponentType() {
        return typeof(ShieldGenerator);
    }

    protected override void UpdateCosts() {
        base.UpdateCosts();
        cost += (long)(shieldRegenHealth / shieldRegenRate * 14 + maxShieldHealth);
        AddResourceCost(CargoBay.CargoTypes.Metal, maxShieldHealth / 2);
    }
}
