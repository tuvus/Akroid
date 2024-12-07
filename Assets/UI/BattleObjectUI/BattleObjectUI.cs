using UnityEngine;

public abstract class BattleObjectUI : ObjectUI {
    public BattleObject battleObject { get; private set; }

    public virtual void Setup(BattleObject battleObject) {
        base.Setup();
        this.battleObject = battleObject;
        transform.position = battleObject.GetPosition();
        SetRotation(battleObject.rotation);
        transform.localScale = battleObject.scale;
    }

    public override void UpdateObject() {
        // if (!Mathf.Approximately(transform.rotation.z, GetRotation()))
            SetRotation(GetRotation());
        // if (!battleObject.position.Equals(transform.position))
            transform.localPosition = GetPosition();
    }

    public virtual Vector2 GetPosition() {
        return battleObject.position;
    }

    public virtual float GetRotation() {
        return battleObject.rotation;
    }

    public override bool IsSelectable() {
        return battleObject.IsSpawned();
    }

}
