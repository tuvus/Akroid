using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FactionAI : MonoBehaviour {
    public BattleManager battleManager { get; private set; }
    public Faction faction { get; protected set; }
    public bool autoResearch;
    public float attackTime;

    [field:SerializeField] public List<Ship> idleShips { get; protected set; }
    [SerializeField] public List<SelectionGroup> newNearbyEnemyUnits;

    public virtual void SetupFactionAI(BattleManager battleManager, Faction faction) {
        this.battleManager = battleManager;
        this.faction = faction;
        idleShips = new List<Ship>(10);
        autoResearch = true;
    }

    public virtual void GenerateFactionAI() {

    }

    public virtual void UpdateFactionAI(float deltaTime) {
        if (autoResearch)
            faction.UpdateFactionResearch();
        attackTime = math.max(0, attackTime - deltaTime);
        if (attackTime <= 0) {
            foreach (Faction enemy in faction.enemyFactions) {
                foreach (Planet planet in faction.planets) {
                    Planet.PlanetFaction planetFaction = planet.planetFactions[faction];
                    if (planetFaction.force > 0 && planet.planetFactions.ContainsKey(enemy) && planet.planetFactions[enemy].territory.GetTotalAreas() > 0) {
                        planet.planetFactions[faction].FightFactionForTerritory(enemy, .3f, deltaTime);
                    }
                }
            }
            attackTime = .5f;
        }
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

    public virtual void RemoveShip(Ship ship) {
        idleShips.Remove(ship);
    }

    public virtual void RemoveFleet(Fleet fleet) {

    }

    protected float GetTimeScale() {
        return BattleManager.Instance.timeScale;
    }
}
