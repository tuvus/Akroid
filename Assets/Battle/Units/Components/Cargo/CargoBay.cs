using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoBay : ModuleComponent {
    public enum CargoTypes {
        Empty = 0,
        Metal = 1,
    }

    CargoBayScriptableObject cargoBayScriptableObject;

    public List<CargoTypes> cargoBayTypes = new List<CargoTypes>();
    public List<long> cargoBays = new List<long>();

    public override void SetupComponent(Module module, ComponentScriptableObject componentScriptableObject) {
        cargoBayScriptableObject = (CargoBayScriptableObject)componentScriptableObject;
    }

    /// <summary>
    /// Returns Cargo that was not loaded.
    /// </summary>
    public long LoadCargo(long cargoToLoad, CargoTypes cargoType) {
        //Puts Cargo in existing cargo bays
        for (int i = 0; i < cargoBays.Count; i++) {
            cargoToLoad = AddCargoToBay(cargoToLoad, cargoType, i);
            if (cargoToLoad <= 0)
                return 0;
        }
        //Puts Cargo in new cargo bays
        if (cargoToLoad > 0) {
            while (GetOpenCargoBays() > 0 && cargoToLoad > 0) {
                cargoToLoad = AddNewCargoBay(cargoToLoad, cargoType);
            }
        }
        return cargoToLoad;
    }

    /// <summary>
    /// Returns Unadded Cargo
    /// </summary>
    long AddCargoToBay(long cargoToAdd, CargoTypes cargoType, int cargoBayNumber) {
        if (cargoBayTypes[cargoBayNumber] != cargoType)
            return cargoToAdd;
        if (cargoBays[cargoBayNumber] + cargoToAdd <= cargoBayScriptableObject.cargoBaySize) {
            cargoBays[cargoBayNumber] += cargoToAdd;
            return 0;
        }
        long returnValue = cargoToAdd + cargoBays[cargoBayNumber] - cargoBayScriptableObject.cargoBaySize;
        cargoBays[cargoBayNumber] = cargoBayScriptableObject.cargoBaySize;
        return returnValue;
    }

    /// <summary>
    /// Returns Unadded Cargo
    /// </summary>
    long AddNewCargoBay(long cargoAmount, CargoTypes cargoType) {
        if (cargoBays.Count < cargoBayScriptableObject.maxCargoBays) {
            if (cargoAmount <= cargoBayScriptableObject.cargoBaySize) {
                cargoBays.Add(cargoAmount);
                cargoBayTypes.Add(cargoType);
                return 0;
            } else {
                cargoBays.Add(cargoBayScriptableObject.cargoBaySize);
                cargoBayTypes.Add(cargoType);
                return cargoAmount - cargoBayScriptableObject.cargoBaySize;
            }
        }
        return cargoAmount;
    }

    int GetOpenCargoBays() {
        return cargoBayScriptableObject.maxCargoBays - cargoBays.Count;
    }

    /// <summary>
    /// Returns Unused Cargo
    /// </summary>
    public long UseCargo(long cargoAmount, CargoTypes cargoType) {
        long cargoToUse = cargoAmount;
        for (int i = 0; i < cargoBays.Count; i++) {
            cargoToUse = UseCargoFromBay(cargoAmount, cargoType, i);
            if (cargoToUse <= 0)
                return 0;
        }
        return cargoToUse;
    }

    /// <summary>
    /// Returns Unused Cargo
    /// </summary>
    long UseCargoFromBay(long cargoAmount, CargoTypes cargoType, int cargoBayNumber) {
        if (cargoType != cargoBayTypes[cargoBayNumber])
            return cargoAmount;
        if (cargoBays[cargoBayNumber] > cargoAmount) {
            cargoBays[cargoBayNumber] -= cargoAmount;
            return 0;
        } else {
            long cargo = cargoAmount -= cargoBays[cargoBayNumber];
            cargoBays.RemoveAt(cargoBayNumber);
            cargoBayTypes.RemoveAt(cargoBayNumber);
            return cargo;
        }
    }

    public void LoadCargoFromBay(CargoBay cargoBay, CargoTypes cargoType, long maxLoad = long.MaxValue) {
        long openSpace = GetOpenCargoCapacityOfType(cargoType);
        if (openSpace <= 0)
            return;
        long targetAvailableCargo = cargoBay.GetAllCargo(cargoType);
        long cargoToTransfer = Unity.Mathematics.math.min(targetAvailableCargo, Unity.Mathematics.math.min(openSpace, maxLoad));
        cargoBay.UseCargo(cargoToTransfer, cargoType);
        LoadCargo(cargoToTransfer, cargoType);
    }

    public long GetOpenCargoCapacityOfType(CargoTypes cargoType) {
        long totalCargoCapacity = 0;
        for (int i = 0; i < cargoBays.Count; i++) {
            if (cargoBayTypes[i] == cargoType) {
                totalCargoCapacity += cargoBayScriptableObject.cargoBaySize - cargoBays[i];
            }
        }
        return totalCargoCapacity + (GetOpenCargoBays() * cargoBayScriptableObject.cargoBaySize);
    }

    public bool IsCargoFullOfType(CargoTypes cargoTypes) {
        return GetOpenCargoCapacityOfType(cargoTypes) <= 0;
    }

    public bool IsCargoEmptyOfType(CargoTypes cargoTypes) {
        return GetAllCargo(cargoTypes) <= 0;
    }

    public long GetAllCargo(CargoTypes cargoType) {
        long totalCargo = 0;
        for (int i = 0; i < cargoBays.Count; i++) {
            if (cargoBayTypes[i] == cargoType) {
                totalCargo += cargoBays[i];
            }
        }
        return totalCargo;
    }

    public int GetUsedCargoBays() {
        return cargoBays.Count;
    }

    public int GetMaxCargoBays() {
        return cargoBayScriptableObject.maxCargoBays;
    }

    public long GetCargoBayCapacity() {
        return cargoBayScriptableObject.cargoBaySize;
    }
}
