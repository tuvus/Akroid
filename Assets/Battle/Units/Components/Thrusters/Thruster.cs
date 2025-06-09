using UnityEngine;

public class Thruster : ModuleComponent {
    public ThrusterScriptableObject thrusterScriptableObject { get; private set; }

    public Thruster(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        thrusterScriptableObject = (ThrusterScriptableObject)componentScriptableObject;
        visible = true;
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        thrusterScriptableObject = (ThrusterScriptableObject)componentScriptableObject;
        if (unit.IsShip()) ((Ship)unit).RecalculateThrust();
    }

    public float GetThrust() {
        return thrusterScriptableObject.thrustSpeed;
    }

    public override GameObject GetPrefab() {
        return thrusterScriptableObject.thrustEffect;
    }
}
