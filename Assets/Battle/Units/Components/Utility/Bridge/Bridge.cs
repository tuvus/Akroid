using UnityEngine;

public class Bridge : ModuleComponent {
    public BridgeScriptableObject bridgeScriptableObject;
    public Population population { get; private set; }

    public Bridge(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        bridgeScriptableObject = (BridgeScriptableObject)componentScriptableObject;
        population = new Population();
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        bridgeScriptableObject = (BridgeScriptableObject)componentScriptableObject;
    }
}
