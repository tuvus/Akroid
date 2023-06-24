using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ModuleSystem;

public class Module : MonoBehaviour {
    public ModuleSystem moduleSystem { get; private set; }

    public int system { get; private set; }
    [field: SerializeField] public float rotation { get; private set; }
    [field: SerializeField] public float minRotate { get; private set; }
    [field: SerializeField] public float maxRotate { get; private set; }
    [field: SerializeField] public int size { get; private set; }



    public void SetupModule(ModuleSystem moduleSystem, int system) {
        this.moduleSystem = moduleSystem;
        this.system = system;
    }
}
