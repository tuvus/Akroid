using UnityEngine;

public abstract class BattleObjectUI : ObjectUI {
    protected UIManager uIManager { get; private set; }
    public BattleObject battleObject { get; private set; }
    public bool active { get; private set; }
    public bool displayed { get; private set; }
    private int sortinOrder;

    public virtual void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject);
        this.uIManager = uIManager;
        this.battleObject = battleObject;
        SetRotation(battleObject.rotation);
        transform.localScale = battleObject.scale;
        active = true;
        sortinOrder = spriteRenderer.sortingOrder;
    }

    public override void UpdateObject() {
        SetRotation(GetRotation());
        SetPosition(GetPosition());
        spriteRenderer.enabled = IsVisible();
    }

    public virtual void OnBattleObjectRemoved() {
        active = false;
    }

    public virtual void SetDisplayedObject() {
        displayed = true;
        spriteRenderer.sortingOrder = sortinOrder + 10;
    }

    public virtual void UnsetDisplayedObject() {
        displayed = false;
        spriteRenderer.sortingOrder = sortinOrder;
    }

    public virtual Vector2 GetPosition() {
        return battleObject.position;
    }

    public virtual float GetRotation() {
        return battleObject.rotation;
    }

    public virtual bool IsVisible() {
        return battleObject.visible || displayed;
    }

    public override bool IsSelectable() {
        return battleObject.IsSpawned();
    }
}
