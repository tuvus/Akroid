using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasCollector : ModuleComponent {
    Unit unit;

    GasCollectorScriptableObject gasCollectorScriptableObject;
    float collectionTime;

    public override void SetupComponent(Module module, ComponentScriptableObject componentScriptableObject) {
        gasCollectorScriptableObject = (GasCollectorScriptableObject)componentScriptableObject;
    }

    public void SetupGasCollector(Unit unit) {
        this.unit = unit;
        collectionTime = gasCollectorScriptableObject.collectionSpeed;
    }

    public bool CollectGas(GasCloud gasCloud, float deltaTime) {
        collectionTime -= deltaTime;
        if (collectionTime <= 0) {
            if (unit.LoadCargo(gasCollectorScriptableObject.collectionAmount, CargoBay.CargoTypes.Gas) > 0) {
                collectionTime = gasCollectorScriptableObject.collectionSpeed;
                return true;
            }
            collectionTime += gasCollectorScriptableObject.collectionSpeed;
        }
        return false;
    }

    public bool WantsMoreGas() {
        return unit.GetAvailableCargoSpace(CargoBay.CargoTypes.Gas) > 0;
    }
}
