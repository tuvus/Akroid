using UnityEngine;

public class Asteroid : BattleObject, IPositionConfirmer {
    public long resources;

    public Asteroid(BattleObjectData battleObjectData, BattleManager battleManager, AsteroidField asteroidField,
        long resources,
        AsteroidScriptableObject asteroidScriptableObject) : base(battleObjectData, battleManager) {
        this.asteroidScriptableObject = asteroidScriptableObject;
        this.asteroidField = asteroidField;
        this.resources = resources;
        asteroidField.totalResources += this.resources;
        visible = true;
        Spawn();
        SetSize(SetupSize());
    }
    public AsteroidScriptableObject asteroidScriptableObject { get; }
    public AsteroidField asteroidField { get; }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (Asteroid asteroid in asteroidField.battleObjects) {
            float dist = Vector2.Distance(position, asteroid.position);
            if (dist <= minDistanceFromObject + GetSize() + asteroid.GetSize()) {
                return false;
            }
        }

        return true;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        Vector2? targetPosition = battleManager.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    /// <summary>
    ///     Returns the amount mined.
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
        this.position += position;
    }

    public override float GetSpriteSize() {
        return Calculator.GetSpriteSizeFromBounds(asteroidScriptableObject.spriteBounds, scale);
    }

    public override GameObject GetPrefab() {
        return asteroidScriptableObject.prefab;
    }
}
