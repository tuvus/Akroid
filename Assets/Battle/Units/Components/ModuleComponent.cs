using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleComponent : BattleObject {
    public Module module;
    protected Unit unit { get; private set; }
    public ComponentScriptableObject ComponentScriptableObject { get; private set; }
    
    public virtual void SetupComponent(Module module, Unit unit, ComponentScriptableObject componentScriptableObject) {
        base.SetupBattleObject(unit.battleManager);
        module.moduleComponent = this;
        this.module = module;
        this.unit = unit;
        this.faction = unit.faction;
        this.ComponentScriptableObject = componentScriptableObject;
    }
}
