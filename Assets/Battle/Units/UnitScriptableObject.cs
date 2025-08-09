using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using static CargoBay;

public class UnitScriptableObject : ScriptableObject {
    public long cost;
    public List<CargoType> resourceTypes;
    public List<long> resourceCosts;

    public string prefabPath;
    public GameObject prefab;
    public string unitName;
    public int maxHealth;
    public Sprite sprite;
    public AudioResource explosionSound;

    [SerializeField] protected ModuleSystem.System[] systems;
    public DestroyEffectScriptableObject destroyEffect;
    public Vector2 baseScale = Vector2.one;
    // Note: field:SerializeField is required in order for Unity to copy over a private variable when building the game.
    // Removing it will result in the build behaving differently than the editor and object having a size of 0.
    [field: SerializeField] public Vector2 spriteBounds { get; private set; }
    [SerializeField] protected IModule[] modules;
    [field: SerializeField] public float spriteSize { get; private set; }

    public virtual void OnValidate() {
        if (systems == null) {
            systems = Array.Empty<ModuleSystem.System>();
        }

        prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab != null) {
            ModuleSystem.System[] oldSystems = systems;
            PrefabModuleSystem prefabModuleSystem = prefab.GetComponent<PrefabModuleSystem>();
            systems = new ModuleSystem.System[prefabModuleSystem.systems.Count];
            for (int i = 0; i < Mathf.Min(oldSystems.Length, systems.Length); i++) {
                systems[i] = oldSystems[i];
            }

            for (int i = 0; i < prefabModuleSystem.systems.Count; i++) {
                if (systems[i] != null) {
                    systems[i] = new ModuleSystem.System(prefabModuleSystem.systems[i], systems[i].component);
                }
            }

            modules = prefabModuleSystem.modules.Cast<IModule>().ToArray();
        }

        if (sprite != null) {
            if (Calculator.GetSpriteBounds(sprite) != Vector2.zero)
                spriteBounds = Calculator.GetSpriteBounds(sprite);
            spriteSize = Calculator.GetSpriteSizeFromBounds(spriteBounds, baseScale);
        }

        UpdateCosts();
    }

    protected virtual void UpdateCosts() {
        cost = maxHealth * 10;
        resourceTypes.Clear();
        resourceCosts.Clear();
        AddResourceCost(CargoType.Metal, maxHealth);
        foreach (ModuleSystem.System system in systems.ToList()) {
            if (system == null || system.component == null) {
                Debug.Log("Null Component " + unitName);
                continue;
            }

            if (system.moduleCount == 0) Debug.Log($"{unitName} system {system.name} has a moduleCount of 0!");
            cost += system.component.cost * system.moduleCount;
            for (int f = 0; f < system.component.resourceTypes.Count; f++) {
                AddResourceCost(system.component.resourceTypes[f],
                    system.component.resourceCosts[f] * system.moduleCount + 10);
            }
        }
    }

    protected void AddResourceCost(CargoType type, long cost) {
        int metalIndex = resourceTypes.IndexOf(type);
        if (metalIndex == -1) {
            resourceTypes.Add(type);
            resourceCosts.Add(0);
            metalIndex = resourceTypes.Count - 1;
        }

        resourceCosts[metalIndex] += cost;
    }

    public List<ModuleSystem.System> GetSystems() {
        return systems.ToList();
    }

    public List<IModule> GetModules() {
        // In some cases the modules is properly setup in the editor but not in the build.
        // This provides a backup to create the list on the fly.
        if (modules == null) OnValidate();
        return modules.ToList();
    }

    public List<ComponentScriptableObject> GetSystemComponents() {
        return systems.Select(a => a.component).ToList();
    }
}
