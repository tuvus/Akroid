using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidField : ObjectGroup<Asteroid>, IPositionConfirmer {
    public float totalResources;

    public void SetupAsteroidField(BattleManager.PositionGiver positionGiver) {
        SetupObjectGroup(battleManager, new HashSet<Asteroid>(), true);
        UpdateObjectGroup();
        foreach (var asteroid in battleObjects) {
            asteroid.AdjustPosition(-GetPosition());
        }
        SetPosition(GetSetupPosition(positionGiver));
        transform.position = GetPosition();
    }

    private Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {

        foreach (var star in battleManager.stars) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject + GetSize() + star.GetSize()) {
                return false;
            }
        }
        foreach (var asteroidField in battleManager.asteroidFields) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <= minDistanceFromObject + GetSize() + asteroidField.GetSize()) {
                return false;
            }
        }
        foreach (var station in battleManager.stations) {
            if (Vector2.Distance(position, station.position) <= minDistanceFromObject + GetSize() + station.GetSize()) {
                return false;
            }
        }
        foreach (var planet in battleManager.planets) {
            if (Vector2.Distance(position, planet.GetPosition()) <= minDistanceFromObject + planet.GetSize() + GetSize()) {
                return false;
            }
        }
        return true;
    }
}
