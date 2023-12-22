using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class Planet : BattleObject, IPositionConfirmer {
    public string planetName { get; protected set; }
    [field: SerializeField] public long population { get; protected set; }

    public float rotationSpeed;
    public double rateOfGrowth;
    [SerializeField] long carryingCapacity;
    [SerializeField] long startingPop;
    [SerializeField] float timeSinceStart;

    public void SetupPlanet(string name, Faction faction, BattleManager.PositionGiver positionGiver, long population, double rateOfGrowth, float rotation) {
        this.faction = faction;
        base.SetupBattleObject(positionGiver, rotation);
        this.planetName = name;
        this.population = population;
        this.rateOfGrowth = rateOfGrowth;
        SetPopulationTarget(population);
        rotationSpeed *= Random.Range(.5f, 1.5f);
        if (Random.Range(-1, 1) < 0) {
            rotationSpeed *= -1;
        }
    }

    public void UpdatePlanet(float deltaTime) {
        timeSinceStart += deltaTime;
        population = (long)((carryingCapacity / (1 + ((carryingCapacity / startingPop) - 1) * Mathf.Pow(math.E, (float)(-rateOfGrowth * timeSinceStart)))) * (-Mathf.Sin(timeSinceStart / 100) / 30.0 + 1));
        SetRotation(transform.eulerAngles.z + rotationSpeed * deltaTime);
    }

    public void SetPopulationTarget(long carryingCapacity) {
        timeSinceStart = 0;
        startingPop = population;
        this.carryingCapacity = carryingCapacity;
    }

    public void SetRateOfGrowth(double rateOfGrowth) {
        this.rateOfGrowth = rateOfGrowth;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.stars) {
            if (Vector2.Distance(position, star.GetPosition()) <= minDistanceFromObject + star.GetSize() + GetSize()) {
                return false;
            }
        }
        foreach (var asteroidField in BattleManager.Instance.asteroidFields) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <= minDistanceFromObject + asteroidField.GetSize() + GetSize()) {
                return false;
            }
        }
        foreach (var station in BattleManager.Instance.stations) {
            if (Vector2.Distance(position, station.GetPosition()) <= minDistanceFromObject + station.GetSize() + GetSize()) {
                return false;
            }
        }
        return true;
    }

    public override float GetSpriteSize() {
        return spriteRenderer.sprite.bounds.size.x / 2;
    }

    public long GetPopulation() {
        return population;
    }
}
