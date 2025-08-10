using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///     Manages a list of systems for a unit during runtime.
///     Each system is holds a component and a list of modules which will use that component.
/// </summary>
[Serializable]
public class ModuleSystem {
    public enum SystemType {
        Any,
        Utility,
        Weapon,
        Turret,
        Thruster,
        Bridge
    }

    [field: SerializeField] public List<System> systems { get; private set; }

    private Unit unit;

    [field: SerializeField] public List<ModuleComponent> modules { get; private set; }
    public Dictionary<ModuleComponent, System> moduleToSystem { get; private set; }
    public event Action OnSystemReplaced = delegate { };

    [Serializable]
    public class System {
        public string name;
        public ModuleSystem.SystemType type;
        public ComponentScriptableObject component;
        public int moduleSize;

        public System(string name, ModuleSystem.SystemType type) {
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
            component = system.component;
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
        public int moduleCount;
    }

    public ModuleSystem(BattleManager battleManager, Unit unit, UnitScriptableObject unitScriptableObject) {
        this.unit = unit;
        List<System> systemComponents = unitScriptableObject.GetSystems();
        List<IModule> prefabModules = unitScriptableObject.GetModules();
        systems = new List<System>(systemComponents.Count);
        modules = new List<ModuleComponent>();
        moduleToSystem = new Dictionary<ModuleComponent, System>();
        foreach (System system in systemComponents) {
            if (system == null) {
                Debug.Log($"{unit.GetUnitName()} has a null component at {systems.Count}");
                continue;
            }

            System newSystem = new System(system);
            systems.Add(newSystem);
        }

        foreach (IModule prefabModule in prefabModules) {
            System system = systems[prefabModule.GetSystemIndex()];
            ModuleComponent newComponent = (ModuleComponent)Activator.CreateInstance(
                system.component.GetComponentType(),
                battleManager, prefabModule, unit, system.component);
            modules.Add(newComponent);
            moduleToSystem.Add(newComponent, system);
        }
    }

    #region SystemUpgrades

    public bool IsComponentCompatibleOnSystem(System system, ComponentScriptableObject component) {
        if (component.GetSystemType() != SystemType.Any &&
            component.GetSystemType() != system.type) return false;
        return true;
    }

    public ComponentScriptableObject GetSystemUpgrade(int system) {
        return systems[system].component.upgrade;
    }

    public bool CanUpgradeSystem(int systemIndex, Unit upgrader) {
        return CanUpgradeSystem(systems[systemIndex], upgrader);
    }

    public bool CanUpgradeSystem(System system, Unit upgrader) {
        ComponentScriptableObject current = system.component;
        ComponentScriptableObject upgrade = current.upgrade;
        if (upgrade == null) return false;
        if (!IsComponentCompatibleOnSystem(system, upgrade)) return false;
        if (upgrader.faction.credits < (upgrade.cost - current.cost) * system.moduleCount) return false;
        for (int i = 0; i < upgrade.resourceTypes.Count; i++) {
            long currentAmount = 0;
            int currentTypeIndex = current.resourceTypes.IndexOf(upgrade.resourceTypes[i]);
            if (currentTypeIndex >= 0) currentAmount = current.resourceCosts[currentTypeIndex];
            if (upgrader.GetAllCargoOfType(upgrade.resourceTypes[i], true) <
                upgrade.resourceCosts[i] - currentAmount) {
                return false;
            }
        }

        return true;
    }

    public void UpgradeSystem(int systemIndex, Unit upgrader) {
        UpgradeSystem(systems[systemIndex], upgrader);
    }

    public void UpgradeSystem(System system, Unit upgrader) {
        ComponentScriptableObject current = system.component;
        ComponentScriptableObject upgrade = current.upgrade;
        if (!CanUpgradeSystem(system, upgrader)) return;

        //Pay for the upgrade cost
        upgrader.faction.UseCredits((upgrade.cost - current.cost) * system.moduleCount);
        for (int i = 0; i < upgrade.resourceTypes.Count; i++) {
            upgrader.UseCargo(upgrade.resourceCosts[i] - current.resourceCosts[i] / 2, upgrade.resourceTypes[i]);
        }

        //Upgrade the system
        systems[systems.IndexOf(system)].component = upgrade;

        //Upgrade the moduleComponents
        modules.Where(m => moduleToSystem[m] == system).ToList()
            .ForEach(m => m.Upgrade(upgrade));
        OnSystemReplaced();
    }

    public bool CanReplaceSystem(System system, ComponentScriptableObject replacement, Unit upgrader) {
        if (!IsComponentCompatibleOnSystem(system, replacement)) return false;
        if (upgrader.faction.credits < replacement.cost * system.moduleCount) return false;
        for (int i = 0; i < replacement.resourceTypes.Count; i++) {
            if (upgrader.GetAllCargoOfType(replacement.resourceTypes[i], true) < replacement.resourceCosts[i])
                return false;
        }
        return true;
    }

    public void ReplaceSystem(System system, ComponentScriptableObject replacement, Unit upgrader) {
        if (!CanReplaceSystem(system, replacement, upgrader))
            Debug.LogError("Trying to replace a component that can't be paid for!");

        //Pay for the upgrade cost
        upgrader.faction.UseCredits(replacement.cost * system.moduleCount);
        for (int i = 0; i < replacement.resourceTypes.Count; i++) {
            upgrader.UseCargo(replacement.resourceCosts[i], replacement.resourceTypes[i]);
        }
        systems[systems.IndexOf(system)].component = replacement;


        for (int i = 0; i < modules.Count; i++) {
            ModuleComponent oldModule = modules[i];
            if (moduleToSystem[oldModule] != system) continue;

            object[] args = { unit.battleManager, oldModule.module, unit, replacement };
            modules[i] = (ModuleComponent)Activator.CreateInstance(replacement.GetComponentType(), args);
            moduleToSystem.Remove(oldModule);
            moduleToSystem.Add(modules[i], system);
        }
        OnSystemReplaced();
    }

    #endregion
}
