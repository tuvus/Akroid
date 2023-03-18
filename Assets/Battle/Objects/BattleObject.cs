using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BattleObject : MonoBehaviour, IPositionConfirmer {

    [SerializeField] float size;
    protected Vector2 position;
    protected SpriteRenderer spriteRenderer;
    private List<IObjectGroupLink> battleObjectInGroups;
    //Transform sizeIndicator;

    /// <summary>
    /// Sets up the BattleObject with default position and rotation.
    /// Sets up the size as normal
    /// </summary>
    protected void SetupBattleObject() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        position = transform.position;
        //sizeIndicator = Instantiate(BattleManager.GetSizeIndicatorPrefab(), transform).transform;
        SetSize(SetupSize());
    }

    /// <summary>
    /// Uses the given positionGiver and rotation to set the position and the rotation of the BattleObject.
    /// Also sets up the size.
    /// </summary>
    /// <param name="positionGiver"></param>
    /// <param name="rotation"></param>
    protected void SetupBattleObject(BattleManager.PositionGiver positionGiver, float rotation) {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.eulerAngles = new Vector3(0, 0, rotation);
        //sizeIndicator = Instantiate(BattleManager.GetSizeIndicatorPrefab(), transform).transform;
        SetSize(SetupSize());
        transform.position = GetSetupPosition(positionGiver);
        position = transform.position;
    }

    public void AddGroup(IObjectGroupLink newGroup) {
        battleObjectInGroups.Add(newGroup);
    }

    public void RemoveGroup(IObjectGroupLink removeGroup) {
        battleObjectInGroups.Remove(removeGroup);
    }

    public void RemoveFromAllGroups() {
        for (int i = 0; i < battleObjectInGroups.Count; i++) {
            battleObjectInGroups[i].RemoveBattleObject(this);
        }
    }

    protected virtual float SetupSize() {
        return GetSpriteSize();
    }

    protected void SetSize(float newSize) {
        size = newSize;
        //sizeIndicator.localScale = new Vector3(GetSize() / transform.localScale.x * 2, GetSize() / transform.localScale.y * 2, 1);
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

    /// <summary>
    /// Returns the game size of the object used for collisions, targeting, etc.
    /// </summary>
    /// <returns>the physical size of the object</returns>
    public float GetSize() {
        return size;
    }

    [ContextMenu("GetObjectSize")]
    private void ManualLogSize() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Debug.Log(SetupSize());
    }

    /// <summary>
    /// Returns the size of the sprite not including any scale modifications specific to the BattleObject.
    /// </summary>
    /// <returns>the size of the sprite</returns>
    public virtual float GetSpriteSize() {
        return Mathf.Max(Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.y, spriteRenderer.sprite.bounds.size.z)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.z, spriteRenderer.sprite.bounds.size.x))) / 2 * transform.localScale.y;
    }

    public SpriteRenderer GetSpriteRenderer() {
        return spriteRenderer;
    }
}