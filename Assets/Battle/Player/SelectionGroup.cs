using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

[System.Serializable]
public class SelectionGroup {
    public enum GroupType {
        Station = -2,
        Ship = -1,
        None = 0,
        Ships = 1,
        Units = 2,
        Fleet = 3,
    }
    public GroupType groupType;

    public List<Unit> units = new List<Unit>();
    public Fleet fleet;

    public void ClearGroup() {
        units.Clear();
        fleet = null;
        groupType = GroupType.None;
    }

    public List<Unit> GetAllUnits() {
        if (groupType == GroupType.Fleet) {
            List<Unit> fleetUnits = new List<Unit>();
            foreach (var ship in fleet.GetShips()) {
                fleetUnits.Add(ship);
            }
            return fleetUnits;
        }
        return units;
    }

    public List<Ship> GetAllShips(Faction faction = null) {
        List<Ship> listOfShips = new List<Ship>();
        foreach (Unit unit in units) {
            if (unit.IsShip()) {
                Ship ship = (Ship)unit;
                if (faction != null) {
                    if (ship.faction == faction)
                        listOfShips.Add(ship);
                } else {
                    listOfShips.Add(ship);
                }
            }
        }
        return listOfShips;
    }

    public List<Station> GetAllStations(Faction faction = null) {
        List<Station> listOfStations = new List<Station>();
        foreach (Unit unit in units) {
            if (unit.IsStation()) {
                Station station = (Station)unit;
                if (faction != null) {
                    if (station.faction == faction)
                        listOfStations.Add(station);
                } else {
                    listOfStations.Add(station);
                }
            }
        }
        return listOfStations;
    }

    public bool ContainsUnit(Unit unit) {
        return units.Contains(unit);
    }

    public bool HasShip() {
        if (groupType == GroupType.Fleet && fleet.GetShips().Count > 0)
            return true;
        for (int i = 0; i < units.Count; i++) {
            if (units[i].IsShip())
                return true;
        }
        return false;
    }

    public bool HasStation() {
        for (int i = 0; i < units.Count; i++) {
            if (units[i].IsStation())
                return true;
        }
        return false;
    }

    public void SetShips(List<Ship> shipList) {
        ClearGroup();
        units.AddRange(shipList);
        groupType = GroupType.Ships;
    }

    public void AddShips(List<Ship> shipList) {
        units.AddRange(shipList);
    }

    public void AddShip(Ship ship) {
        units.Add(ship);
        if (groupType == GroupType.Ship)
            groupType = GroupType.Ships;
        else if (groupType == GroupType.None)
            groupType = GroupType.Ship;
        else if (groupType == GroupType.Station)
            groupType = GroupType.Units;
    }

    public List<Ship> GetShipsOfClass(Ship.ShipClass shipClass) {
        List<Ship> shipList = new List<Ship>();
        foreach (var ship in GetAllShips()) {
            if (ship.GetShipClass() == shipClass) {
                shipList.Add(ship);
            }
        }
        return shipList;
    }

    public void SetUnits(List<Unit> unitList) {
        ClearGroup();
        units.AddRange(unitList);
        groupType = GroupType.Units;
    }

    public void CopyGroup(SelectionGroup group) {
        this.groupType = group.groupType;
        foreach (var unit in group.units) {
            units.Add(unit);
        }
        this.fleet = group.fleet;
    }

    public void AddUnits(List<Unit> unitList) {
        units.AddRange(unitList);
        groupType = GroupType.Units;
    }

    public void AddUnits(SelectionGroup unitGroup) {
        AddUnits(unitGroup.units);
    }

    public void AddUnit(Unit unit) {
        units.Add(unit);
        groupType = GroupType.Units;
    }

