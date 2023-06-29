using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ComponentScriptableObject : ScriptableObject {
    public abstract Type GetComponentType();
}
