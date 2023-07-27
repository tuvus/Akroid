using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Playables;
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
        public int count { get; private set; }

        public System(string name, SystemType type) {
            this.name = name;
            this.type = type;
            count = 0;
        }

        public System(System system, int moduleChange) {
            this.name = system.name;
            this.type = system.type;
            this.count = system.count + moduleChange;
        }
    }

    private Unit unit;
    [field: SerializeField] public List<System> systems { get; private set; } = new List<System>();
    [field: SerializeField] public List<Module> modules { get; private set; } = new List<Module>();

    #region SystemsAndModules
    public void AddSystem() {
        systems.Add(new System("New System", SystemType.Any));
    }

    public void AddSystemAndReplace(SystemType newSystemType) {
        if (newSystemType == SystemType.Any) {
            AddSystemAndReplace(SystemType.Turret);
            if (systems[systems.Count - 1].count == 0)
                RemoveSystem(systems.Count - 1);
            AddSystemAndReplace(SystemType.Weapon);
            if (systems[systems.Count - 1].count == 0)
                RemoveSystem(systems.Count - 1);
            AddSystemAndReplace(SystemType.Utility);
            if (systems[systems.Count - 1].count == 0)
                RemoveSystem(systems.Count - 1);
            AddSystemAndReplace(SystemType.Thruster);
            if (systems[systems.Count - 1].count == 0)
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

            if ((targetTransform.name.Contains(targetName) || (systems[systemIndex].type == SystemType.Utility && targetTransform.name.Contains("CargoBay")) || (systems[systemIndex].type == SystemType.Utility && targetTransform.name.Contains("ResearchEquipment")) || (systems[systemIndex].type == SystemType.Utility && targetTransform.name.Contains("Hanger"))) && !targetTransform.GetComponent<Module>()) {
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
    }

    public void RemoveSystem(int system) {
        if (system >= systems.Count)
            return;
        for (int i = modules.Count - 1; i >= 0; i--) {
            if (modules[i].system == system) {
                if (modules[i] != null)
                    DestroyImmediate(modules[i].gameObject);
                modules.RemoveAt(i);
            } else if (modules[i].system > system) {
                modules[i].DecrementSystemIndex();
            }
        }
        systems.RemoveAt(system);
    }

    public void RemoveAllSystems() {
        for (int i = systems.Count - 1; i >= 0; i--) {
            RemoveSystem(i);
        }
    }

    public Module AddModule(int system) {
        if (system >= systems.Count)
            return null;
        systems[system] = new System(systems[system], 1);
        Module newModule = ((GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Prefabs/Module"), transform)).GetComponent<Module>();
        modules.Add(newModule);
        newModule.name = systems[system].type.ToString();
        newModule.CreateModule(this, system, 0);
        return newModule;
    }

    public Module AddModule(int system, float rotation, float minRotate, float maxRotate) {
        if (system >= systems.Count)
            return null;
        systems[system] = new System(systems[system], 1);
        Module newModule = ((GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Prefabs/Module"), transform)).GetComponent<Module>();
        modules.Add(newModule);
        newModule.name = systems[system].type.ToString();
        newModule.CreateModule(this, system, rotation, minRotate, maxRotate, 0);
        return newModule;
    }

    public void RemoveModule(Module removeModule) {
        systems[removeModule.system] = new System(systems[removeModule.system], -1);
        DestroyImmediate(removeModule.gameObject);
        modules.Remove(removeModule);
    }
    #endregion

    public void SetupModuleSystem(Unit unit, UnitScriptableObject unitScriptableObject) {
        this.unit = unit;
        ComponentScriptableObject[] components = unitScriptableObject.GetComponents(); 
        for (int i = 0; i < components.Length; i++) {
            ;
            ModuleComponent newComponent = modules[i].gameObject.AddComponent(components[i].GetComponentType()).GetComponent<ModuleComponent>();
            if (newComponent == null) {
                print(components[i].GetComponentType());
                continue;
            }
            newComponent.SetupComponent(modules[i], components[i]);
        }
    }
}
