using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionAI : MonoBehaviour {
    public Faction faction { protected set; get; }
    public bool autoResearch;

    [SerializeField] protected List<Ship> idleShips;
    [SerializeField] public List<UnitGroup> newNearbyEnemyUnits;

    public virtual void SetupFactionAI(Faction faction) {
        this.faction = faction;
        idleShips = new List<Ship>(10);
        autoResearch = true;
    }

    public virtual void GenerateFactionAI() {

    }

    public virtual void UpdateFactionAI(float deltaTime) {
        if (autoResearch)
            faction.UpdateFactionResearch();
    }

    public virtual void OnStationBuilt(Station station) {

    }

    public virtual void OnShipBuilt(Ship ship) {

    }

    public virtual void OnShipBuiltForAnotherFaction(Ship ship, Faction faction) {
    }

    public virtual void AddIdleShip(Ship ship) {
        idleShips.Add(ship);
    }

    public virtual void RemoveIdleShip(Ship ship) {
        idleShips.Remove(ship);
    }
}
