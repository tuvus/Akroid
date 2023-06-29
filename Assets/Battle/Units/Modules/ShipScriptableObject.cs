using UnityEngine;
using static Ship;

[CreateAssetMenu(fileName = "Resources/Units/Ship/Ship", menuName = "Units/Ship", order = 1)]
public class ShipScriptableObject : UnitScriptableObject {
    public ShipClass shipClass;
    public ShipType shipType;
    public float turnSpeed;
    public float combatRotation;
}
