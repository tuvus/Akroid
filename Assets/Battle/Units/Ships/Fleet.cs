using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public class Fleet : ShipGroup {
    private readonly string fleetName;

    public Fleet(BattleManager battleManger, Faction faction, string fleetName, Ship ship) :
        this(battleManger, faction, fleetName, new HashSet<Ship> { ship }) { }

    public Fleet(BattleManager battleManager, Faction faction, string fleetName, HashSet<Ship> ships) :
        base(battleManager, new HashSet<Ship>(), true) {
        this.faction = faction;
        this.fleetName = fleetName;
        foreach (Ship ship in ships) {
            AddShip(ship);
        }

        enemyUnitsInRange = new List<Unit>(20);
        enemyUnitsInRangeDistance = new List<float>(20);
        minShipSpeed = GetMinShipSpeed();
        maxWeaponRange = GetMaxTurretRange();
        fleetAI = new FleetAI(this);
    }
    public Faction faction { get; }
    public FleetAI fleetAI { get; }
    public float minShipSpeed { get; private set; }
    public float maxWeaponRange { get; private set; }

    public List<Unit> enemyUnitsInRange { get; protected set; }
    public List<float> enemyUnitsInRangeDistance { get; protected set; }

    public void DisbandFleet() {
        faction.RemoveFleet(this);
        foreach (Ship ship in ships.ToList()) {
            ship.SetIdle();
            ship.shipAI.ClearCommands();
            ship.fleet = null;
            RemoveUnit(ship);
        }
    }

    public override void AddShip(Ship ship) {
        AddShip(ship);
    }

    public void AddShip(Ship ship, bool setMinSpeed = true) {
        if (ship.fleet != null) ship.fleet.RemoveShip(ship);
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
        List<Ship> shipsToMerge = ships.ToList();
        DisbandFleet();
        foreach (Ship ship in shipsToMerge) {
            fleet.AddShip(ship);
        }

        fleet.fleetAI.AddFormationCommand(Command.CommandAction.AddToBeginning);
    }

    public void UpdateFleet(float deltaTime) {
        fleetAI.UpdateAI(deltaTime);
        for (int i = sentFleets.Count - 1; i >= 0; i--) {
            if (sentFleets[i] == null) {
                sentFleets.RemoveAt(i);
            }
        }
    }

    public void FindEnemies() {
        Profiler.BeginSample("FindingEnemies");
        enemyUnitsInRange.Clear();
        enemyUnitsInRangeDistance.Clear();
        float distanceFromFactionCenter =
            Vector2.Distance(faction.GetPosition(), GetPosition()) + maxWeaponRange * 2 + GetSize();
        for (int i = 0; i < faction.closeEnemyGroups.Count; i++) {
            if (faction.closeEnemyGroupsDistance[i] > distanceFromFactionCenter)
                break;
            FindEnemyGroup(faction.closeEnemyGroups[i]);
        }

        Profiler.EndSample();
    }

    private void FindEnemyGroup(UnitGroup targetGroup) {
        foreach (Unit battleObject in targetGroup.battleObjects) {
            FindEnemyUnit(battleObject);
        }
    }

    private void FindEnemyUnit(Unit targetUnit) {
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
        foreach (Ship ship in ships) {
            ship.shipAI.NextCommand();
        }
    }

    public bool IsFleetIdle() {
        if (fleetAI.commands.Count == 0 || fleetAI.commands[0].commandType == Command.CommandType.Idle)
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
    ///     Returns the fleet of the closest enemy ship with a fleet.
    /// </summary>
    /// <returns>the closest Enemy fleet</returns>
    public Fleet GetNearbyEnemyFleet() {
        foreach (Unit enemyUnit in enemyUnitsInRange) {
            if (enemyUnit.IsShip() && ((Ship)enemyUnit).fleet != null) {
                return ((Ship)enemyUnit).fleet;
            }
        }

        return null;
    }

    public bool HasNearbyEnemyCombatShip() {
        foreach (Unit enemyUnit in enemyUnitsInRange) {
            if (enemyUnit.IsShip() && ((Ship)enemyUnit).IsCombatShip()) {
                return true;
            }
        }

        return false;
    }

    public float GetMinShipSpeed() {
        return ships.Min(s => s.speed);
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

    public override bool IsFleet() {
        return true;
    }

    public string GetFleetName() {
        return fleetName;
    }

    public override string GetName() {
        return fleetName;
    }
}
