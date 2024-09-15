using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShipGroup : UnitGroup {
    [field: SerializeField] public HashSet<Ship> ships { get; private set; }
    public List<Fleet> sentFleets;

    public ShipGroup(BattleManager battleManager, HashSet<Ship> objects, bool deleteWhenEmpty, bool setupGroupPositionAndSize = true,
        bool changeSizeIndicatorPosition = false) :
        base(battleManager, objects.Cast<Unit>().ToHashSet(), deleteWhenEmpty, setupGroupPositionAndSize, changeSizeIndicatorPosition) {
        ships = objects;
        sentFleets = new List<Fleet>();
    }

    public void SetupTargetGroup(HashSet<Ship> ships, bool deleteWhenEmpty) {
        //SetupObjectGroup(ships, deleteWhenEmpty);
        UpdateObjectGroup();
    }

    public virtual void AddShip(Ship ship) {
        base.AddUnit(ship);
        UpdateObjectGroup();
    }

    public virtual void RemoveShip(Ship ship) {
        base.RemoveUnit(ship);
        ships.Remove(ship);
    }

    public override void AddBattleObject(BattleObject battleObject) {
        base.AddBattleObject(battleObject);
        ships.Add((Ship)battleObject);
    }

    public override void RemoveBattleObject(BattleObject battleObject) {
        base.RemoveBattleObject(battleObject);
        ships.Remove((Ship)battleObject);
    }

    /// <summary>
    /// Finds if the fleets sent by sentFaction is stronger than this fleet.
    /// If sentFaction is null add up all fleets regardless of their faction.
    /// </summary>
    /// <param name="sentFaction">the fleets with faction ownership that should be counted</param>
    /// <returns>true if the sent fleets has a strength greater than this fleet</returns>
    public bool IsSentFleetsStronger(Faction sentFaction = null) {
        int totalHealth = 0;
        for (int i = 0; i < sentFleets.Count; i++) {
            if (sentFaction == null || sentFleets[i].faction == sentFaction)
                totalHealth += sentFleets[i].GetTotalFleetHealth();
        }

        return totalHealth >= GetTotalGroupHealth();
    }

    public List<Ship> GetShips() {
        return ships.ToList();
    }
}
