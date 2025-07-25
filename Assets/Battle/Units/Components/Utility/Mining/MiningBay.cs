using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class MiningBay : HabitationArea {
    private MiningBayScriptableObject miningBayScriptableObject;

    private float miningTime;
    public bool activelyMining { get; private set; }

    public List<Asteroid> nearbyAsteroids;

    public MiningBay(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) : base(battleManager, module, unit,
        componentScriptableObject) {
        miningBayScriptableObject = (MiningBayScriptableObject)componentScriptableObject;
        miningTime = 0f;
        activelyMining = true;
        nearbyAsteroids = new();
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        miningBayScriptableObject = (MiningBayScriptableObject)componentScriptableObject;
        UpdateMiningBayAsteroids();
    }

    public void UpdateMiningBayAsteroids() {
        nearbyAsteroids = battleManager.asteroidFields
            .Where(af => af.totalResources > 0 && Vector2.Distance(unit.GetPosition(), af.GetPosition()) <=
                GetMiningRange() + af.GetSize())
            .SelectMany(af => af.battleObjects).ToList()
            .OrderBy(a => math.distancesq(a.GetPosition(), unit.GetPosition())).ToList();
        activelyMining = nearbyAsteroids.Count != 0;
    }

    public void UpdateMiningBay(float deltaTime) {
        if (!activelyMining) return;

        if (unit is Station station) {
            long engineersWanted = miningBayScriptableObject.engineersRequired - population.engineers;
            station.RequestPersonnel(this, new Population(0, 0, engineersWanted));
        }

        miningTime -= deltaTime;
        if (miningTime <= 0) {
            while (nearbyAsteroids.Count > 0 && !nearbyAsteroids[0].HasResources()) nearbyAsteroids.RemoveAt(0);
            if (nearbyAsteroids.Count == 0) {
                UpdateMiningBayAsteroids();
                if (!activelyMining) return;
            }

            unit.LoadCargo(nearbyAsteroids.First()
                    .MineAsteroid(math.min(unit.GetAvailableCargoSpace(nearbyAsteroids.First().GetAsteroidType()),
                        (miningBayScriptableObject.miningAmount * population.engineers) /
                        miningBayScriptableObject.engineersRequired)),
                nearbyAsteroids.First().GetAsteroidType());
            miningTime += miningBayScriptableObject.miningSpeed;
        }
    }

    public void FillEmployees(float ratio = 1f) {
        population.engineers = (long)(miningBayScriptableObject.engineersRequired * ratio);
    }

    public float GetMiningRange() {
        return miningBayScriptableObject.miningRange * battleManager.systemSizeModifier;
    }
}
