using UnityEngine;

public class Thruster : ModuleComponent {
    public ThrusterScriptableObject thrusterScriptableObject { get; private set; }

    public Thruster(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        thrusterScriptableObject = (ThrusterScriptableObject)componentScriptableObject;
    }

    public float GetThrust() {
        return thrusterScriptableObject.thrustSpeed;
    }

    public override GameObject GetPrefab() {
        return thrusterScriptableObject.thrustEffect;
    }
}
