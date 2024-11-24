using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents an object that can exist in the scene.
/// Holds functionality that is common to most objects.
/// </summary>
public abstract class BattleObject : IObject, IPositionConfirmer {
    public BattleManager battleManager { get; private set; }
    [field: SerializeField] public string objectName { get; protected set; }
    [field: SerializeField] public float size { get; protected set; }
    [field: SerializeField] public Vector2 position { get; protected set; }
    [field: SerializeField] public float rotation { get; protected set; }
    [field: SerializeField] public Vector2 scale { get; protected set; }

    private List<IObjectGroupLink> battleObjectInGroups = new List<IObjectGroupLink>(5);
    [field: SerializeField] public Faction faction { get; protected set; }
    public bool spawned { get; protected set; }
    public bool visible { get; protected set; }

    public struct BattleObjectData {
        public string objectName;
        public BattleManager.PositionGiver positionGiver;
        public float rotation;
        public Vector2 scale;
        public Faction faction;

        public BattleObjectData(string objectName, BattleManager.PositionGiver positionGiver, float rotation, Vector2 scale,
            Faction faction = null) {
            this.objectName = objectName;
            this.positionGiver = positionGiver;
            this.rotation = rotation;
            this.scale = scale;
            this.faction = faction;
        }

        public BattleObjectData(string objectName, Vector2 position, float rotation, Vector2 scale, Faction faction = null) :
            this(objectName, new BattleManager.PositionGiver(position), rotation, Vector2.one, faction) { }

        public BattleObjectData(string objectName, BattleManager.PositionGiver positionGiver, float rotation, Faction faction = null) :
            this(objectName, positionGiver, rotation, Vector2.one, faction) { }

        public BattleObjectData(string objectName, Vector2 position, float rotation, Faction faction = null) :
            this(objectName, new BattleManager.PositionGiver(position), rotation, Vector2.one, faction) { }

        public BattleObjectData(string objectName) : this(objectName, new BattleManager.PositionGiver(Vector2.zero), 0, Vector2.one) { }
    }

    public BattleObject() { }

    public BattleObject(BattleObjectData battleObjectData, BattleManager battleManager) {
        this.battleManager = battleManager;
        this.objectName = battleObjectData.objectName;
        this.position = Vector2.zero;
        this.rotation = battleObjectData.rotation;
        this.scale = battleObjectData.scale;
        this.faction = battleObjectData.faction;
        spawned = false;
        visible = false;
        SetSize(SetupSize());
    }

    public void SetupPosition(BattleManager.PositionGiver positionGiver) {
        this.position = GetSetupPosition(positionGiver);
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

    public void SetRotation(float rotation) {
        this.rotation = rotation;
    }
    //
    // public virtual void SelectObject(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) { }
    //
    // public virtual void UnselectObject() { }

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
        visible = false;
    }

    public virtual bool IsSpawned() {
        return spawned;
    }

    public virtual bool IsSelectable() {
        return IsSpawned();
    }

    public void SetFaction(Faction faction) {
        this.faction = faction;
    }

    /// <summary>
    /// Returns the size of the sprite not including any scale modifications specific to the BattleObject.
    /// </summary>
    /// <returns>the size of the sprite</returns>
    public virtual float GetSpriteSize() {
        return 0;
//         if (spriteRenderer.sprite == null) return 0;
//         return Mathf.Max(Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y)),
// Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.y, spriteRenderer.sprite.bounds.size.z)),
// Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.z, spriteRenderer.sprite.bounds.size.x))) / 2 * transform.localScale.y;
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

    public bool IsAsteroid() {
        return this is Asteroid;
    }

    public bool IsGasCloud() {
        return this is GasCloud;
    }
}
