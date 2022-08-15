using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Star : MonoBehaviour, IPositionConfirmer {
    SpriteRenderer spriteRenderer;
    SpriteRenderer glareRenderer;
    public Vector2 position;
    [SerializeField]float size;
    Color color;

    public void SetupStar(BattleManager.PositionGiver positionGiver) {
        spriteRenderer = GetComponent<SpriteRenderer>();
        glareRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        color = Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(.8f, 1f), Random.Range(.8f, 1f));
        spriteRenderer.color = color;
        glareRenderer.color = color;
        transform.eulerAngles = new Vector3(0, 0, Random.Range(0, 360));
        float scale = Random.Range(10, 50);
        transform.localScale = new Vector2(scale, scale);
        size = GetSpriteSize() * scale;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrament(positionGiver, this);
        if (targetPosition.HasValue)
            transform.position = targetPosition.Value;
        else
            transform.position = positionGiver.position;
        this.position = transform.position;
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.stars) {
            float dist = Vector2.Distance(position, star.position);
            if (dist <= minDistanceFromObject + size + star.GetSize()) {
                return false;
            }
        }
        foreach (var planet in BattleManager.Instance.planets) {
            if (Vector2.Distance(position, planet.GetPosition()) <= minDistanceFromObject + planet.GetSize() + size) {
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

    public float GetSpriteSize() {
        return Mathf.Max(Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.y, spriteRenderer.sprite.bounds.size.z)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.z, spriteRenderer.sprite.bounds.size.x))) / 2;
    }

    public Vector2 GetPosition() {
        return position;
    }
    public float GetSize() {
        return size;
    }

}
