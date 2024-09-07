
/// <summary>
/// Represents a component that is assosiated with a module and provides some sort of functionality based on the underlying class.
/// </summary>
public abstract class ModuleComponent : BattleObject {
    public Module module;
    protected Unit unit { get; private set; }
    public ComponentScriptableObject componentScriptableObject { get; private set; }

    public ModuleComponent(BattleManager battleManager, Module module, Unit unit, ComponentScriptableObject componentScriptableObject): 
        base(new BattleObjectData(componentScriptableObject.name, new BattleManager.PositionGiver(module.transform.position), module.rotation, unit.faction), battleManager) {
        this.module = module;
    }
}
