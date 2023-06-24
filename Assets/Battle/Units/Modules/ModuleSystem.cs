using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ModuleSystem : MonoBehaviour {
    public enum SystemType {
        Any,
        Utility,
        Weapon,
        Engine,
    }

    [Serializable]
    public struct System {
        public string name;
        public SystemType type;
        private int moduleCount;

        public System(string name, SystemType type) {
            this.name = name;
            this.type = type;
            moduleCount = 0;
        }

        public System(System system, int moduleChange) {
            this.name = system.name;
            this.type = system.type;
            this.moduleCount = system.moduleCount + moduleChange;
        }
    }

    private Unit unit;
    [field: SerializeField] public List<System> systems { get; private set; } = new List<System>();
    [field: SerializeField] public List<Module> modules { get; private set; } = new List<Module>();

    public void AddSystem() {
        systems.Add(new System("New System", SystemType.Any));
    }

    public void RemoveSystem(int system) {
        for (int i = modules.Count - 1; i >= 0; i--) {
            if (modules[i].system == system) {
                DestroyImmediate(modules[i].gameObject);
                modules.RemoveAt(i);
            }
        }
        systems.RemoveAt(system);
    }

    public void AddModule(int system) {
        if (system >= systems.Count)
            return;
        systems[system] = new System(systems[system], 1);
        Module newModule = Instantiate(Resources.Load<GameObject>("Prefabs/Module"), transform).GetComponent<Module>();
        modules.Add(newModule);
        newModule.name = systems[system].name;
        newModule.SetupModule(this, system);
    }

    public void RemoveModule(int module) {
        if (module >= modules.Count) 
            return;
        systems[modules[module].system] = new System(systems[modules[module].system], -1);
        DestroyImmediate(modules[module].gameObject);
        modules.RemoveAt(module);
    }

    public void SetupModuleSystem(Unit unit) {
        this.unit = unit;
    }
}
