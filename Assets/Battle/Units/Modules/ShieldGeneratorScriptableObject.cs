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

    public override Type GetComponentType() {
        return typeof(ShieldGenerator);
    }
}
