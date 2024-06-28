using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BattleManager;

public class AsteroidField : ObjectGroup<Asteroid>, IPositionConfirmer {
    public float totalResources;

    public void SetupAsteroidField(BattleManager battleManager) {
        SetupObjectGroup(battleManager, new HashSet<Asteroid>(), true);
    }

    /// <summary>
    /// SetupAstroidFieldPosition needs to be called after all asteroids have been added and placed to determine it's size.
    /// All asteroids need to be added after the battleObjects set in ObjectGroup is initialized.
    /// Therefore SetupAstroidFieldPosition must be called after SetupAsteroidField.
    /// </summary>
    public void SetupAstroidFieldPosition(BattleManager.PositionGiver positionGiver) {
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
        foreach (var blockingObject in battleManager.GetPositionBlockingObjects()) {
            if (Vector2.Distance(position, blockingObject.GetPosition()) <= minDistanceFromObject + GetSize() + blockingObject.GetSize()) {
                return false;
            }
        }
        return true;
    }
}
