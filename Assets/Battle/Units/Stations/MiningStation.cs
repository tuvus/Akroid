using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class MiningStation : Station {

    public bool activelyMining;
    public List<Asteroid> nearbyAsteroids;
    [SerializeField] private int miningAmount;
    [SerializeField] private float miningSpeed;
    private float miningTime;
    [SerializeField] private int miningRange;

    public override void SetupUnit(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, bool built, float timeScale) {
        base.SetupUnit(name, faction, positionGiver, rotation, built, timeScale);
        nearbyAsteroids = new List<Asteroid>(10);
        UpdateMiningStationAsteroids();
        activelyMining = true;
        faction.AddMiningStation(this);
        if (this.built) {
            SetGroup(faction.CreateNewUnitGroup("MiningGroup" + faction.stations.Count, true, new List<Unit>(10)));
        }
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        List<AsteroidField> eligibleAsteroidFields = faction.GetClosestAvailableAsteroidFields(positionGiver.position);

        for (int i = 0; i < eligibleAsteroidFields.Count; i++) {
            Vector2 targetCenterPosition = Vector2.MoveTowards(eligibleAsteroidFields[i].GetPosition(), positionGiver.position, eligibleAsteroidFields[i].GetSize() + GetSize() + 10);
            Vector2? targetLocationAsteroidField = BattleManager.Instance.FindFreeLocationIncrement(new BattleManager.PositionGiver(targetCenterPosition, positionGiver), this);
            if (targetLocationAsteroidField.HasValue)
                return targetLocationAsteroidField.Value;
        }
        Vector2? targetLocation = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetLocation.HasValue)
            return targetLocation.Value;

        return positionGiver.position;
    }

    public override bool BuildStation() {
        if (!built) {
            SetGroup(faction.CreateNewUnitGroup("MiningGroup" + faction.stations.Count, true, new List<Unit>(10)));
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
            if (nearbyAsteroids.Count == 0 && GetAllCargo(CargoBay.CargoTypes.Metal) <= 0) {
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
            GetCargoBay().LoadCargo(nearbyAsteroids[0].MineAsteroid(math.min((int)GetCargoBay().GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal), GetMiningAmount())), CargoBay.CargoTypes.Metal);
            if (!nearbyAsteroids[0].HasResources()) {
                nearbyAsteroids.RemoveAt(0);
            }
        }
    }

    public void UpdateMiningStationAsteroids() {
        List<Asteroid> tempAsteroids = new List<Asteroid>(10);
        foreach (var asteroidField in BattleManager.Instance.GetAllAsteroidFields()) {
            if (asteroidField.totalResources <= 0)
                continue;
            float tempDistance = Vector2.Distance(transform.position, asteroidField.GetPosition());
            if (tempDistance <= GetMiningRange() + asteroidField.GetSize()) {
                foreach (var asteroid in asteroidField.asteroids) {
                    tempAsteroids.Add(asteroid);
                }
            }
        }
        while (tempAsteroids.Count > 0) {
            Asteroid closest = null;
            float closestDist = 0;
            for (int i = 0; i < tempAsteroids.Count; i++) {
                float tempDist = Vector2.Distance(transform.position, tempAsteroids[i].GetPosition());
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
        return miningAmount;
    }

    public float GetMiningSpeed() {
        return miningSpeed;
    }

    public int GetMiningRange() {
        return miningRange;
    }

    public MiningStationAI GetMiningStationAI() {
        return (MiningStationAI)stationAI;
    }
}
