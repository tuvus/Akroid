﻿using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class MiningStation : Station {
    public MiningStationScriptableObject miningStationScriptableObject { get; private set; }

    public bool activelyMining;
    public List<Asteroid> nearbyAsteroids;
    private float miningTime;

    public MiningStation(BattleObjectData battleObjectData, BattleManager battleManager,
        MiningStationScriptableObject miningStationScriptableObject,
        bool built) : base(battleObjectData, battleManager, miningStationScriptableObject, built) {
        this.miningStationScriptableObject = miningStationScriptableObject;
        nearbyAsteroids = new List<Asteroid>(10);
        UpdateMiningStationAsteroids();
        activelyMining = true;
        faction.AddMiningStation(this);
        if (this.built) {
            SetGroup(faction.CreateNewUnitGroup("MiningGroup" + faction.stations.Count, true, new HashSet<Unit>(10)));
        }
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;


        foreach (var asteroidField in faction.GetClosestAvailableAsteroidFields(positionGiver.position)) {
            Vector2 targetCenterPosition = Vector2.MoveTowards(asteroidField.position, positionGiver.position,
                asteroidField.GetSize() + GetSize() + 10);
            Vector2? targetLocationAsteroidField = battleManager.FindFreeLocationIncrement(
                new BattleManager.PositionGiver(targetCenterPosition, positionGiver), this);
            if (targetLocationAsteroidField.HasValue)
                return targetLocationAsteroidField.Value;
        }

        Vector2? targetLocation = battleManager.FindFreeLocationIncrement(positionGiver, this);
        if (targetLocation.HasValue)
            return targetLocation.Value;

        return positionGiver.position;
    }

    public override bool BuildStation() {
        if (!built) {
            SetGroup(faction.CreateNewUnitGroup("MiningGroup" + faction.stations.Count, true, new HashSet<Unit>(10)));
        }

        return base.BuildStation();
    }

    public override void UpdateUnit(float deltaTime) {
        base.UpdateUnit(deltaTime);
        if (activelyMining) {
            Profiler.BeginSample("UpdateMining");
            miningTime -= deltaTime;
            if (miningTime <= 0) {
                ManageStationMining();
                miningTime += GetMiningSpeed();
            }

            if (nearbyAsteroids.Count == 0 && GetAllCargoOfType(CargoBay.CargoTypes.Metal) <= 0) {
                activelyMining = false;
                faction.RemoveMiningStation(this);
            }

            Profiler.EndSample();
        }
    }

    public void ManageStationMining() {
        if (nearbyAsteroids.Count == 0) {
            UpdateMiningStationAsteroids();
        }

        if (nearbyAsteroids.Count > 0) {
            LoadCargo(nearbyAsteroids[0].MineAsteroid(math.min(GetAvailableCargoSpace(CargoBay.CargoTypes.Metal), GetMiningAmount())),
                CargoBay.CargoTypes.Metal);
            if (!nearbyAsteroids[0].HasResources()) {
                nearbyAsteroids.RemoveAt(0);
            }
        }
    }

    public void UpdateMiningStationAsteroids() {
        List<Asteroid> tempAsteroids = new List<Asteroid>(10);
        foreach (var asteroidField in battleManager.asteroidFields) {
            if (asteroidField.totalResources <= 0)
                continue;
            float tempDistance = Vector2.Distance(position, asteroidField.GetPosition());
            if (tempDistance <= GetMiningRange() + asteroidField.GetSize()) {
                foreach (var asteroid in asteroidField.battleObjects) {
                    tempAsteroids.Add(asteroid);
                }
            }
        }

        while (tempAsteroids.Count > 0) {
            Asteroid closest = null;
            float closestDist = 0;
            for (int i = 0; i < tempAsteroids.Count; i++) {
                float tempDist = Vector2.Distance(position, tempAsteroids[i].GetPosition());
                if (closest == null || tempDist < closestDist) {
                    closest = tempAsteroids[i];
                    closestDist = tempDist;
                }
            }

            nearbyAsteroids.Add(closest);
            tempAsteroids.Remove(closest);
        }
    }

    public int GetMiningAmount() {
        return miningStationScriptableObject.miningAmount;
    }

    public float GetMiningSpeed() {
        return miningStationScriptableObject.miningSpeed;
    }

    public int GetMiningRange() {
        return (int)(miningStationScriptableObject.miningRange * battleManager.systemSizeModifier);
    }

    public MiningStationAI GetMiningStationAI() {
        return (MiningStationAI)stationAI;
    }
}
