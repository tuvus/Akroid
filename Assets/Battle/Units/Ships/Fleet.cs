using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class Fleet : ObjectGroup<Ship> {
    Faction faction;
    public FleetAI FleetAI { get; private set; }
    string fleetName;
    public List<Ship> ships;
    public float minFleetSpeed { get; private set; }
    public float maxWeaponRange { get; private set; }

    public List<Unit> enemyUnitsInRange { get; protected set; }
    public List<float> enemyUnitsInRangeDistance { get; protected set; }

    public void SetupFleet(Faction faction, string fleetName, Ship ship) {
        SetupFleet(faction, fleetName, new List<Ship>() { ship });
    }

    public void SetupFleet(Faction faction, string fleetName, List<Ship> ships) {
        this.faction = faction;
        this.fleetName = fleetName;
        this.ships = new List<Ship>(ships.Count * 2);
        for (int i = 0; i < ships.Count; i++) {
            AddShip(ships[i], false);
        }
        SetupObjectGroup(ships);
        enemyUnitsInRange = new List<Unit>(20);
        enemyUnitsInRangeDistance = new List<float>(20);
        minFleetSpeed = GetMinShipSpeed();
        maxWeaponRange = GetMaxTurretRange();
        FleetAI = GetComponent<FleetAI>();
        FleetAI.SetupFleetAI(this);
    }

    public void DisbandFleet() {
        foreach (Ship ship in ships) {
            ship.SetIdle();
            ship.shipAI.ClearCommands();
            ship.fleet = null;
            faction.unitsNotInFleet.Add(ship);
        }
        faction.RemoveFleet(this);
        Destroy(gameObject);
    }

    public void AddShip(Ship ship, bool setMinSpeed = true) {
        ships.Add(ship);
        AddBattleObject(ship);
        if (ship.fleet != null) {
            ship.fleet.RemoveShip(ship);
        }
        faction.unitsNotInFleet.Remove(ship);
        ship.fleet = this;
        if (setMinSpeed)
            minFleetSpeed = GetMinShipSpeed();
        maxWeaponRange = GetMaxTurretRange();
    }

    public void RemoveShip(Ship ship) {
        ships.Remove(ship);
        RemoveBattleObject(ship);
        ship.fleet = null;
        faction.unitsNotInFleet.Add(ship);
        if (ships.Count == 0) {
            DisbandFleet();
        } else {
            minFleetSpeed = GetMinShipSpeed();
            maxWeaponRange = GetMaxTurretRange();
        }
    }

    public void MergeIntoFleet(Fleet fleet) {
        for (int i = ships.Count - 1; i >= 0; i--) {
            fleet.AddShip(ships[i]);
        }
        fleet.FleetAI.AddFormationCommand(Command.CommandAction.AddToBegining);
    }

    public void UpdateFleet(float deltaTime) {
        UpdateObjectGroup();
        FindEnemies();
        FleetAI.UpdateAI(deltaTime);
        transform.position = GetPosition();
    }

    //Vector2 CalculateFleetCenter() {
    //    Vector2 sum = ships[0].GetPosition();
    //    for (int i = 1; i < ships.Count; i++) {
    //        sum += ships[i].GetPosition();
    //    }
    //    return sum / ships.Count;
    //}

    void FindEnemies() {
        Profiler.BeginSample("FindingEnemies");
        enemyUnitsInRange.Clear();
        enemyUnitsInRangeDistance.Clear();
        foreach (var enemyFaction in faction.enemyFactions) {
            if (Vector2.Distance(GetPosition(), enemyFaction.GetPosition()) > GetSize() + maxWeaponRange * 2 + enemyFaction.GetSize())
                continue;
            for (int i = 0; i < enemyFaction.unitsNotInFleet.Count; i++) {
                FindUnit(enemyFaction.unitsNotInFleet[i]);
            }
            for (int i = 0; i < enemyFaction.fleets.Count; i++) {
                Fleet targetFleet = enemyFaction.fleets[i];
                if (Vector2.Distance(GetPosition(), targetFleet.GetPosition()) <= maxWeaponRange * 2 + GetSize() + targetFleet.GetSize()) {
                    for (int f = 0; f < targetFleet.ships.Count; f++) {
                        FindUnit(targetFleet.ships[f]);
                    }
                }
            }
        }
        Profiler.EndSample();
    }

    void FindUnit(Unit targetUnit) {
        if (targetUnit == null || !targetUnit.IsTargetable())
            return;
        float distance = Vector2.Distance(GetPosition(), targetUnit.GetPosition());
        if (distance <= maxWeaponRange * 2 + GetSize() + targetUnit.GetSize()) {
            for (int f = 0; f < enemyUnitsInRangeDistance.Count; f++) {
                if (enemyUnitsInRangeDistance[f] >= distance) {
                    enemyUnitsInRangeDistance.Insert(f, distance);
                    enemyUnitsInRange.Insert(f, targetUnit);
                    return;
                }
            }
            //Has not been added yet
            enemyUnitsInRange.Add(targetUnit);
            enemyUnitsInRangeDistance.Add(distance);
        }
    }

    public void NextShipsCommand() {
        for (int i = 0; i < ships.Count; i++) {
            ships[i].shipAI.NextCommand();
        }
    }

    public bool IsFleetIdle() {
        if (FleetAI.commands.Count == 0 || FleetAI.commands[0].commandType == Command.CommandType.Idle)
            return AreShipsIdle();
        return false;
    }

    public bool AreShipsIdle() {
        for (int i = 0; i < ships.Count; i++) {
            if (!ships[i].IsIdle())
                return false;
        }
        return true;
    }

    public int GetTotalFleetHealth() {
        int totalHealth = 0;
        for (int i = 0; i < ships.Count; i++) {
            totalHealth += ships[i].GetTotalHealth();
        }
        return totalHealth;
    }

    public int GetFleetHealth() {
        int health = 0;
        for (int i = 0; i < ships.Count; i++) {
            health += ships[i].GetHealth();
        }
        return health;
    }

    public int GetMaxFleetHealth() {
        int maxHealth = 0;
        for (int i = 0; i < ships.Count; i++) {
            maxHealth += ships[i].GetMaxHealth();
        }
        return maxHealth;
    }


    public int GetFleetSheilds() {
        int shields = 0;
        for (int i = 0; i < ships.Count; i++) {
            shields += ships[i].GetShields();
        }
        return shields;
    }

    public int GetMaxFleetShields() {
        int maxShields = 0;
        for (int i = 0; i < ships.Count; i++) {
            maxShields += ships[i].GetMaxShields();
        }
        return maxShields;
    }
    Unit GetClosestEnemyUnitInRadius(float radius) {
        Unit targetUnit = null;
        float distance = 0;
        //for (int i = 0; i < ship.enemyUnitsInRange.Count; i++) {
        //    Unit tempUnit = ship.enemyUnitsInRange[i];
        //    float tempDistance = Vector2.Distance(ship.transform.position, tempUnit.transform.position);
        //    if (tempDistance <= radius && (targetUnit == null || tempDistance < distance)) {
        //        targetUnit = tempUnit;
        //        distance = tempDistance;
        //    }
        //}
        return targetUnit;
    }

    /// <summary>
    /// Returns the fleet of the closest enemy ship with a fleet.
    /// </summary>
    /// <returns>the closest Enemy fleet</returns>
    public Fleet GetNearbyEnemyFleet() {
        for (int i = 0; i < enemyUnitsInRange.Count; i++) {
            if (enemyUnitsInRange[i].IsShip() && ((Ship)enemyUnitsInRange[i]).fleet) {
                return ((Ship)enemyUnitsInRange[i]).fleet;
            }
        }
        return null;
    }

    public bool HasNearbyEnemyCombatShip() {
        for (int i = 0; i < enemyUnitsInRange.Count; i++) {
            if (enemyUnitsInRange[i].IsShip() && ((Ship)enemyUnitsInRange[i]).IsCombatShip()) {
                return true;
            }
        }
        return false;
    }

    public float GetMinShipSpeed() {
        float minSpeed = float.MaxValue;
        for (int i = 0; i < ships.Count; i++) {
            minSpeed = math.min(ships[i].GetSpeed(), minSpeed);
        }
        return minSpeed;
    }

    public float GetMaxShipSize() {
        float maxShipSize = 0;
        for (int i = 0; i < ships.Count; i++) {
            maxShipSize = math.max(maxShipSize, ships[i].GetSize());
        }
        return maxShipSize;
    }

    public float GetMaxTurretRange() {
        float maxTurretRange = 0;
        for (int i = 0; i < ships.Count; i++) {
            maxTurretRange = math.max(maxTurretRange, ships[i].GetMaxWeaponRange());
        }
        return maxTurretRange;
    }

    public bool IsDockedWithStation(Station station) {
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].dockedStation != station)
                return false;
        }
        return true;
    }

    public List<Ship> GetAllShips() {
        return ships;
    }

    public void SelectFleet(UnitSelection.SelectionStrength strength = UnitSelection.SelectionStrength.Unselected) {
        foreach (Ship ship in ships) {
            ship.SelectUnit(strength);
        }
    }

    public void UnselectFleet() {
        SelectFleet(UnitSelection.SelectionStrength.Unselected);
    }

    public string GetFleetName() {
        return fleetName;
    }
}