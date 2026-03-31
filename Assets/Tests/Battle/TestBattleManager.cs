using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;


public class TestBattleManager : BattleManager {
    public TestBattleManager() {
        timeScale = 1;
        startOfSimulation = 0;
        random = new Random(1);
        factions = new HashSet<Faction>(0);
        battleObjects = new HashSet<BattleObject>(0);
        units = new HashSet<Unit>(0);
        ships = new HashSet<Ship>(0);
        stations = new HashSet<Station>(0);
        stationsInProgress = new HashSet<Station>(0);
        stars = new HashSet<Star>(0);
        planets = new HashSet<Planet>(0);
        asteroidFields = new HashSet<AsteroidField>(0);
        gasClouds = new HashSet<GasCloud>(0);
        projectiles = new HashSet<Projectile>(0);
        missiles = new HashSet<Missile>(0);
        destroyedUnits = new HashSet<Unit>(0);
        usedProjectiles = new HashSet<Projectile>(0);
        unusedProjectiles = new HashSet<Projectile>(0);
        usedMissiles = new HashSet<Missile>(0);
        unusedMissiles = new HashSet<Missile>(0);
        players = new HashSet<Player>();
        systemSizeModifier = 1;
        researchModifier = 1;
        battleState = BattleState.Running;
    }

    public Faction CreateTestFaction() {
        var factionData = new Faction.FactionData("TestFaction" + factions.Count, "TF" + factions.Count, Color.black,
            new Character("TestCharacter", Resources.Load<GameObject>("Prefabs/Characters/Firon")),
            0, 0, 0, 0);
        Faction faction = new Faction(this, factionData, new PositionGiver(Vector2.zero));
        factions.Add(faction);
        faction.GenerateFaction(factionData, 100);
        return faction;
    }
}
