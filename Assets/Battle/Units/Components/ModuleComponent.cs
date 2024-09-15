/// <summary>
/// Represents a component that is assosiated with a module and provides some sort of functionality based on the underlying class.
/// </summary>
public abstract class ModuleComponent : BattleObject {
    public IModule module;
    protected Unit unit { get; private set; }
    public ComponentScriptableObject componentScriptableObject { get; private set; }

    public ModuleComponent(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
        base(new BattleObjectData(componentScriptableObject.name, module.GetPosition(),
            module.GetRotation(), unit.faction), battleManager) {
        this.module = module;
        this.componentScriptableObject = componentScriptableObject;
    }
}
