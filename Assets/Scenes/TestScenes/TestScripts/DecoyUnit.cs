using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecoyUnit : Unit {
    public Unit target;
    public Faction tempFaction;
    public Vector2 inputVelocity;

    public DecoyUnit(BattleObjectData battleObjectData, BattleManager battleManager, UnitScriptableObject unitScriptableObject, Unit target,
        Faction tempFaction, Vector2 inputVelocity) : base(battleObjectData, battleManager, unitScriptableObject) {
        this.target = target;
        this.tempFaction = tempFaction;
        this.inputVelocity = inputVelocity;
    }

    private void Start() {
        faction = tempFaction;
        enemyUnitsInRange = new List<Unit>() { target };
        Spawn();
    }

    public void FixedUpdate() {
        velocity = inputVelocity;
    }


    public override bool Destroyed() {
        return faction == null;
    }

    public override void Explode() { }

    public override void TakeDamage(int damage) {
        //print(damage);
        health -= damage;
        unitScriptableObject.maxHealth -= damage;
        return;
    }

    public override Vector2 GetVelocity() {
        return velocity;
    }

    public override bool IsSpawned() {
        return true;
    }

    public override GameObject GetPrefab() {
        throw new System.NotImplementedException();
    }

    public override void DestroyUnit() { }
}
