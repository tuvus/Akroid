using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour, IPositionConfirmer {
    private AsteroidField asteroidField;
    private SpriteRenderer spriteRenderer;
    private Vector2 position;
    public int resources;
    public CargoBay.CargoTypes asteroidType;
    private float size;

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

    public void SetupAsteroid(AsteroidField asteroidField,BattleManager.PositionGiver positionGiver, AsteroidData asteroidData) {
        spriteRenderer = GetComponent<SpriteRenderer>();
        this.asteroidField = asteroidField;
        this.position = asteroidData.position;
        this.resources = asteroidData.resources;
        asteroidField.totalResources += this.resources;
        this.asteroidType = asteroidData.asteroidType;
        transform.localScale = new Vector2(asteroidData.size, asteroidData.size);
        transform.eulerAngles = new Vector3(0, 0, asteroidData.rotation);
        size = GetSpriteSize() * transform.localScale.x;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrament(positionGiver, this);
        if (targetPosition.HasValue)
            transform.position = targetPosition.Value;
        else
            transform.position = positionGiver.position;
        this.position = transform.position;
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var asteroid in asteroidField.asteroids) {
            float dist = Vector2.Distance(position, asteroid.position);
            if (dist <= minDistanceFromObject + size + asteroid.GetSize()) {
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

    public Vector2 GetPosition() {
        return position;
    }

    public float GetSpriteSize() {
        return Mathf.Max(Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.y, spriteRenderer.sprite.bounds.size.z)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.z, spriteRenderer.sprite.bounds.size.x))) / 2;

    }

    public float GetSize() {
        return size;
    }
}
