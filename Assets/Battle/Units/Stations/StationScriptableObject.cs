using UnityEngine;
using static Station;

[CreateAssetMenu(fileName = "Resources/Units/Stations/Station", menuName = "Units/Station", order = 2)]
public class StationScriptableObject : UnitScriptableObject {
    public StationType stationType;
    public int repairAmount;
    public float repairSpeed;
    public float rotationSpeed;
}