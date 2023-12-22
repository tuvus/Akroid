using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : BattleObject, IPositionConfirmer {
    private AsteroidField asteroidField;
    public int resources;
    public CargoBay.CargoTypes asteroidType;

    public struct AsteroidData {
        public Vector2 position;
        public float rotation;
        public float size;
        public int resources;
        public CargoBay.CargoTypes asteroidType;

        public AsteroidData(Vector2 position, float rotation, float size, int resources, CargoBay.CargoTypes asteroidType) {
            this.position = position;
            this.rotation = rotation;
            this.size = size;
            this.resources = resources;
            this.asteroidType = asteroidType;
        }
    }

    public void SetupAsteroid(AsteroidField asteroidField, BattleManager.PositionGiver positionGiver, AsteroidData asteroidData) {
        transform.localScale = new Vector2(asteroidData.size, asteroidData.size);
        this.asteroidField = asteroidField;
        base.SetupBattleObject(positionGiver, asteroidData.rotation);
        this.position = asteroidData.position;
        this.resources = asteroidData.resources;
        asteroidField.totalResources += this.resources;
        this.asteroidType = asteroidData.asteroidType;
        this.position = transform.position;
        float greyColor = Random.Range(0.3f, 0.7f);
        spriteRenderer.color = new Color(greyColor, greyColor, greyColor, 1);
        Spawn();
    }

    protected override float SetupSize() {
        return GetSpriteSize() * transform.localScale.x;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        else
            return positionGiver.position;
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var asteroid in asteroidField.asteroids) {
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
    /// <param name="ammount"></param>
    /// <returns></returns>
    public int MineAsteroid(int ammount) {
        if (resources > ammount) {
            resources -= ammount;
            asteroidField.totalResources -= ammount;
            return ammount;
        }
        int returnValue = resources;
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

    public override float GetSpriteSize() {
        return spriteRenderer.sprite.bounds.size.x / 2;
    }
}
