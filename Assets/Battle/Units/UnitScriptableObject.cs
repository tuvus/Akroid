using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
    [SerializeField] private ModuleSystem.System[] systems;

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

    public void OnValidate() {
        if (systems == null) {
            systems = Array.Empty<ModuleSystem.System>();
        }
        GameObject targetPrefab = Resources.Load<GameObject>(prefabPath);
        if (targetPrefab != null) {
            ModuleSystem.System[] oldSystems = systems;
            PrefabModuleSystem prefabModuleSystem = targetPrefab.GetComponent<PrefabModuleSystem>();
            systems = new ModuleSystem.System[prefabModuleSystem.systems.Count];
            for (int i = 0; i < Mathf.Min(oldSystems.Length, systems.Length); i++) {
                systems[i] = oldSystems[i];
            }
            for (int i = 0; i < prefabModuleSystem.systems.Count; i++) {
                if (systems[i] != null) {
                    systems[i] = new ModuleSystem.System(prefabModuleSystem.systems[i], systems[i].component);
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
        foreach (var system in systems.ToList()) {
            if (system == null || system.component == null) {
                Debug.Log("Null Component " + unitName);
                continue;
            }
            if (system.moduleCount == 0) Debug.Log($"{unitName} system {system.name} has a moduleCount of 0!");
            cost += system.component.cost * system.moduleCount;
            for (int f = 0; f < system.component.resourceTypes.Count; f++) {
                AddResourceCost(system.component.resourceTypes[f], system.component.resourceCosts[f] * system.moduleCount + 10);
            }
        }
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
            moduleSystems[i] = new ModuleSystem.System(systems[i], systems[i].component);
        }
    }


    [ContextMenu("ConvertUnitComponents")]
    public void ConvertUnitComponents() {
        // Unit unit = Resources.Load<Unit>(prefabPath);
        // ModuleSystem moduleSystem = unit.moduleSystem;
        // List<ModuleSystem.System > oldSystems = new List<ModuleSystem.System>(moduleSystem.systems);
        // moduleSystem.systems.Clear();
        // List<SystemData> newSystems = new List<SystemData>();
        // for (int i = 0; i < components.Length;) {
        //     moduleSystem.AddSystem(GenerateName(components[i].name), oldSystems[moduleSystem.modules[i].system].type);
        //     newSystems.Add(new SystemData(moduleSystem.systems.Last().name, -1, components[i].component));
        //     do {
        //         moduleSystem.modules[i].SetSystem(moduleSystem.systems.Count - 1);
        //         i++;
        //     } while (i < components.Length && components[i].component == newSystems.Last().component);
        // }
        // systems = newSystems.ToArray();
    }

    public List<ComponentScriptableObject> GetSystemComponents() {
        return systems.Select(a => a.component).ToList();
    }

    string GenerateName(string name) {
        if (name.StartsWith("Left")) {
            return name.Substring(4) + "s";
        }
        return name;
    }
}
