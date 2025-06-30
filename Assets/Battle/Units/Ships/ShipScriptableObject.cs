using System.Linq;
using UnityEngine;
using static Ship;

[CreateAssetMenu(fileName = "Resources/Units/Ships/Ship", menuName = "Units/Ship", order = 1)]
public class ShipScriptableObject : UnitScriptableObject {
    public ShipClass shipClass;
    public ShipType shipType;
    public float turnSpeed;
    public float combatRotation;
    [Tooltip("Not used in game, this exists purely to visualise the ship's mass")]
    public float baseMass;
    [Tooltip("Not used in game, this exists purely to visualise the ship's speed")]
    public float baseSpeed;

    public override void OnValidate() {
        base.OnValidate();
        float thrustSpeed = systems.Where(s => s.component != null && s.component is ThrusterScriptableObject)
            .Sum(s => ((ThrusterScriptableObject)s.component).thrustSpeed * s.moduleCount);
        baseMass = Calculator.GetSpriteSizeFromBounds(spriteBounds, baseScale) * 100;
        baseSpeed = thrustSpeed / baseMass;
    }

}
