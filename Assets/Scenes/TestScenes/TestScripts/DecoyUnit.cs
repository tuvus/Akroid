using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecoyUnit : Unit {
    public Unit target;
    public Faction tempFaction;
    public Vector2 velocity;

    private void Start() {
        faction = tempFaction;
        enemyUnitsInRange = new List<Unit>() { target };
        Spawn();
    }

    public override void SetupUnit(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation) {
    }
    public override bool Destroyed() {
        return faction;
    }

    public override void Explode() {
    }

    public override int TakeDamage(int damage) {
        //print(damage);
        health -= damage;
        return 0;
    }

    public override Vector2 GetVelocity() {
        return velocity;
    }

    public override bool IsSpawned() {
        return true;
    }

    public override void DestroyUnit() {
    }
}
