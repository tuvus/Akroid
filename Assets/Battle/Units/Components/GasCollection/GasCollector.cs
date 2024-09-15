using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GasCollector : ModuleComponent {
    GasCollectorScriptableObject gasCollectorScriptableObject;
    float collectionTime;

    public GasCollector(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        gasCollectorScriptableObject = (GasCollectorScriptableObject)componentScriptableObject;

        collectionTime = gasCollectorScriptableObject.collectionSpeed;
    }

    /// <returns> False if we are finished collecting gas, true otherwise </returns>
    public bool CollectGas(GasCloud gasCloud, float deltaTime) {
        collectionTime -= deltaTime;
        if (collectionTime <= 0) {
            if (unit.LoadCargo(gasCloud.CollectGas(gasCollectorScriptableObject.collectionAmount), CargoBay.CargoTypes.Gas) > 0) {
                collectionTime = gasCollectorScriptableObject.collectionSpeed;
                return false;
            }
            collectionTime += gasCollectorScriptableObject.collectionSpeed;
        }
        return true;
    }

    public bool WantsMoreGas() {
        return unit.GetAvailableCargoSpace(CargoBay.CargoTypes.Gas) > 0;
    }
}
