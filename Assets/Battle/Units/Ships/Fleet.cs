using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class Fleet : ShipGroup {
    public Faction faction { get; private set; }
    public FleetAI FleetAI { get; private set; }
    string fleetName;
    public float minShipSpeed { get; private set; }
    public float maxWeaponRange { get; private set; }

    public List<Unit> enemyUnitsInRange { get; protected set; }
    public List<float> enemyUnitsInRangeDistance { get; protected set; }

    public void SetupFleet(BattleManager battleManger, Faction faction, string fleetName, Ship ship) {
        SetupFleet(battleManger, faction, fleetName, new HashSet<Ship>() { ship });
    }

    public void SetupFleet(BattleManager battleManger, Faction faction, string fleetName, HashSet<Ship> ships) {
        this.faction = faction;
        this.fleetName = fleetName;
        SetupShipGroup(battleManager, ships, true);
        enemyUnitsInRange = new List<Unit>(20);
        enemyUnitsInRangeDistance = new List<float>(20);
        minShipSpeed = GetMinShipSpeed();
        maxWeaponRange = GetMaxTurretRange();
        FleetAI = GetComponent<FleetAI>();
        FleetAI.SetupFleetAI(this);
    }

    public void DisbandFleet() {
        faction.RemoveFleet(this);
        foreach (var ship in ships) {
            ship.SetIdle();
            ship.shipAI.ClearCommands();
            ship.fleet = null;
            RemoveUnit(ship);
        }
    }

    public override void AddShip(Ship ship) {
        AddShip(ship, true);
    }

    public void AddShip(Ship ship, bool setMinSpeed = true) {
        base.AddShip(ship);
        ship.fleet = this;
        if (setMinSpeed)
            minShipSpeed = GetMinShipSpeed();
        maxWeaponRange = GetMaxTurretRange();
    }

    public override void RemoveShip(Ship ship) {
        base.RemoveShip(ship);
        ship.fleet = null;
        if (ships.Count == 0) {
            DisbandFleet();
        } else {
            minShipSpeed = GetMinShipSpeed();
            maxWeaponRange = GetMaxTurretRange();
        }
    }

    public void MergeIntoFleet(Fleet fleet) {
        if (fleet == this)
            Debug.LogError("Merging a fleet into itself");
        foreach (var ship in ships) {
            fleet.AddShip(ship);
        }
        fleet.FleetAI.AddFormationCommand(Command.CommandAction.AddToBegining);
    }

    public void UpdateFleet(float deltaTime) {
        UpdateObjectGroup();
        FindEnemies();
        FleetAI.UpdateAI(deltaTime);
        transform.position = GetPosition();
        for (int i = sentFleets.Count - 1; i >= 0; i--) {
            if (sentFleets[i] == null) {
                sentFleets.RemoveAt(i);
            }
        }
    }

    void FindEnemies() {
        Profiler.BeginSample("FindingEnemies");
        enemyUnitsInRange.Clear();
        enemyUnitsInRangeDistance.Clear();
        float distanceFromFactionCenter = Vector2.Distance(faction.GetPosition(), GetPosition()) + maxWeaponRange * 2 + GetSize();
        for (int i = 0; i < faction.closeEnemyGroups.Count; i++) {
            if (faction.closeEnemyGroupsDistance[i] > distanceFromFactionCenter)
                break;
            FindEnemyGroup(faction.closeEnemyGroups[i]);

        }
        Profiler.EndSample();
    }

    void FindEnemyGroup(UnitGroup targetGroup) {
        foreach (var battleObject in targetGroup.battleObjects) {
            FindEnemyUnit(battleObject);
        }
    }

    void FindEnemyUnit(Unit targetUnit) {
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
        foreach (var ship in ships) {
            ship.shipAI.NextCommand();
        }
    }

    public bool IsFleetIdle() {
        if (FleetAI.commands.Count == 0 || FleetAI.commands[0].commandType == Command.CommandType.Idle)
            return AreShipsIdle();
        return false;
    }

    public bool AreShipsIdle() {
        return ships.All(s => s.IsIdle());
    }

    public int GetTotalFleetHealth() {
        return ships.Sum(s => s.GetTotalHealth());
    }

    public int GetFleetHealth() {
        return ships.Sum(s => s.GetHealth());
    }

    public int GetMaxFleetHealth() {
        return ships.Sum(s => s.GetMaxHealth());
    }


    public int GetFleetShields() {
        return ships.Sum(s => s.GetShields());
    }

    public int GetMaxFleetShields() {
        return ships.Sum(s => s.GetMaxShields());
    }

    /// <summary>
    /// Returns the fleet of the closest enemy ship with a fleet.
    /// </summary>
    /// <returns>the closest Enemy fleet</returns>
    public Fleet GetNearbyEnemyFleet() {
        foreach (var enemyUnit in enemyUnitsInRange) {
            if (enemyUnit.IsShip() && ((Ship)enemyUnit).fleet) {
                return ((Ship)enemyUnit).fleet;
            }
        }
        return null;
    }

    public bool HasNearbyEnemyCombatShip() {
        foreach (var enemyUnit in enemyUnitsInRange) {
            if (enemyUnit.IsShip() && ((Ship)enemyUnit).IsCombatShip()) {
                return true;
            }
        }
        return false;
    }

    public float GetMinShipSpeed() {
        return ships.Min(s => s.GetSpeed());
    }

    public float GetMaxShipSize() {
        return ships.Max(s => s.GetSize());
    }

    public float GetMaxTurretRange() {
        return ships.Max(s => s.GetMaxWeaponRange());
    }

    public bool IsDockedWithStation(Station station) {
        return ships.All(s => s.dockedStation == station);
    }

    public void SelectFleet(UnitSelection.SelectionStrength strength = UnitSelection.SelectionStrength.Unselected) {
        foreach (Ship ship in ships) {
            ship.SelectObject(strength);
        }
    }

    public void UnselectFleet() {
        SelectFleet(UnitSelection.SelectionStrength.Unselected);
    }

    public override bool IsFleet() {
        return true;
    }

    public string GetFleetName() {
        return fleetName;
    }
}