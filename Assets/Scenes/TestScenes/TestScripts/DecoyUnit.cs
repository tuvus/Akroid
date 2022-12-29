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
        SetupUnit("Test", tempFaction, new BattleManager.PositionGiver(), 0, 1);
    }

    public override void SetupUnit(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, float timeScale) {
        turrets = new List<Turret>(GetComponentsInChildren<Turret>());
        foreach (var turret in turrets) {
            turret.SetupTurret(this);
        }
        missileLaunchers = new List<MissileLauncher>(GetComponentsInChildren<MissileLauncher>());
        foreach (var missileLauncher in missileLaunchers) {
            missileLauncher.SetupMissileLauncher(this); ;
        }
        enemyUnitsInRange = new List<Unit>() { target };
    }

    public void FixedUpdate() {
        velocity = inputVelocity;
        foreach (var turret in turrets) {
            turret.UpdateTurret(Time.fixedDeltaTime);
        }
        foreach (var missileLauncher in missileLaunchers) {
            missileLauncher.UpdateMissileLauncher(Time.fixedDeltaTime);
        }
    }


    public override bool Destroyed() {
        return faction;
    }

    public override void Explode() {
    }

    public override int TakeDamage(int damage) {
        //print(damage);
        health -= damage;
        maxHealth -= damage;
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
