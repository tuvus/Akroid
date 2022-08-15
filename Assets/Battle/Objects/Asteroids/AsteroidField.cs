using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidField : BattleObject, IPositionConfirmer {
    public List<Asteroid> asteroids;
    public float totalResources;

    public void SetupAsteroidField(BattleManager.PositionGiver positionGiver) {
        base.SetupBattleObject(positionGiver, 0);
    }

    protected override float SetupSize() {
        float size = 0;
        for (int i = 0; i < asteroids.Count; i++) {
            size = Mathf.Max(size, Vector2.Distance(position, asteroids[i].GetPosition()));
        }
        return size;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrament(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.GetAllStars()) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject + size + star.GetSize()) {
                return false;
            }
        }
        foreach (var asteroidField in BattleManager.Instance.GetAllAsteroidFields()) {
            if (Vector2.Distance(position, asteroidField.position) <= minDistanceFromObject + size + asteroidField.size) {
                return false;
            }
        }
        foreach (var station in BattleManager.Instance.GetAllStations()) {
            if (Vector2.Distance(position, station.GetPosition()) <= minDistanceFromObject + size + station.GetSize()) {
                return false;
            }
        }
        return true;
    }
}
