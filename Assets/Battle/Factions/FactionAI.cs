using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FactionAI {
    public bool autoResearch;

    public FactionAI(BattleManager battleManager, Faction faction) {
        this.battleManager = battleManager;
        this.faction = faction;
        idleShips = new HashSet<Ship>(40);
        autoResearch = true;
    }
    public BattleManager battleManager { get; }
    public Faction faction { get; protected set; }

    [field: SerializeField] public HashSet<Ship> idleShips { get; protected set; }

    public virtual void UpdateFactionAI(float deltaTime) {
        if (autoResearch)
            faction.UpdateFactionResearch();
    }

    public virtual void OnStationBuilt(Station station) { }

    public virtual void OnShipBuilt(Ship ship) { }

    public virtual void AddIdleShip(Ship ship) {
        if (!idleShips.Contains(ship))
            idleShips.Add(ship);
    }

    public virtual void RemoveShip(Ship ship) {
        idleShips.Remove(ship);
    }

    public virtual void RemoveFleet(Fleet fleet) { }

    public virtual double GetSellCostOfMetal() {
        return 2.4f;
    }

    protected float GetTimeScale() {
        return battleManager.timeScale;
    }

    public virtual Station GetFleetCommand() {
        return null;
    }
}
