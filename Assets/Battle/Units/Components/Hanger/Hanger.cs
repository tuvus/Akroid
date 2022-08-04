using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hanger : MonoBehaviour {
    Station station;
    [SerializeField] List<Ship> ships;
    [SerializeField] int maxDockSpace;
    [SerializeField] int dockSpace;

    public void SetupHanger(Station station) {
        ships = new List<Ship>();
        this.station = station;
    }

    public bool DockShip(Ship ship) {
        if (dockSpace < maxDockSpace) {
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


    public List<Ship> GetAllCombatShips() {
        List<Ship> combatShips = new List<Ship>(ships.Count);
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].IsCombatShip()) {
                combatShips.Add(ships[i]);
            }
        }
        return combatShips;
    }

    public List<Ship> GetAllUndamagedCombatShips() {
        List<Ship> combatShips = new List<Ship>(ships.Count);
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].IsCombatShip() && !ships[i].IsDammaged()) {
                combatShips.Add(ships[i]);
            }
        }
        return combatShips;
    }


    public List<Ship> GetShips() {
        return ships;
    }

    public int GetDockedSpace() {
        return dockSpace;
    }

    public int GetMaxDockSpace() {
        return maxDockSpace;
    }
}
