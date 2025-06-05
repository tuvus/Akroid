using UnityEngine;

public class Thruster : ModuleComponent {
    public Thruster(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        thrusterScriptableObject = (ThrusterScriptableObject)componentScriptableObject;
        visible = true;
    }
    public ThrusterScriptableObject thrusterScriptableObject { get; }

    public float GetThrust() {
        return thrusterScriptableObject.thrustSpeed;
    }

    public override GameObject GetPrefab() {
        return thrusterScriptableObject.thrustEffect;
    }
}
