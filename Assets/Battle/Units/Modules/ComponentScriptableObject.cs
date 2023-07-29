using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CargoBay;

public abstract class ComponentScriptableObject : ScriptableObject {
    public long cost;
    public List<CargoTypes> resourceTypes;
    public List<long> resourceCosts;

    public abstract Type GetComponentType();

#pragma warning disable CS0414
    public virtual void OnValidate() {
        UpdateCosts();
    }
#pragma warning restore CS0414

    protected virtual void UpdateCosts() {
        cost = 0;
        resourceTypes.Clear();
        resourceCosts.Clear();
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
}