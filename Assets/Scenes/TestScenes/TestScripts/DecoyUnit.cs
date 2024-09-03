using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecoyUnit : Unit {
    public Unit target;
    public Faction tempFaction;
    public Vector2 inputVelocity;

    private void Start() {
        faction = tempFaction;
        enemyUnitsInRange = new List<Unit>() { target };
        Spawn();
        SetupUnit(null, "Test", tempFaction, new BattleManager.PositionGiver(), 0, 1, null);
    }

    public override void SetupUnit(BattleManager battleManager, string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, float timeScale, UnitScriptableObject unitScriptableObject) {
        enemyUnitsInRange = new List<Unit>() { target };
    }

    public void FixedUpdate() {
        velocity = inputVelocity;
    }


    public override bool Destroyed() {
        return faction;
    }

    public override void Explode() {
    }

    public override int TakeDamage(int damage) {
        //print(damage);
        health -= damage;
        UnitScriptableObject.maxHealth -= damage;
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
