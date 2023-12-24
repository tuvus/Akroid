using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FactionAI : MonoBehaviour {
    public Faction faction { protected set; get; }
    public bool autoResearch;

    [SerializeField] protected List<Ship> idleShips;
    [SerializeField] public List<SelectionGroup> newNearbyEnemyUnits;

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
        foreach (Faction enemy in faction.enemyFactions) {
            foreach (Planet planet in faction.planets) {
                Planet.PlanetFaction planetFaction = planet.planetFactions[faction];
                if (planetFaction.force > 0 && planet.planetFactions.ContainsKey(enemy) && planet.planetFactions[enemy].territory > 0) {
                    planet.planetFactions[faction].FightFactionForTerritory(enemy, math.max(1, (long)(planetFaction.force / (math.max(1, planetFaction.territory) * .6d))), deltaTime);
                }
            }
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

    public virtual void RemoveIdleShip(Ship ship) {
        idleShips.Remove(ship);
    }

    public virtual void RemoveFleet(Fleet fleet) {

    }

    protected float GetTimeScale() {
        return BattleManager.Instance.timeScale;
    }
}
