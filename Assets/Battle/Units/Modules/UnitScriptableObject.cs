using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using static CargoBay;

public class UnitScriptableObject : ScriptableObject {
    public long cost;
    public List<CargoTypes> resourceTypes;
    public List<long> resourceCosts;

    public string prefabPath;
    public string unitName;
    public int maxHealth;
    public Sprite sprite;

    [SerializeField] private ComponentData[] components;
    [SerializeField] private SystemData[] systems;

    [Serializable]
    private class ComponentData {
        [HideInInspector]
        public string name;
        public ComponentScriptableObject component;

        public ComponentData(string name, ComponentScriptableObject component) {
            this.name = name;
            this.component = component;
        }
    }

    [Serializable]
    private class SystemData {
        [HideInInspector]
        public string name;
        public int moduleCount;
        public ComponentScriptableObject component;

        public SystemData(string name, int moduleCount, ComponentScriptableObject component) {
            this.name = name;
            this.moduleCount = moduleCount;
            this.component = component;
        }
    }

    public void OnValidate() {
        if (systems == null) {
            systems = new SystemData[0];
        }
        GameObject targetPrefab = Resources.Load<GameObject>(prefabPath);
        if (targetPrefab != null) {
            SystemData[] oldSystems = systems;
            ModuleSystem moduleSystem = targetPrefab.GetComponent<ModuleSystem>();
            systems = new SystemData[moduleSystem.systems.Count];
            for (int i = 0; i < Mathf.Min(oldSystems.Length, systems.Length); i++) {
                systems[i] = oldSystems[i];
            }
            for (int i = 0; i < moduleSystem.systems.Count; i++) {
                if (systems[i] != null) {
                    systems[i].name = moduleSystem.systems[i].name;
                    int systemModuleCount = moduleSystem.modules.Count(t => t.system == i);
                    systems[i] = new SystemData(moduleSystem.systems[i].name, systemModuleCount, systems[i].component);
                }
            }
        }

        UpdateCosts();
    }

    protected virtual void UpdateCosts() {
        cost = maxHealth * 10;
        resourceTypes.Clear();
        resourceCosts.Clear();
        AddResourceCost(CargoTypes.Metal, maxHealth);
        systems.ToList().ForEach(t => {
            if (t == null || t.component == null) {
                Debug.Log("Null Component " + unitName);
                return;
            }
            if (t.moduleCount == 0) Debug.Log("Error");
            cost += t.component.cost * t.moduleCount;
            for (int f = 0; f < t.component.resourceTypes.Count; f++) {
                AddResourceCost(t.component.resourceTypes[f], t.component.resourceCosts[f] * t.moduleCount);
            }
        });
    }

    protected void AddResourceCost(CargoTypes type, long cost) {
        int metalIndex = resourceTypes.IndexOf(type);
        if (metalIndex == -1) {
            resourceTypes.Add(type);
            resourceCosts.Add(0);
            metalIndex = resourceTypes.Count - 1;
        }
        resourceCosts[metalIndex] += cost;
    }

    public void ApplyComponentsToSystems(List<ModuleSystem.System> moduleSystems) {
        for (int i = 0; i < moduleSystems.Count; i++) {
            moduleSystems[i] = new ModuleSystem.System(moduleSystems[i], systems[i].component);
        }
    }


    [ContextMenu("ConvertUnitComponents")]
    public void ConvertUnitComponents() {
        Unit unit = Resources.Load<Unit>(prefabPath);
        ModuleSystem moduleSystem = unit.GetComponent<ModuleSystem>();
        List<ModuleSystem.System > oldSystems = new List<ModuleSystem.System>(moduleSystem.systems);
        moduleSystem.systems.Clear();
        List<SystemData> newSystems = new List<SystemData>();
        for (int i = 0; i < components.Length;) {
            moduleSystem.AddSystem(GenerateName(components[i].name), oldSystems[moduleSystem.modules[i].system].type);
            newSystems.Add(new SystemData(moduleSystem.systems.Last().name, -1, components[i].component));
            do {
                moduleSystem.modules[i].SetSystem(moduleSystem.systems.Count - 1);
                i++;
            } while (i < components.Length && components[i].component == newSystems.Last().component);
        }
        systems = newSystems.ToArray();
    }

    string GenerateName(string name) {
        if (name.StartsWith("Left")) {
            return name.Substring(4) + "s";
        }
        return name;
    }
}
