using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BattleObject : MonoBehaviour, IPositionConfirmer {

    [SerializeField] protected float size;
    protected Vector2 position;
    protected SpriteRenderer spriteRenderer;

    protected void SetupBattleObject(BattleManager.PositionGiver positionGiver, float rotation) {
        spriteRenderer = GetComponent<SpriteRenderer>();
        size = SetupSize();
        transform.position = GetSetupPosition(positionGiver);
        position = transform.position;
        transform.eulerAngles = new Vector3(0, 0, rotation);
    }

    protected virtual float SetupSize() {
        return GetSpriteSize();
    }

    protected virtual Vector2 GetSetupPosition(BattleManager.PositionGiver position) {
        return position.position;
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        return true;
    }

    public Vector2 GetPosition() {
        return position;
    }

    public float GetRotation() {
        return transform.eulerAngles.z;
    }

    public float GetSize() {
        return size;
    }

    public virtual float GetSpriteSize() {
        return Mathf.Max(Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.y, spriteRenderer.sprite.bounds.size.z)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.z, spriteRenderer.sprite.bounds.size.x))) / 2 * transform.localScale.y;
    }

    public SpriteRenderer GetSpriteRenderer() {
        return spriteRenderer;
    }
}
