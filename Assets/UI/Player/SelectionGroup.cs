using System.Collections.Generic;
using System.Linq;

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

    public List<BattleObjectUI> objects = new List<BattleObjectUI>();
    public FleetUI fleet;

    public void ClearGroup() {
        objects.Clear();
        fleet = null;
        groupType = GroupType.None;
    }

    public List<UnitUI> GetAllUnits() {
        if (groupType == GroupType.Fleet) {
            List<UnitUI> fleetUnits = new List<UnitUI>();
            foreach (var ship in fleet.GetShipsUI()) {
                fleetUnits.Add(ship);
            }

            return fleetUnits;
        }

        return objects.Where(obj => obj.battleObject.IsUnit()).Cast<UnitUI>().ToList();
    }

    public List<ShipUI> GetAllShips(Faction faction = null) {
        List<ShipUI> listOfShips = new List<ShipUI>();
        foreach (BattleObjectUI battleObject in objects) {
            if (battleObject.battleObject.IsShip()) {
                ShipUI ship = (ShipUI)battleObject;
                if (faction != null) {
                    if (ship.ship.faction == faction)
                        listOfShips.Add(ship);
                } else {
                    listOfShips.Add(ship);
                }
            }
        }

        return listOfShips;
    }

    public List<StationUI> GetAllStations(Faction faction = null) {
        List<StationUI> listOfStations = new List<StationUI>();
        foreach (BattleObjectUI battleObject in objects) {
            if (battleObject.battleObject.IsStation()) {
                StationUI station = (StationUI)battleObject;
                if (faction != null) {
                    if (station.station.faction == faction)
                        listOfStations.Add(station);
                } else {
                    listOfStations.Add(station);
                }
            }
        }

        return listOfStations;
    }

    public bool ContainsObject(BattleObjectUI battleObject) {
        return objects.Contains(battleObject);
    }


    public bool HasShip() {
        if (groupType == GroupType.Fleet && fleet.GetShipsUI().Any())
            return true;
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].battleObject.IsShip())
                return true;
        }

        return false;
    }

    public bool HasStation() {
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].battleObject.IsStation())
                return true;
        }

        return false;
    }

    public void SetShips(List<ShipUI> shipList) {
        ClearGroup();
        objects.AddRange(shipList);
        groupType = GroupType.Ships;
    }

    public void AddShips(List<ShipUI> shipList) {
        shipList.ForEach((s) => AddShip(s));
    }

    public void AddShip(ShipUI ship) {
        objects.Add(ship);
        if (groupType == GroupType.Ship)
            groupType = GroupType.Ships;
        else if (groupType == GroupType.None)
            groupType = GroupType.Ship;
        else if (groupType == GroupType.Station || groupType == GroupType.Unit)
            groupType = GroupType.Units;
        else if (groupType == GroupType.Object)
            groupType = GroupType.Objects;
        else if (groupType == GroupType.Fleet) {
            groupType = GroupType.Ships;
            objects.AddRange(fleet.GetShipsUI());
            fleet = null;
        }
    }

    public List<ShipUI> GetShipsOfClass(Ship.ShipClass shipClass) {
        List<ShipUI> shipList = new List<ShipUI>();
        foreach (var ship in GetAllShips()) {
            if (ship.ship.GetShipClass() == shipClass) {
                shipList.Add(ship);
            }
        }

        return shipList;
    }

    public void SetUnits(List<UnitUI> unitList) {
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

    public void AddUnits(List<UnitUI> unitList) {
        unitList.ForEach((u) => AddUnit(u));
    }

    public void AddUnits(SelectionGroup unitGroup) {
        AddUnits(unitGroup.GetAllUnits().Where(u => !objects.Contains(u)).ToList());
    }

    public void AddUnit(UnitUI unit) {
        objects.Add(unit);
        if (groupType == GroupType.Object || groupType == GroupType.Objects)
            groupType = GroupType.Objects;
        else if (groupType == GroupType.None)
            groupType = GroupType.Unit;
        else if (groupType == GroupType.Fleet) {
            groupType = GroupType.Units;
            objects.AddRange(fleet.GetShipsUI());
            fleet = null;
        } else groupType = GroupType.Units;
    }

    public void RemoveUnit(UnitUI unit) {
        List<UnitUI> groupUnits = GetAllUnits();
        if (groupUnits.Contains(unit)) {
            if (groupType == GroupType.Fleet) {
                groupUnits.Remove(unit);
                ClearGroup();
                for (int i = 0; i < groupUnits.Count; i++) {
                    AddShip((ShipUI)groupUnits[i]);
                }
            } else {
                objects.Remove(unit);
            }
        }
    }

    public void AddBattleObject(BattleObjectUI battleObject) {
        objects.Add(battleObject);
        if (groupType == GroupType.None)
            groupType = GroupType.Object;
        if (groupType == GroupType.Fleet) {
            objects.AddRange(fleet.GetShipsUI());
            fleet = null;
            groupType = GroupType.Objects;
        } else groupType = GroupType.Objects;
    }

    public void AddBattleObjects(List<BattleObjectUI> battleObject) {
        battleObject.ForEach((b) => AddBattleObject(b));
    }

    public void RemoveBattleObject(BattleObjectUI battleObject) {
        if (battleObject.battleObject.IsUnit()) {
            RemoveUnit((UnitUI)battleObject);
        } else {
            objects.Remove(battleObject);
        }
    }

    /// <summary>
    /// Gets the first ship in the group.
    /// If the group is a fleet returns the first ship in the fleet
    /// </summary>
    /// <returns>the first ship in the group</returns>
    public ShipUI GetShip() {
        if (groupType == GroupType.Fleet)
            return fleet.GetShipsUI().First();
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].battleObject.IsShip())
                return (ShipUI)objects[i];
        }

        return null;
    }

    /// <summary>
    /// Gets the first station in the group.
    /// </summary>
    /// <returns>the first station in the group</returns>
    public StationUI GetStation() {
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].battleObject.IsStation())
                return (StationUI)objects[i];
        }

        return null;
    }

    //Takes out all non ship units in units
    public void ConvertToShips() {
        for (int i = 0; i < objects.Count; i++) {
            if ((ShipUI)objects[i] == null) {
                objects.RemoveAt(i);
            }
        }
    }

    public void SetShip(ShipUI ship) {
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

    public void SetFleet(FleetUI fleet) {
        ClearGroup();
        groupType = GroupType.Fleet;
        this.fleet = fleet;
    }

    public int GetUnitCount() {
        if (groupType == GroupType.Fleet)
            return fleet.GetShipsUI().Count();
        return objects.Count;
    }

    public void RemoveAllNonCombatShips() {
        // for (int i = objects.Count - 1; i >= 0; i--) {
        //     if (!objects[i].IsShip() || !((Ship)objects[i]).IsCombatShip()) {
        //         objects[i].SelectObject(UnitSelection.SelectionStrength.Unselected);
        //         objects.RemoveAt(i);
        //     }
        // }
    }

    public void SelectAllBattleObjects(UnitSelection.SelectionStrength strength = UnitSelection.SelectionStrength.Unselected) {
        objects.ForEach(obj => obj.SelectObject(strength));
        if (fleet != null) {
            fleet.SelectObject(strength);
        }
    }

    public void UnselectAllBattleObjects() {
        SelectAllBattleObjects(UnitSelection.SelectionStrength.Unselected);
    }

    public void GiveCommand(Command command, Command.CommandAction commandAction) {
        if (groupType == GroupType.Fleet) {
            fleet.fleet.FleetAI.AddFleetAICommand(command, commandAction);
            return;
        }

        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].battleObject.IsSpawned() && objects[i].battleObject.IsShip()) {
                ((ShipUI)objects[i]).ship.shipAI.AddUnitAICommand(command, commandAction);
            }
        }
    }

    public void ClearCommands() {
        for (int i = 0; i < objects.Count; i++) {
            if (objects[i].battleObject.IsShip()) {
                ((ShipUI)objects[i]).ship.shipAI.ClearCommands();
            }
        }
    }

    public void RemoveAnyUnitsNotInHashSet(HashSet<Unit> unitList) {
        for (int i = objects.Count - 1; i >= 0; i--) {
            if (!objects[i].battleObject.IsUnit() || !unitList.Contains((Unit)objects[i].battleObject)) {
                objects[i].UnselectObject();
                objects.RemoveAt(i);
            }
        }
    }

    public void RemoveAnyNullUnits() {
        for (int i = objects.Count - 1; i >= 0; i--) {
            if (objects[i] == null || !objects[i].battleObject.IsSpawned()) {
                objects.RemoveAt(i);
            }
        }
    }

    public int GetTotalUnitHealth() {
        int totalHealth = 0;
        if (groupType == GroupType.Fleet)
            totalHealth += fleet.fleet.GetTotalFleetHealth();
        totalHealth = objects.Sum(obj => {
            if (obj.battleObject.IsUnit()) return 0;
            return ((UnitUI)obj).unit.GetTotalHealth();
        });
        return totalHealth;
    }

    public bool ContainsOnlyConstructionShips() {
        return objects.All(obj => obj.battleObject.IsShip() && ((ShipUI)obj).ship.IsConstructionShip());
    }

    public bool ContainsOnlyScienceShips() {
        return objects.All(obj => obj.battleObject.IsShip() && ((ShipUI)obj).ship.IsScienceShip());
    }

    public bool ContainsOnlyGasCollectionShips() {
        return objects.All(obj => obj.battleObject.IsShip() && ((ShipUI)obj).ship.IsGasCollectorShip());
    }

    public bool ContainsOnlyTransportShips() {
        return objects.All(obj => obj.battleObject.IsShip() && ((ShipUI)obj).ship.IsTransportShip());
    }

    public bool ContainsOnlyColonizerShips() {
        return objects.All(obj => obj.battleObject.IsShip() && ((ShipUI)obj).ship.IsColonizerShip());
    }
}
