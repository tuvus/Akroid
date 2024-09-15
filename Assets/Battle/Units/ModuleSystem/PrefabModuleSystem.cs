using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages a list of systems for a unit.
/// Each system is holds a component and a list of modules which will use that component.
/// The ModuleSystem is set up in the prefab before compiling.
/// The system components are set in the Unit scriptable objects.
/// </summary>
[Serializable]
public class PrefabModuleSystem : MonoBehaviour {
    public enum SystemType {
        Any,
        Utility,
        Weapon,
        Turret,
        Thruster,
    }

    [Serializable]
    public struct PrefabSystem {
        public string name;
        public SystemType type;
        public int moduleSize;
        public int moduleCount;

        public PrefabSystem(string name, SystemType type) {
            this.name = name;
            this.type = type;
            moduleCount = 0;
            moduleSize = 0;
        }

        public PrefabSystem(PrefabSystem prefabSystem, int moduleCount) {
            this.name = prefabSystem.name;
            this.type = prefabSystem.type;
            moduleSize = prefabSystem.moduleSize;
            this.moduleCount = moduleCount;
        }

        public PrefabSystem(string name, SystemType type, int moduleSize, int moduleCount) {
            this.name = name;
            this.type = type;
            this.moduleSize = moduleSize;
            this.moduleCount = moduleCount;
        }
    }

    [field: SerializeField] public List<PrefabSystem> systems { get; private set; } = new List<PrefabSystem>();
    [field: SerializeField] public List<Module> modules { get; private set; } = new List<Module>();

    #region SystemsAndModules

    public void AddSystem() {
        systems.Add(new PrefabSystem("New System", SystemType.Any));
    }

    public void AddSystem(string name, SystemType type) {
        systems.Add(new PrefabSystem(name, type));
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
        systems[system] = new PrefabSystem(systems[system], modules.Count(m => m.system == system));
        Module newModule = Instantiate(Resources.Load<GameObject>("Prefabs/Module"), transform).GetComponent<Module>();
        modules.Add(newModule);
        newModule.name = systems[system].type.ToString();
        // newModule.CreateModule(this, system, rotation, minRotate, maxRotate);
        RefreshSystemCounts();
        return newModule;
    }

    public void RemoveModule(Module removeModule) {
        systems[removeModule.system] = new PrefabSystem(systems[removeModule.system], systems[removeModule.system].moduleCount - 1);
        DestroyImmediate(removeModule.gameObject);
        modules.Remove(removeModule);
        RefreshSystemCounts();
    }

    public void RefreshComponents() {
        modules.Clear();
        modules.AddRange(GetComponentsInChildren<Module>());
        RefreshSystemCounts();
    }

    [ContextMenu("RefreshSystems")]
    public void RefreshSystemCounts() {
        for (int i = 0; i < systems.Count; i++) {
            systems[i] = new PrefabSystem(systems[i], modules.Count(t => t.system == i));
        }
    }

    #endregion
}
