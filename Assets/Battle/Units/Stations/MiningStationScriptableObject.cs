using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Units/Stations/MiningStation", menuName = "Units/MiningStation", order = 2)]
public class MiningStationScriptableObject : StationScriptableObject {
    public int miningRange;

    public override void OnValidate() {
        base.OnValidate();
        miningRange = 0;
        for (var i = 0; i < systems.Length; i++) {
            if (systems[i].component != null && systems[i].component is MiningBayScriptableObject miningBayScriptableObject) {
                miningRange = math.max(miningRange, miningBayScriptableObject.miningRange);
            }
        }
    }
}
