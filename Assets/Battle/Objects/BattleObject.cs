using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BattleObject : MonoBehaviour, IObject, IPositionConfirmer {
    public BattleManager battleManager { get; private set; }
    [field: SerializeField] public string objectName { get; protected set; }
    [field: SerializeField] public float size { get; protected set; }
    [field: SerializeField] public Vector2 position { get; protected set; }
    protected SpriteRenderer spriteRenderer;
    private List<IObjectGroupLink> battleObjectInGroups = new List<IObjectGroupLink>(5);
    [field: SerializeField] public Faction faction { get; protected set; }
    private bool spawned;

    //Transform sizeIndicator;

    /// <summary>
    /// Sets up the BattleObject with default position and rotation.
    /// Sets up the size as normal
    /// </summary>
    protected void SetupBattleObject(BattleManager battleManager) {
        this.battleManager = battleManager;
        faction = null;
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
    protected void SetupBattleObject(BattleManager battleManager, BattleManager.PositionGiver positionGiver, float rotation) {
        this.battleManager = battleManager;
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.eulerAngles = new Vector3(0, 0, rotation);
        //sizeIndicator = Instantiate(BattleManager.GetSizeIndicatorPrefab(), transform).transform;
        SetSize(SetupSize());
        transform.position = GetSetupPosition(positionGiver);
        position = transform.position;
    }

    public bool IsInGroup(IObjectGroupLink newGroup) {
        return battleObjectInGroups.Contains(newGroup);
    }

    public void AddGroup(IObjectGroupLink newGroup) {
        battleObjectInGroups.Add(newGroup);
    }

    public void RemoveGroup(IObjectGroupLink removeGroup) {
        battleObjectInGroups.Remove(removeGroup);
    }

    public void RemoveFromAllGroups() {
        for (int i = battleObjectInGroups.Count - 1; i >= 0; i--) {
            battleObjectInGroups[i].RemoveBattleObject(this);
        }
    }

    public void UpdateGroups() {
        for (int i = 0; i < battleObjectInGroups.Count; i++) {
            battleObjectInGroups[i].UpdateObjectGroup();
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

    public void SetRotation(float rotation) {
        transform.eulerAngles = new Vector3(0, 0, rotation);
    }

    public virtual void SelectObject(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) { }

    public virtual void UnselectObject() { }

    /// <summary>
    /// Returns the game size of the object used for collisions, targeting, etc.
    /// </summary>
    /// <returns>the physical size of the object</returns>
    public float GetSize() {
        return size;
    }

    protected virtual void Spawn() {
        spawned = true;
    }

    protected virtual void Despawn(bool removeImmediately) {
        spawned = false;
        if (removeImmediately) {
            Destroy(gameObject);
        }
    }

    public virtual bool IsSpawned() {
        return spawned;
    }

    public virtual bool IsSelectable() {
        return IsSpawned();
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

    public virtual List<SpriteRenderer> GetSpriteRenderers() {
        return new List<SpriteRenderer> { spriteRenderer };
    }

    public bool IsUnit() {
        return this is Unit;
    }

    public bool IsShip() {
        return this is Ship;
    }

    public bool IsStation() {
        return this is Station;
    }

    public bool IsPlanet() {
        return this is Planet;
    }

    public bool IsStar() {
        return this is Star;
    }
}