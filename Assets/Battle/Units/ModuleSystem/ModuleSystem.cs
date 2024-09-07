using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a list of systems for a unit during runtime.
/// Each system is holds a component and a list of modules which will use that component.
/// </summary>
[Serializable]
public class ModuleSystem {
    [Serializable]
    public class System {
        public string name;
        public PrefabModuleSystem.SystemType type;
        public ComponentScriptableObject component;
        public int moduleSize;
        public int moduleCount { get; private set; }

        public System(string name, PrefabModuleSystem.SystemType type) {
            this.name = name;
            this.type = type;
            moduleCount = 0;
            moduleSize = 0;
            component = null;
        }

        public System(System system, ComponentScriptableObject component) {
            name = system.name;
            type = system.type;
            moduleCount = system.moduleCount;
            moduleSize = system.moduleSize;
            this.component = component;
        }

        public System(PrefabModuleSystem.PrefabSystem prefabSystem, ComponentScriptableObject component) {
            name = prefabSystem.name;
            type = prefabSystem.type;
            moduleCount = prefabSystem.moduleCount;
            moduleSize = prefabSystem.moduleSize;
            this.component = component;
        }
    }

    private Unit unit;
    [field: SerializeField] public List<System> systems { get; private set; } = new List<System>();
    [field: SerializeField] public List<Module> modules { get; private set; } = new List<Module>();

    public ModuleSystem(BattleManager battleManager, Unit unit, UnitScriptableObject unitScriptableObject) {
        this.unit = unit;
        List<ComponentScriptableObject> systemComponents = unitScriptableObject.GetSystemComponents();
        for (int i = 0; i < systems.Count; i++) {
            systems[i] = new System(systems[i], systemComponents[i]);
        }
        unitScriptableObject.ApplyComponentsToSystems(systems);

        foreach (var module in modules) {
            if (systems[module.system].component == null) continue;
            // ModuleComponent newComponent = modules[i].gameObject.AddComponent(systems[module.system].component.GetComponentType()).GetComponent<ModuleComponent>();
            ModuleComponent newComponent = (ModuleComponent)Activator.CreateInstance(systems[module.system].component.GetComponentType(), new List<object>(){battleManager, module, systems[module.system].component});
            if (newComponent == null) {
                Debug.Log(systems[module.system].component.GetComponentType());
            }
            // newComponent.SetupComponent(battleManager, module, unit, systems[module.system].component);
        }

    }

    #region SystemUpgrades
    public ComponentScriptableObject GetSystemUpgrade(int system) => systems[system].component.upgrade;

    public bool CanUpgradeSystem(int systemIndex, Unit upgrader) {
        System system = systems[systemIndex];
        ComponentScriptableObject current = system.component;
        ComponentScriptableObject upgrade = current.upgrade;
        if (upgrade == null) return false;
        if (upgrader.faction.credits >= (upgrade.cost - current.cost) * system.moduleCount) {
            for (int i = 0; i < upgrade.resourceTypes.Count; i++) {
                long currentAmount = 0;
                int currentTypeIndex = current.resourceTypes.IndexOf(upgrade.resourceTypes[i]);
                if (currentTypeIndex >= 0) currentAmount = current.resourceCosts[currentTypeIndex];
                if (upgrader.GetAllCargoOfType(upgrade.resourceTypes[i]) < upgrade.resourceCosts[i] - currentAmount) {
                    return false;
                }
            }
        }
        return true;
    }

    public void UpgradeSystem(int systemIndex, Unit upgrader) {
        System system = systems[systemIndex];
        ComponentScriptableObject current = system.component;
        ComponentScriptableObject upgrade = current.upgrade;
        if (CanUpgradeSystem(systemIndex, upgrader)) {
            //Pay for the upgrade cost
            upgrader.faction.UseCredits((upgrade.cost - current.cost) * system.moduleCount);
            for (int i = 0; i < upgrade.resourceTypes.Count; i++) {
                long currentAmount = 0;
                int currentTypeIndex = current.resourceTypes.IndexOf(upgrade.resourceTypes[i]);
                upgrader.UseCargo(upgrade.resourceCosts[i] - currentAmount, upgrade.resourceTypes[i]);
            }
            //Upgrade the moduleComponents
            systems[systemIndex] = new System(system, system.component.upgrade);
            for (int i = 0; i < modules.Count; i++) {
                if (modules[i].system == systemIndex) {
                    modules[i].moduleComponent = (ModuleComponent)Activator.CreateInstance(
                        systems[modules[systemIndex].system].component.GetComponentType(),
                        new List<object>() {
                            upgrader.battleManager, modules[systemIndex], systems[modules[systemIndex].system].component
                        }
                    );
                }
            }
        }
    }
    #endregion
}
