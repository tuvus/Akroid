using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ModuleSystem;

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
