using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Planet : BattleObject, IPositionConfirmer {
    public string planetName { get; protected set; }
    public Faction faction { get; protected set; }

    [SerializeField] long population;

    public double rateOfGrowth;
    [SerializeField] long carryingCapacity;
    [SerializeField] long stargingPop;
    [SerializeField] float timeSinceStart;

    public void SetupPlanet(string name, Faction faction, BattleManager.PositionGiver positionGiver, long population, double rateOfGrowth, float rotation) {
        this.faction = faction;
        base.SetupBattleObject(positionGiver, rotation);
        this.planetName = name;
        this.population = population;
        this.rateOfGrowth = rateOfGrowth;
        SetPopulationTarget(population);
    }

    public void UpdatePlanet(float deltaTime) {
        timeSinceStart += deltaTime;
        population = (long)((carryingCapacity / (1 + ((carryingCapacity / stargingPop) - 1) * Mathf.Pow(math.E, (float)(-rateOfGrowth * timeSinceStart)))) * (-Mathf.Sin(timeSinceStart / 100) / 30.0 + 1));
    }

    public void SetPopulationTarget(long carryingCapacity) {
        timeSinceStart = 0;
        stargingPop = population;
        this.carryingCapacity = carryingCapacity;
    }

    public void SetRateOfGrowth(double rateOfGrowth) {
        this.rateOfGrowth = rateOfGrowth;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrament(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.stars) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject + star.GetSize() + size) {
                return false;
            }
        }
        foreach (var asteroidField in BattleManager.Instance.asteroidFields) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <= minDistanceFromObject + asteroidField.GetSize() + size) {
                return false;
            }
        }
        foreach (var station in BattleManager.Instance.stations) {
            if (Vector2.Distance(position, station.GetPosition()) <= minDistanceFromObject + station.GetSize() + size) {
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
