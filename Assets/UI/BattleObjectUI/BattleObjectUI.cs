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
        if (!Mathf.Approximately(transform.rotation.z, battleObject.rotation))
            SetRotation(battleObject.rotation);
        if (!battleObject.position.Equals(transform.position))
            transform.position = battleObject.position;

    }

    public override bool IsSelectable() {
        return battleObject.IsSpawned();
    }

}
