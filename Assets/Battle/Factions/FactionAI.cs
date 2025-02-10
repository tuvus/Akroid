using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FactionAI {
    public BattleManager battleManager { get; private set; }
    public Faction faction { get; protected set; }
    public bool autoResearch;
    public float attackSpeed = 3f;
    public float attackTime;
    public float attackStrength = 0.1f;

    [field: SerializeField] public HashSet<Ship> idleShips { get; protected set; }

    public FactionAI(BattleManager battleManager, Faction faction) {
        this.battleManager = battleManager;
        this.faction = faction;
        idleShips = new HashSet<Ship>(40);
        autoResearch = true;
    }

    public virtual void UpdateFactionAI(float deltaTime) {
        if (autoResearch)
            faction.UpdateFactionResearch();
        attackTime = math.max(0, attackTime - deltaTime);
        if (attackTime <= 0) {
            foreach (Faction enemy in faction.enemyFactions) {
                foreach (Planet planet in faction.planets) {
                    PlanetFaction planetFaction = planet.planetFactions[faction];
                    if (planetFaction.force > 0 && planet.planetFactions.ContainsKey(enemy) &&
                        planet.planetFactions[enemy].territory.GetTotalAreas() > 0) {
                        planet.planetFactions[faction].FightFactionForTerritory(enemy, attackStrength, deltaTime);
                    }
                }
            }

            attackTime = attackSpeed;
        }
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
