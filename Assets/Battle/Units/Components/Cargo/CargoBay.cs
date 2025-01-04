using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

/// <summary>
/// Handles storing resources in multiple cargo bays.
/// Each cargo bay may only store one resource up to its cargoBayCapacity
/// Does not hold a list of cargo bays but instead calculates how many cargo bays are being used based on how much cargo of a type is being stored.
/// This allows us to easily reserve cargo bays based on the type of resource.
/// </summary>
public class CargoBay : ModuleComponent {
    public enum CargoTypes {
        All = -1,
        Empty = 0,
        Metal = 1,
        Gas = 2,
    }

    public static List<CargoTypes> allCargoTypes = new() { CargoTypes.Metal, CargoTypes.Gas };

    CargoBayScriptableObject cargoBayScriptableObject;

    public Dictionary<CargoTypes, long> cargoBays { get; private set; } = new();
    private int cargoBaysInUse;

    /// <summary>
    /// How many cargo bays are reserved for each type of cargo.
    /// Does not change when the amount of cargo bays used by the type changes.
    /// The All type should not be put in here.
    /// </summary>
    private Dictionary<CargoTypes, int> reservedBaysType = new();

    /// <summary>
    /// The number of empty cargo bays that are reserved.
    /// The sum of every entry in reservedBaysType.
    /// </summary>
    private int reservedCargoBays;

    public CargoBay(BattleManager battleManager, IModule module, Unit unit, ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        cargoBayScriptableObject = (CargoBayScriptableObject)componentScriptableObject;
        foreach (var cargoType in Enum.GetValues(typeof(CargoTypes)).Cast<CargoTypes>()) {
            if (cargoType != CargoTypes.All) {
                cargoBays.Add(cargoType, 0);
                reservedBaysType.Add(cargoType, 0);
            }
        }
    }

    /// <returns> Returns the amount of cargo that could not be loaded. </returns>
    public long LoadCargo(long cargoToLoad, CargoTypes cargoType) {
        // Puts Cargo into the existing half full cargo bay
        long openSpaceInUsedBay = cargoBayScriptableObject.cargoBaySize - cargoBays[cargoType] % cargoBayScriptableObject.cargoBaySize;
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

        // Calculate if we loaded cargo into any reserved cargo bays
        int previousCargoBaysInUse = GetCargoBaysUsedByType(cargoType) - cargoBaysToLoad;
        // If we hadn't filled all of the reserved cargo bays then account for the new reserved cargo bays in use
        if (previousCargoBaysInUse < reservedBaysType[cargoType])
            reservedCargoBays -= math.min(cargoBaysToLoad, reservedBaysType[cargoType] - previousCargoBaysInUse);
        return cargoToLoad - actualCargoToLoad;
    }


    /// <summary> Returns the amount of cargo that could not be used. </summary>
    public long UseCargo(long cargoAmount, CargoTypes cargoType) {
        if (cargoType == CargoTypes.All) {
            foreach (var allCargoType in allCargoTypes) {
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

        // Check if we have freed any reserved cargo bays that should remain reserved
        if (newCargoBaysInUse < reservedBaysType[cargoType])
            reservedCargoBays += math.min(reservedBaysType[cargoType] - newCargoBaysInUse, previousCargoBaysInUse - newCargoBaysInUse);

        return cargoAmount - cargoToUse;
    }

    public void LoadCargoFromBay(CargoBay cargoBay, CargoTypes cargoType, long maxLoad = long.MaxValue) {
        if (cargoType == CargoTypes.All) {
            foreach (var allCargoType in allCargoTypes) {
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
    private int GetOpenCargoBays(CargoTypes cargoType) {
        if (cargoType == CargoTypes.All) return cargoBayScriptableObject.maxCargoBays - cargoBaysInUse;
        return cargoBayScriptableObject.maxCargoBays - cargoBaysInUse - reservedCargoBays +
            math.max(0, reservedBaysType[cargoType] - GetCargoBaysUsedByType(cargoType));
    }

    public long GetOpenCargoCapacityOfType(CargoTypes cargoType) {
        long openSpaceFromUsedCargoBays = 0;
        if (cargoType != CargoTypes.All) openSpaceFromUsedCargoBays = cargoBays[cargoType] % cargoBayScriptableObject.cargoBaySize;
        return openSpaceFromUsedCargoBays + GetOpenCargoBays(cargoType) * cargoBayScriptableObject.cargoBaySize;
    }

    public bool IsCargoFullOfType(CargoTypes cargoTypes) {
        return GetOpenCargoCapacityOfType(cargoTypes) <= 0;
    }

    public bool IsCargoEmptyOfType(CargoTypes cargoTypes) {
        return GetAllCargo(cargoTypes) <= 0;
    }

    public long GetAllCargo(CargoTypes cargoType) {
        if (cargoType == CargoTypes.All) return allCargoTypes.Sum((t) => GetAllCargo(t));
        return cargoBays[cargoType];
    }

    public int GetCargoBaysUsed() {
        return cargoBaysInUse;
    }

    public int GetCargoBaysUsedByType(CargoTypes cargoType) {
        if (cargoType == CargoTypes.All) return cargoBayScriptableObject.maxCargoBays - cargoBaysInUse;
        return (int)((cargoBays[cargoType] + cargoBayScriptableObject.cargoBaySize - 1) / cargoBayScriptableObject.cargoBaySize);
    }

    public int GetMaxCargoBays() {
        return cargoBayScriptableObject.maxCargoBays;
    }

    public long GetCargoBayCapacity() {
        return cargoBayScriptableObject.cargoBaySize;
    }

    public void AddReservedCargoBays(CargoTypes cargoType, int amount) {
        int oldReservedBays = reservedBaysType[cargoType];
        reservedBaysType[cargoType] += amount;
        reservedCargoBays += amount;
        int cargoBaysUsed = GetCargoBaysUsedByType(cargoType);
        if (cargoBaysUsed > oldReservedBays) {
            reservedCargoBays -= math.min(amount, cargoBaysUsed - oldReservedBays);
        }
    }
}
