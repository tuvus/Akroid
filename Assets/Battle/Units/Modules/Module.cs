using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A template that holds general information about a ModuleComponent and connects it to a system in the ModuleSystem.
/// Information held by the Module is set in the prefab before the game begins and probably aren't going to change during the game.
/// </summary>
public class Module : MonoBehaviour {
    public ModuleSystem moduleSystem { get; private set; }

    [field: SerializeField] public int system { get; private set; }
    [field: SerializeField] public float rotation { get; private set; }
    [field: SerializeField] public float minRotate { get; private set; }
    [field: SerializeField] public float maxRotate { get; private set; }
    public ModuleComponent moduleComponent;


    public void CreateModule(ModuleSystem moduleSystem, int system) {
        this.moduleSystem = moduleSystem;
        this.system = system;
    }

    public void CreateModule(ModuleSystem moduleSystem, int system, float rotation, float minRotate, float maxRotate) {
        this.moduleSystem = moduleSystem;
        this.system = system;
        this.rotation = rotation;
        this.minRotate = minRotate;
        this.maxRotate = maxRotate;
    }

    public void DecrementSystemIndex() {
        system--;
    }

    public void SetSystem(int system) {
        this.system = system;
    }
}
