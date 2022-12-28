using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Fleet : MonoBehaviour {
    Faction faction;
    public FleetAI FleetAI { get; private set; }
    string fleetName;
    public List<Ship> ships;
    protected Vector2 position;
    protected float size;
    protected float maxSize;
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
        enemyUnitsInRange = new List<Unit>(20);
        enemyUnitsInRangeDistance = new List<float>(20);
        minFleetSpeed = GetMinShipSpeed();
        maxWeaponRange = GetMaxTurretRange();
        FleetAI = GetComponent<FleetAI>();
        FleetAI.SetupFleetAI(this);
    }

    public void DisbandFleet() {
        foreach (Ship ship in ships) {
            ship.fleet = null;
        }
        faction.RemoveFleet(this);
        Destroy(gameObject);
    }

    public void AddShip(Ship ship, bool setMinSpeed = true) {
        ships.Add(ship);
        if (ship.fleet != null) {
            ship.fleet.RemoveShip(ship);
        }
        ship.fleet = this;
        if (setMinSpeed)
            minFleetSpeed = GetMinShipSpeed();
        maxWeaponRange = GetMaxTurretRange();
    }

    public void RemoveShip(Ship ship) {
        ships.Remove(ship);
        ship.fleet = null;
        if (ships.Count == 0) {
            DisbandFleet();
        } else {
            minFleetSpeed = GetMinShipSpeed();
            maxWeaponRange = GetMaxTurretRange();
        }
    }

    public void UpdateFleet(float deltaTime) {
        position = CalculateFleetCenter();
        size = CalculateFleetSize();
        maxSize = calculateFleetMaxSize();
        FindEnemies();
        FleetAI.UpdateAI(deltaTime);
    }

    Vector2 CalculateFleetCenter() {
        Vector2 sum = ships[0].GetPosition();
        for (int i = 1; i < ships.Count; i++) {
            sum += ships[i].GetPosition();
        }
        return sum / ships.Count;
    }

    float CalculateFleetSize() {
        float size = ships[0].GetSize();
        for (int i = 1; i < ships.Count; i++) {
            size = Math.Max(size, Vector2.Distance(GetPosition(), ships[i].GetPosition()) + ships[i].GetSize());
        }
        return size;
    }

    float calculateFleetMaxSize() {
        float maxSize = 0;
        for (int i = 0; i < ships.Count; i++) {
            maxSize = Math.Max(maxSize, Vector2.Distance(GetPosition(), ships[i].GetPosition()) + ships[i].GetSize());
        }
        return maxSize;
    }

    void FindEnemies() {
        Profiler.BeginSample("FindingEnemies");
        enemyUnitsInRange.Clear();
        enemyUnitsInRangeDistance.Clear();
        foreach (var enemyFaction in faction.enemyFactions) {
            if (Vector2.Distance(GetPosition(), enemyFaction.factionPosition) > GetMaxSize() + maxWeaponRange * 2 + enemyFaction.factionUnitsSize)
                continue;
            for (int i = 0; i < enemyFaction.units.Count; i++) {
                Unit targetUnit = enemyFaction.units[i];
                if (targetUnit == null || !targetUnit.IsTargetable())
                    continue;
                float distance = Vector2.Distance(GetPosition(), targetUnit.GetPosition());
                if (distance <= maxWeaponRange * 2) {
                    bool added = false;
                    for (int f = 0; f < enemyUnitsInRangeDistance.Count; f++) {
                        if (enemyUnitsInRangeDistance[f] >= distance) {
                            enemyUnitsInRangeDistance.Insert(f, distance);
                            enemyUnitsInRange.Insert(f, targetUnit);
                            added = true;
                            break;
                        }
                    }
                    if (!added) {
                        enemyUnitsInRange.Add(targetUnit);
                        enemyUnitsInRangeDistance.Add(distance);
                    }
                }
            }
        }
        Profiler.EndSample();
    }

    public void NextShipsCommand() {
        for (int i = 0; i < ships.Count; i++) {
            ships[i].shipAI.NextCommand();
        }
    }

    public bool IsFleetIdle() {
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

    public Vector2 GetPosition() {
        return position;
    }

    public float GetSize() {
        return size;
    }

    public float GetMaxSize() {
        return maxSize;
    }

    public float GetMinShipSpeed() {
        float minSpeed = float.MaxValue;
        for (int i = 0; i < ships.Count; i++) {
            minSpeed = Math.Min(ships[i].GetSpeed(), minSpeed);
        }
        return minSpeed;
    }

    public float GetMaxShipSize() {
        float maxShipSize = 0;
        for (int i = 0; i < ships.Count; i++) {
            maxShipSize = Math.Max(maxShipSize, ships[i].GetSize());
        }
        return maxShipSize;
    }

    public float GetMaxTurretRange() {
        float maxTurretRange = 0;
        for (int i = 0; i < ships.Count; i++) {
            maxTurretRange = Math.Max(maxTurretRange, ships[i].GetMaxWeaponRange());
        }
        return maxTurretRange;
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
}
