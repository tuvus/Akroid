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
        public ComponentScriptableObject component;

        public SystemData(string name, ComponentScriptableObject component) {
            this.name = name;
            this.component = component;
        }
    }

    public void OnValidate() {
        if (components == null) {
            components = new ComponentData[0];
        }
        GameObject targetPrefab = Resources.Load<GameObject>(prefabPath);
        if (targetPrefab != null) {
            ComponentData[] oldComponents = components;
            ModuleSystem moduleSystem = targetPrefab.GetComponent<ModuleSystem>();
            components = new ComponentData[moduleSystem.modules.Count];
            for (int i = 0; i < Mathf.Min(oldComponents.Length, components.Length); i++) {
                components[i] = oldComponents[i];
            }
            for (int i = 0; i < moduleSystem.modules.Count; i++) {
                if (components[i] != null) {
                    components[i].name = moduleSystem.modules[i].name;
                } else {
                    components[i] = new ComponentData(moduleSystem.modules[i].name, null);
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

        for (int i = 0; i < components.Length; i++) {
            for (int f = 0; f < components[i].component.resourceTypes.Count; f++) {
                cost += components[i].component.cost;
                AddResourceCost(components[i].component.resourceTypes[f], components[i].component.resourceCosts[f]);
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
            newSystems.Add(new SystemData(moduleSystem.systems.Last().name, components[i].component));
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
