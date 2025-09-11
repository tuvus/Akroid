using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
///     Handles storing resources in multiple cargo bays.
///     Each cargo bay may only store one resource up to its cargoBayCapacity
///     Does not hold a list of cargo bays but instead calculates how many cargo bays are being used based on how much
///     cargo of a type is being stored.
///     This allows us to easily reserve cargo bays based on the type of resource.
/// </summary>
public class CargoBay : ModuleComponent {
    public enum CargoType {
        All = -1,
        Empty = 0,
        Metal = 1,
        Gas = 2
    }

    public static readonly List<CargoType> allCargoTypes = new List<CargoType> { CargoType.Metal, CargoType.Gas };

    private CargoBayScriptableObject cargoBayScriptableObject;
    private int cargoBaysInUse;
    public Dictionary<CargoType, long> cargoBays { get; } = new Dictionary<CargoType, long>();

    public CargoBay(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        cargoBayScriptableObject = (CargoBayScriptableObject)componentScriptableObject;
        foreach (CargoType cargoType in allCargoTypes) {
            cargoBays.Add(cargoType, 0);
        }
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        cargoBayScriptableObject = (CargoBayScriptableObject)componentScriptableObject;
    }


    /// <returns> Returns the amount of cargo that could not be loaded. </returns>
    public long LoadCargo(long cargoToLoad, CargoType cargoType) {
        // Puts Cargo into the existing half full cargo bay
        long openSpaceInUsedBay = cargoBayScriptableObject.cargoBaySize -
            cargoBays[cargoType] % cargoBayScriptableObject.cargoBaySize;
        if (openSpaceInUsedBay != cargoBayScriptableObject.cargoBaySize) {
            cargoBays[cargoType] += math.min(cargoToLoad, openSpaceInUsedBay);
            cargoToLoad -= math.min(cargoToLoad, openSpaceInUsedBay);
        }

        if (cargoToLoad <= 0) return cargoToLoad;

        // Puts Cargo in new cargo bays
        int cargoBaysToLoad = math.min(GetOpenCargoBays(cargoType),
            // The minimum number of open cargo bays needed to fill all of our cargo (using the ceiling function)
            (int)((cargoToLoad + cargoBayScriptableObject.cargoBaySize - 1) / cargoBayScriptableObject.cargoBaySize));
        long actualCargoToLoad = math.min(cargoBaysToLoad * cargoBayScriptableObject.cargoBaySize, cargoToLoad);
        cargoBays[cargoType] += actualCargoToLoad;
        cargoBaysInUse += cargoBaysToLoad;

        return cargoToLoad - actualCargoToLoad;
    }


    /// <summary> Returns the amount of cargo that could not be used. </summary>
    public long UseCargo(long cargoAmount, CargoType cargoType) {
        if (cargoType == CargoType.All) {
            foreach (CargoType allCargoType in allCargoTypes) {
                cargoAmount = UseCargo(cargoAmount, allCargoType);
                if (cargoAmount <= 0) return cargoAmount;
            }

            return cargoAmount;
        }

        long cargoToUse = math.min(cargoAmount, cargoBays[cargoType]);


        int previousCargoBaysInUse = GetCargoBaysUsedByType(cargoType);
        cargoBays[cargoType] -= cargoToUse;
        int newCargoBaysInUse = GetCargoBaysUsedByType(cargoType);
        cargoBaysInUse -= previousCargoBaysInUse - newCargoBaysInUse;

        return cargoAmount - cargoToUse;
    }

    public void LoadCargoFromBay(CargoBay cargoBay, CargoType cargoType, long maxLoad = long.MaxValue) {
        if (cargoType == CargoType.All) {
            foreach (CargoType allCargoType in allCargoTypes) {
                long cargoToLoadOfType = math.min(maxLoad, GetOpenCargoCapacityOfType(allCargoType));
                long cargoLoaded = cargoToLoadOfType - cargoBay.UseCargo(cargoToLoadOfType, allCargoType);
                LoadCargo(cargoLoaded, allCargoType);
                maxLoad -= cargoLoaded;
            }

            return;
        }

        long cargoToLoad = math.min(maxLoad, GetOpenCargoCapacityOfType(cargoType));
        LoadCargo(cargoToLoad - cargoBay.UseCargo(cargoToLoad, cargoType), cargoType);
    }

    /// <returns> The amount of empty cargo bays that can be used for this cargo type. </returns>
    private int GetOpenCargoBays(CargoType cargoType) {
        if (cargoType == CargoType.All) return cargoBayScriptableObject.maxCargoBays - cargoBaysInUse;
        return cargoBayScriptableObject.maxCargoBays - cargoBaysInUse;
    }

    public long GetOpenCargoCapacityOfType(CargoType cargoType) {
        long openSpaceFromUsedCargoBay = 0;
        if (cargoType != CargoType.All) {
            openSpaceFromUsedCargoBay = cargoBayScriptableObject.cargoBaySize -
                cargoBays[cargoType] % cargoBayScriptableObject.cargoBaySize;
            if (openSpaceFromUsedCargoBay == cargoBayScriptableObject.cargoBaySize) openSpaceFromUsedCargoBay = 0;
        }
        return openSpaceFromUsedCargoBay + GetOpenCargoBays(cargoType) * cargoBayScriptableObject.cargoBaySize;
    }

    public bool IsCargoFullOfType(CargoType cargoType) {
        return GetOpenCargoCapacityOfType(cargoType) <= 0;
    }

    public bool IsCargoEmptyOfType(CargoType cargoType) {
        return GetAllCargo(cargoType) <= 0;
    }

    public long GetAllCargo(CargoType cargoType) {
        if (cargoType == CargoType.All) return allCargoTypes.Sum(t => GetAllCargo(t));
        return cargoBays[cargoType];
    }

    public int GetCargoBaysUsed() {
        return cargoBaysInUse;
    }

    public int GetCargoBaysUsedByType(CargoType cargoType) {
        if (cargoType == CargoType.All) return cargoBayScriptableObject.maxCargoBays - cargoBaysInUse;
        return (int)((cargoBays[cargoType] + cargoBayScriptableObject.cargoBaySize - 1) /
            cargoBayScriptableObject.cargoBaySize);
    }

    public int GetMaxCargoBays() {
        return cargoBayScriptableObject.maxCargoBays;
    }

    public long GetCargoBayCapacity() {
        return cargoBayScriptableObject.cargoBaySize;
    }
}
