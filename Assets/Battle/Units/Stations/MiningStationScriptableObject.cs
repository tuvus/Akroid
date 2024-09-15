using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Units/Stations/MiningStation", menuName = "Units/MiningStation", order = 2)]
public class MiningStationScriptableObject : StationScriptableObject {
    public int miningAmount;
    public float miningSpeed;
    public int miningRange;
}
