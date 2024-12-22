using UnityEngine;
using UnityEngine.Serialization;

public abstract class BattleObjectUI : ObjectUI {
    protected UIManager uIManager { get; private set; }
    public BattleObject battleObject { get; private set; }
    public bool active { get; private set; }

    public virtual void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject);
        this.uIManager = uIManager;
        this.battleObject = battleObject;
        transform.position = battleObject.GetPosition();
        SetRotation(battleObject.rotation);
        transform.localScale = battleObject.scale;
        active = true;
    }

    public override void UpdateObject() {
        SetRotation(GetRotation());
        transform.localPosition = GetPosition();
        spriteRenderer.enabled = IsVisible();
    }

    public virtual void OnBattleObjectRemoved() {
        active = false;
    }

    public virtual Vector2 GetPosition() {
        return battleObject.position;
    }

    public virtual float GetRotation() {
        return battleObject.rotation;
    }

    public virtual bool IsVisible() {
        return battleObject.visible;
    }

    public override bool IsSelectable() {
        return battleObject.IsSpawned();
    }
}
