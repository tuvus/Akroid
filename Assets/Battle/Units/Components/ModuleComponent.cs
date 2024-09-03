
/// <summary>
/// Represents a component that is assosiated with a module and provides some sort of functionality based on the underlying class.
/// </summary>
public abstract class ModuleComponent : BattleObject {
    public Module module;
    protected Unit unit { get; private set; }
    public ComponentScriptableObject componentScriptableObject { get; private set; }
    
    public virtual void SetupComponent(Module module, Unit unit, ComponentScriptableObject componentScriptableObject) {
        base.SetupBattleObject(unit.battleManager, unit.faction);
        module.moduleComponent = this;
        this.module = module;
        this.unit = unit;
        this.componentScriptableObject = componentScriptableObject;
    }
}
