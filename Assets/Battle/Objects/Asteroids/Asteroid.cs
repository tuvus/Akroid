using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : BattleObject, IPositionConfirmer {
    private AsteroidField asteroidField;
    public long resources;
    public CargoBay.CargoTypes asteroidType;

    public Asteroid(BattleObjectData battleObjectData, BattleManager battleManager, AsteroidField asteroidField, long resources, CargoBay.CargoTypes asteroidType): base(battleObjectData, battleManager) {
        this.asteroidField = asteroidField;
        this.resources = resources;
        this.asteroidType = asteroidType;
        asteroidField.totalResources += this.resources;
        Spawn();
    }

    protected override float SetupSize() {
        // return GetSpriteSize() * transform.localScale.x;
        return 0;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        else
            return positionGiver.position;
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var asteroid in asteroidField.battleObjects) {
            float dist = Vector2.Distance(position, asteroid.position);
            if (dist <= minDistanceFromObject + GetSize() + asteroid.GetSize()) {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns the ammount mined.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public long MineAsteroid(long amount) {
        if (resources > amount) {
            resources -= amount;
            asteroidField.totalResources -= amount;
            return amount;
        }
        long returnValue = resources;
        asteroidField.totalResources -= resources;
        resources = 0;
        return returnValue;
    }

    public bool HasResources() {
        return resources > 0;
    }

    public void AdjustPosition(Vector2 position) {
        transform.position += (Vector3)position;
        this.position = transform.position;
    }
}
