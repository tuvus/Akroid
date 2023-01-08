using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidField : ObjectGroup<Asteroid>, IPositionConfirmer {
    public List<Asteroid> asteroids;
    public float totalResources;

    public void SetupAsteroidField(BattleManager.PositionGiver positionGiver) {
        SetupObjectGroup(asteroids);
        UpdateObjectGroup();
        for (int i = 0; i < asteroids.Count; i++) {
            asteroids[i].AdjustPosition(-GetPosition());
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
        foreach (var star in BattleManager.Instance.GetAllStars()) {
            if (Vector2.Distance(position, star.GetPosition()) <= minDistanceFromObject + GetSize() + star.GetSize()) {
                return false;
            }
        }
        foreach (var asteroidField in BattleManager.Instance.GetAllAsteroidFields()) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <= minDistanceFromObject + GetSize() + asteroidField.GetSize()) {
                return false;
            }
        }
        foreach (var station in BattleManager.Instance.GetAllStations()) {
            if (Vector2.Distance(position, station.GetPosition()) <= minDistanceFromObject + GetSize() + station.GetSize()) {
                return false;
            }
        }
        foreach (var planet in BattleManager.Instance.planets) {
            if (Vector2.Distance(position, planet.GetPosition()) <= minDistanceFromObject + planet.GetSize() + GetSize()) {
                return false;
            }
        }
        return true;
    }
}
