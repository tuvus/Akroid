public class Bridge : HabitationArea {
    public BridgeScriptableObject bridgeScriptableObject;

    public Bridge(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        bridgeScriptableObject = (BridgeScriptableObject)componentScriptableObject;
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        bridgeScriptableObject = (BridgeScriptableObject)componentScriptableObject;
    }
}
