using System.Collections.Generic;
using UnityEngine;

public class AsteroidField : ObjectGroup<Asteroid>, IPositionConfirmer {
    public float totalResources;

    public AsteroidField(BattleManager battleManager) : base(battleManager, new HashSet<Asteroid>(), true) { }

    /// <summary>
    /// SetupAsteroidFieldPosition needs to be called after all asteroids have been added and placed to determine it's size.
    /// All asteroids need to be added after the battleObjects set in ObjectGroup is initialized.
    /// Therefore SetupAsteroidFieldPosition must be called after SetupAsteroidField.
    /// </summary>
    public void SetupAsteroidFieldPosition(BattleManager.PositionGiver positionGiver) {
        // Set the correct Asteroid Field Size
        UpdateObjectGroup();
        SetPosition(GetSetupPosition(positionGiver));
        // Move the asteroids to the new Asteroid Field position
        foreach (var asteroid in battleObjects) {
            asteroid.AdjustPosition(GetPosition());
        }
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
