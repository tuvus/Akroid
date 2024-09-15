using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public System(System system) {
            name = system.name;
            type = system.type;
            moduleCount = system.moduleCount;
            moduleSize = system.moduleSize;
            this.component = system.component;
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
    [field: SerializeField] public List<System> systems { get; private set; }
    [field: SerializeField] public List<ModuleComponent> modules { get; private set; }
    public Dictionary<ModuleComponent, System> moduleToSystem { get; private set; }

    public ModuleSystem(BattleManager battleManager, Unit unit, UnitScriptableObject unitScriptableObject) {
        this.unit = unit;
        var systemComponents = unitScriptableObject.GetSystems();
        var prefabModules = unitScriptableObject.GetModules();
        systems = new List<System>(systemComponents.Count);
        modules = new List<ModuleComponent>();
        moduleToSystem = new Dictionary<ModuleComponent, System>();
        foreach (var system in systemComponents) {
            if (system == null) {
                Debug.Log($"{unit.GetUnitName()} has a null component at {systems.Count}");
                continue;
            }

            System newSystem = new System(system);
            systems.Add(newSystem);
            for (int i = 0; i < newSystem.moduleCount; i++) {
                IModule module = prefabModules[modules.Count()];
                var args = new object[] {battleManager, module, unit, newSystem.component};
                ModuleComponent newComponent = (ModuleComponent)Activator.CreateInstance(newSystem.component.GetComponentType(), args);
                modules.Add(newComponent);
                moduleToSystem.Add(newComponent, newSystem);
            }
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
        if (!CanUpgradeSystem(systemIndex, upgrader)) return;

        //Pay for the upgrade cost
        upgrader.faction.UseCredits((upgrade.cost - current.cost) * system.moduleCount);
        for (int i = 0; i < upgrade.resourceTypes.Count; i++) {
            upgrader.UseCargo(upgrade.resourceCosts[i] - current.resourceCosts[i] / 2, upgrade.resourceTypes[i]);
        }
        //Upgrade the system
        systems[systemIndex].component = upgrade;

        //Upgrade the moduleComponents
        for (int i = 0; i < modules.Count(); i++) {
            ModuleComponent oldModule = modules[i];
            if (moduleToSystem[oldModule] == system) {
                var args = new object[] {unit.battleManager, oldModule.module, unit, upgrade};
                modules[i] = (ModuleComponent)Activator.CreateInstance(upgrade.GetComponentType(), args);
                moduleToSystem.Remove(oldModule);
                moduleToSystem.Add(modules[i], system);
            }
        }
    }
    #endregion
}