    public void RemoveUnit(Unit unit) {
        List<Unit> groupUnits = GetAllUnits();
        if (groupUnits.Contains(unit)) {
            if (groupType == GroupType.Fleet) {
                groupUnits.Remove(unit);
                ClearGroup();
                for (int i = 0; i < groupUnits.Count; i++) {
                    AddShip((Ship)groupUnits[i]);
                }
            } else {
                units.Remove(unit);
            }
        }
    }

    /// <summary>
    /// Gets the first ship in the group.
    /// If the group is a fleet returns the first ship in the fleet
    /// </summary>
    /// <returns>the first ship in the group</returns>
    public Ship GetShip() {
        if (groupType == GroupType.Fleet)
            return fleet.ships[0];
        for (int i = 0; i < units.Count; i++) {
            if (units[i].IsShip())
                return (Ship)units[i];
        }
        return null;
    }

    /// <summary>
    /// Gets the first station in the group.
    /// </summary>
    /// <returns>the first station in the group</returns>
    public Station GetStation() {
        for (int i = 0; i < units.Count; i++) {
            if (units[i].IsStation())
                return (Station)units[i];
        }
        return null;
    }

    //Takes out all non ship units in units
    public void ConvertToShips() {
        for (int i = 0; i < units.Count; i++) {
            if ((Ship)units[i] == null) {
                units.RemoveAt(i);
            }
        }
    }

    public void SetShip(Ship ship) {
        if (ship != null) {
            ClearGroup();
            units.Add(ship);
            groupType = GroupType.Ship;
        } else {
            ClearGroup();
        }

    }

    public void SetStation(Station station) {
        if (station != null) {
            ClearGroup();
            //List<Station> stations = new List<Station>();
            //units.Add(stations);
            groupType = GroupType.Station;
        } else {
            ClearGroup();
        }
    }

    public void SetFleet(Fleet fleet) {
        ClearGroup();
        groupType = GroupType.Fleet;
        this.fleet = fleet;
    }

    public int GetUnitCount() {
        if (groupType == GroupType.Fleet)
            return fleet.GetShips().Count;
        return units.Count;
    }

    public void RemoveAllNonCombatShips() {
        for (int i = units.Count - 1; i >= 0; i--) {
            if (!units[i].IsShip() || !((Ship)units[i]).IsCombatShip()) {
                units[i].SelectUnit(UnitSelection.SelectionStrength.Unselected);
                units.RemoveAt(i);
            }
        }
    }

    public void SelectAllUnits(UnitSelection.SelectionStrength strength = UnitSelection.SelectionStrength.Unselected) {
        foreach (Unit unit in units) {
            unit.SelectUnit(strength);
        }
        if (fleet != null) {
            fleet.SelectFleet(strength);
        }
    }

    public void UnselectAllUnits() {
        SelectAllUnits(UnitSelection.SelectionStrength.Unselected);
    }

    public void GiveCommand(Command command, Command.CommandAction commandAction) {
        if (groupType == GroupType.Fleet) {
            fleet.FleetAI.AddUnitAICommand(command, commandAction);
            return;
        }
        for (int i = 0; i < units.Count; i++) {
            if (units[i].IsSpawned() && units[i].IsShip()) {
                ((Ship)units[i]).shipAI.AddUnitAICommand(command, commandAction);
            }
        }
    }

    public void ClearCommands() {
        for (int i = 0; i < units.Count; i++) {
            if (units[i].IsShip()) {
                ((Ship)units[i]).shipAI.ClearCommands();
            }
        }
    }

    public void RemoveAnyUnitsNotInList(List<Unit> unitList) {
        for (int i = units.Count - 1; i >= 0; i--) {
            if (!unitList.Contains(units[i])) {
                units[i].UnselectUnit();
                units.RemoveAt(i);
            }
        }
    }

    public int GetTotalUnitHealth() {
        int totalHealth = 0;
        if (groupType == GroupType.Fleet)
            totalHealth += fleet.GetTotalFleetHealth();
        for (int i = 0; i < units.Count; i++) {
            totalHealth += units[i].GetTotalHealth();
        }
        return totalHealth;
    }
}
