using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class SelectionGroup {
    public enum GroupType {
        Station = -4,
        Ship = -3,
        Unit = -2,
        Object = -1,
        None = 0,
        Objects = 1,
        Ships = 2,
        Units = 3,
        Fleet = 4,
    }
    public GroupType groupType;

    public List<BattleObject> objects = new List<BattleObject>();
    public Fleet fleet;

    public void ClearGroup() {
        objects.Clear();
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
        return objects.Where(obj => obj.IsUnit()).Cast<Unit>().ToList();
    }

    public List<Ship> GetAllShips(Faction faction = null) {
        List<Ship> listOfShips = new List<Ship>();
        foreach (BattleObject battleObject in objects) {
            if (battleObject.IsShip()) {
                Ship ship = (Ship)battleObject;
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
        foreach (BattleObject battleObject in objects) {
            if (battleObject.IsStation()) {
                Station station = (Station)battleObject;
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

    public bool ContainsObject(BattleObject battleObject) {
        return objects.Contains(battleObject);
    }


    public bool HasShip() {
        if (groupType == GroupType.Fleet && fleet.GetShips().Count > 0)
            return true;
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].IsShip())
                return true;
        }
        return false;
    }

    public bool HasStation() {
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].IsStation())
                return true;
        }
        return false;
    }

    public void SetShips(List<Ship> shipList) {
        ClearGroup();
        objects.AddRange(shipList);
        groupType = GroupType.Ships;
    }

    public void AddShips(List<Ship> shipList) {
        objects.AddRange(shipList);
    }

    public void AddShip(Ship ship) {
        objects.Add(ship);
        if (groupType == GroupType.Ship)
            groupType = GroupType.Ships;
        else if (groupType == GroupType.None)
            groupType = GroupType.Ship;
        else if (groupType == GroupType.Station || groupType == GroupType.Unit)
            groupType = GroupType.Units;
        else if (groupType == GroupType.Object)
            groupType = GroupType.Objects;
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
        objects.AddRange(unitList);
        groupType = GroupType.Units;
    }

    public void CopyGroup(SelectionGroup group) {
        this.groupType = group.groupType;
        foreach (var unit in group.objects) {
            objects.Add(unit);
        }
        this.fleet = group.fleet;
    }

    public void AddUnits(List<Unit> unitList) {
        objects.AddRange(unitList);
        groupType = GroupType.Units;
    }

    public void AddUnits(SelectionGroup unitGroup) {
        AddUnits(unitGroup.GetAllUnits());
    }

    public void AddUnit(Unit unit) {
        objects.Add(unit);
        if (groupType == GroupType.Object || groupType == GroupType.Objects)
            groupType = GroupType.Objects;
        else if (groupType == GroupType.None)
            groupType = GroupType.Unit;
        else groupType = GroupType.Units;
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
                objects.Remove(unit);
            }
        }
    }

    public void AddBattleObject(BattleObject battleObject) {
        objects.Add(battleObject);
        if (groupType == GroupType.None)
            groupType = GroupType.Object;
        else
            groupType = GroupType.Objects;
    }

    public void AddBattleObjects(List<BattleObject> battleObject) {
        objects.AddRange(battleObject);
        groupType = GroupType.Objects;
    }

    public void RemoveBattleObject(BattleObject battleObject) {
        if (battleObject.IsUnit()) { 
            RemoveUnit((Unit)battleObject);
        } else {
            objects.Remove(battleObject);
        }
    }

    /// <summary>
    /// Gets the first ship in the group.
    /// If the group is a fleet returns the first ship in the fleet
    /// </summary>
    /// <returns>the first ship in the group</returns>
    public Ship GetShip() {
        if (groupType == GroupType.Fleet)
            return fleet.GetShips()[0];
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].IsShip())
                return (Ship)objects[i];
        }
        return null;
    }

    /// <summary>
    /// Gets the first station in the group.
    /// </summary>
    /// <returns>the first station in the group</returns>
    public Station GetStation() {
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].IsStation())
                return (Station)objects[i];
        }
        return null;
    }

    //Takes out all non ship units in units
    public void ConvertToShips() {
        for (int i = 0; i < objects.Count; i++) {
            if ((Ship)objects[i] == null) {
                objects.RemoveAt(i);
            }
        }
    }

    public void SetShip(Ship ship) {
        if (ship != null) {
            ClearGroup();
            objects.Add(ship);
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
        return objects.Count;
    }

    public void RemoveAllNonCombatShips() {
        for (int i = objects.Count - 1; i >= 0; i--) {
            if (!objects[i].IsShip() || !((Ship)objects[i]).IsCombatShip()) {
                objects[i].SelectObject(UnitSelection.SelectionStrength.Unselected);
                objects.RemoveAt(i);
            }
        }
    }

    public void SelectAllBattleObjects(UnitSelection.SelectionStrength strength = UnitSelection.SelectionStrength.Unselected) {
        objects.ForEach(obj => obj.SelectObject(strength));
        if (fleet != null) {
            fleet.SelectFleet(strength);
        }
    }

    public void UnselectAllBattleObjects() {
        SelectAllBattleObjects(UnitSelection.SelectionStrength.Unselected);
    }

    public void GiveCommand(Command command, Command.CommandAction commandAction) {
        if (groupType == GroupType.Fleet) {
            fleet.FleetAI.AddFleetAICommand(command, commandAction);
            return;
        }
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].IsSpawned() && objects[i].IsShip()) {
                ((Ship)objects[i]).shipAI.AddUnitAICommand(command, commandAction);
            }
        }
    }

    public void ClearCommands() {
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].IsShip()) {
                ((Ship)objects[i]).shipAI.ClearCommands();
            }
        }
    }

    public void RemoveAnyUnitsNotInList(List<Unit> unitList) {
        for (int i = objects.Count - 1; i >= 0; i--) {
            if (!unitList.Contains(objects[i])) {
                objects[i].UnselectObject();
                objects.RemoveAt(i);
            }
        }
    }

    public void RemoveAnyNullUnits() {
        for (int i = objects.Count - 1; i >= 0; i--) {
            if (objects[i] == null || !objects[i].IsSpawned()) {
                objects.RemoveAt(i);
            }
        }
    }

    public int GetTotalUnitHealth() {
        int totalHealth = 0;
        if (groupType == GroupType.Fleet)
            totalHealth += fleet.GetTotalFleetHealth();
        totalHealth = objects.Sum(obj => { 
            if (obj.IsUnit()) return 0; 
            return ((Unit)obj).GetTotalHealth(); });
        return totalHealth;
    }

    public bool ContainsOnlyConstructionShips() {
        for(int i = 0;i < objects.Count;i++) {
            if (!objects[i].IsShip() || !((Ship)objects[i]).IsConstructionShip())
                return false;
        }
        return true;
    }

    public bool ContainsOnlyScienceShips() {
        for (int i = 0; i < objects.Count; i++) {
            if (!objects[i].IsShip() || !((Ship)objects[i]).IsScienceShip())
                return false;
        }
        return true;
    }

    public bool ContainsOnlyGasCollectionShips() {
        for (int i = 0; i < objects.Count; i++) {
            if (!objects[i].IsShip() || !((Ship)objects[i]).IsGasCollectorShip())
                return false;
        }
        return true;
    }
}
