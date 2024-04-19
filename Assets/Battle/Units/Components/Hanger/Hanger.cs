using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Hanger : ModuleComponent {
    HangerScriptableObject hangerScriptableObject;
    Station station;
    public List<Ship> ships { get; private set; }
    [SerializeField] int dockSpace;

    public override void SetupComponent(Module module, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, componentScriptableObject);
        hangerScriptableObject = (HangerScriptableObject)componentScriptableObject;
    }

    public void SetupHanger(Station station) {
        ships = new List<Ship>(hangerScriptableObject.maxDockSpace);
        this.station = station;
    }

    public bool DockShip(Ship ship) {
        if (dockSpace < hangerScriptableObject.maxDockSpace) {
            ships.Add(ship);
            dockSpace++;
            return true;
        }
        return false;
    }

    public void RemoveShip(Ship ship) {
        ships.Remove(ship);
        dockSpace--;
    }

    public void UndockAll() {
        for (int i = ships.Count - 1; i >= 0; i--) {
            ships[i].UndockShip();
        }
    }

    public Ship GetCombatShip(int index = 0) {
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].IsCombatShip()) {
                if (index == 0)
                    return ships[i];
                index--;
            }
        }
        return null;
    }

    public Ship GetTransportShip(int index = 0) {
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].IsTransportShip()) {
                if (index == 0)
                    return ships[i];
                index--;
            }
        }
        return null;
    }

    public Ship GetConstructionShip(int index = 0) {
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].IsConstructionShip()) {
                if (index == 0)
                    return ships[i];
                index--;
            }
        }
        return null;
    }

    public Ship GetResearchShip(int index = 0) {
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].IsScienceShip()) {
                if (index == 0)
                    return ships[i];
                index--;
            }
        }
        return null;
    }


    public HashSet<Ship> GetAllCombatShips() {
        return ships.Where(s => s.IsCombatShip()).ToHashSet();
    }

    public HashSet<Ship> GetAllUndamagedCombatShips() {
        return ships.Where(s => s.IsCombatShip() && !s.IsDamaged()).ToHashSet();
    }


    public List<Ship> GetShips() {
        return ships;
    }

    public int GetDockedSpace() {
        return dockSpace;
    }

    public int GetMaxDockSpace() {
        return hangerScriptableObject.maxDockSpace;
    }
}
