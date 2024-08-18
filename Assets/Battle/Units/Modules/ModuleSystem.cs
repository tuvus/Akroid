using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class ModuleSystem : MonoBehaviour {
    public enum SystemType {
        Any,
        Utility,
        Weapon,
        Turret,
        Thruster,
    }

    [Serializable]
    public struct System {
        public string name;
        public SystemType type;
        public ComponentScriptableObject component;
        public int moduleSize;
        public int moduleCount { get; private set; }

        public System(string name, SystemType type) {
            this.name = name;
            this.type = type;
            moduleCount = 0;
            moduleSize = 0;
            component = null;
        }

        public System(System system, int moduleCount) {
            this.name = system.name;
            this.type = system.type;
            this.moduleCount = moduleCount;
            moduleSize = 0;
            component = system.component;
        }

        public System(System system, ComponentScriptableObject component) {
            this.name = system.name;
            this.type = system.type;
            this.moduleCount = system.moduleCount;
            this.moduleSize = system.moduleSize;
            this.component = component;
        }
    }

    private Unit unit;
    [field: SerializeField] public List<System> systems { get; private set; } = new List<System>();
    [field: SerializeField] public List<Module> modules { get; private set; } = new List<Module>();

    public void SetupModuleSystem(Unit unit, UnitScriptableObject unitScriptableObject) {
        this.unit = unit;
        List<ComponentScriptableObject> systemComponents = unitScriptableObject.GetSystemComponents();
        for (int i = 0; i < systems.Count; i++) {
            systems[i] = new System(systems[i], systemComponents[i]);
        }
        unitScriptableObject.ApplyComponentsToSystems(systems);

        for (int i = 0; i < modules.Count; i++) {
            if (systems[modules[i].system].component == null) continue;
            ModuleComponent newComponent = modules[i].gameObject.AddComponent(systems[modules[i].system].component.GetComponentType()).GetComponent<ModuleComponent>();
            if (newComponent == null) {
                print(systems[modules[i].system].component.GetComponentType());
                continue;
            }
            newComponent.SetupComponent(modules[i], systems[modules[i].system].component);
        }
        RefreshSystemCounts();

    }

    #region SystemsAndModules
    public void AddSystem() {
        systems.Add(new System("New System", SystemType.Any));
    }

    public void AddSystem(string name, SystemType type) {
        systems.Add(new System(name, type));
    }

    public void AddSystemAndReplace(SystemType newSystemType) {
        if (newSystemType == SystemType.Any) {
            AddSystemAndReplace(SystemType.Turret);
            if (systems[systems.Count - 1].moduleCount == 0)
                RemoveSystem(systems.Count - 1);
            AddSystemAndReplace(SystemType.Weapon);
            if (systems[systems.Count - 1].moduleCount == 0)
                RemoveSystem(systems.Count - 1);
            AddSystemAndReplace(SystemType.Utility);
            if (systems[systems.Count - 1].moduleCount == 0)
                RemoveSystem(systems.Count - 1);
            AddSystemAndReplace(SystemType.Thruster);
            if (systems[systems.Count - 1].moduleCount == 0)
                RemoveSystem(systems.Count - 1);
            return;
        }

        string name = newSystemType.ToString() + "s";
        if (newSystemType == SystemType.Utility)
            name = "Utilities";
        systems.Add(new System(name, newSystemType));
        int systemIndex = systems.Count - 1;
        for (int i = 0; i < transform.childCount; i++) {
            Transform targetTransform = transform.GetChild(i);
            string targetName = "";
            switch (systems[systemIndex].type) {
                case SystemType.Any:
                    targetName = "";
                    break;
                case SystemType.Utility:
                    targetName = "Shield";
                    break;
                case SystemType.Weapon:
                    targetName = "MissileLauncher";
                    break;
                case SystemType.Turret:
                    targetName = "Turret";
                    break;
                case SystemType.Thruster:
                    targetName = "Thruster";
                    break;
            }

            if ((targetTransform.name.Contains(targetName) || (systems[systemIndex].type == SystemType.Utility && targetTransform.name.Contains("CargoBay")) || (systems[systemIndex].type == SystemType.Utility && targetTransform.name.Contains("ResearchEquipment")) || (systems[systemIndex].type == SystemType.Utility && targetTransform.name.Contains("Hangar"))) && !targetTransform.GetComponent<Module>()) {
                Module newModule;
                if (systems[systemIndex].type == SystemType.Turret && targetTransform.GetComponent<Turret>() != null) {
                    Turret turret = targetTransform.GetComponent<Turret>();
                    newModule = AddModule(systemIndex, 0, 0, 0);
                    newModule.name = targetTransform.name;
                } else {
                    newModule = AddModule(systemIndex);
                }
                if (systems[systemIndex].type == SystemType.Thruster && targetTransform.GetComponent<Thruster>() != null) {
                    newModule.name = targetTransform.name;
                }
                newModule.transform.position = targetTransform.position;
                newModule.transform.rotation = targetTransform.rotation;
                newModule.transform.localScale = targetTransform.localScale;
                if (targetTransform.GetComponent<SpriteRenderer>() != null) {
                    newModule.GetComponent<SpriteRenderer>().sortingOrder = targetTransform.GetComponent<SpriteRenderer>().sortingOrder;
                }
            }
        }
        RefreshSystemCounts();
    }

    public void RemoveSystem(int system, bool destroyModules = false) {
        if (system >= systems.Count)
            return;
        if (destroyModules) {
            for (int i = modules.Count - 1; i >= 0; i--) {
                Debug.Log("1");
                if (modules[i].system == system) {
                    if (modules[i] != null)
                        DestroyImmediate(modules[i].gameObject);
                    modules.RemoveAt(i);
                } else if (modules[i].system > system) {
                    modules[i].DecrementSystemIndex();
                }
            }
        }
        systems.RemoveAt(system);
    }

    public void RemoveAllSystems() {
        for (int i = systems.Count - 1; i >= 0; i--) {
            RemoveSystem(i);
        }
    }

    public Module AddModule(int system) => AddModule(system, 0, 0, 0);

    public Module AddModule(int system, float rotation, float minRotate, float maxRotate) {
        if (system >= systems.Count)
            return null;
        systems[system] = new System(systems[system], systems[system].moduleCount + 1);
        Module newModule = Instantiate(Resources.Load<GameObject>("Prefabs/Module"), transform).GetComponent<Module>();
        modules.Add(newModule);
        newModule.name = systems[system].type.ToString();
        newModule.CreateModule(this, system, rotation, minRotate, maxRotate);
        RefreshSystemCounts();
        return newModule;
    }

    public void RemoveModule(Module removeModule) {
        systems[removeModule.system] = new System(systems[removeModule.system], systems[removeModule.system].moduleCount - 1);
        DestroyImmediate(removeModule.gameObject);
        modules.Remove(removeModule);
        RefreshSystemCounts();
    }

    [ContextMenu("RefreshSystems")]
    public void RefreshSystemCounts() {
        for (int i = 0; i < systems.Count; i++) {
            systems[i] = new System(systems[i], modules.Count(t => t.system == i));
        }
    }

    public List<Module> GetModulesOfSystem(int system) => modules.FindAll(a => a.system == system);
    #endregion

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
            GetModulesOfSystem(systemIndex).ForEach(a => a.moduleComponent.SetupComponent(a, systems[systemIndex].component));
        }
    }
    #endregion
}
