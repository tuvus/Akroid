using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModuleComponent : BattleObject {
    public Module module;
    public ComponentScriptableObject ComponentScriptableObject { get; private set; }
    public virtual void SetupComponent(Module module, Faction faction, ComponentScriptableObject componentScriptableObject) {
        module.moduleComponent = this;
        this.module = module;
        this.faction = faction;
        this.ComponentScriptableObject = componentScriptableObject;
    }
}
