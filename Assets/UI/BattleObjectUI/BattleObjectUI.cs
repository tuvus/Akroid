using UnityEngine;

public abstract class BattleObjectUI : MonoBehaviour {
    public BattleObject battleObject { get; private set; }
    protected SpriteRenderer spriteRenderer { get; private set; }

    public virtual void Setup(BattleObject battleObject) {
        this.battleObject = battleObject;
        transform.position = battleObject.GetPosition();
        SetRotation(battleObject.rotation);
        transform.localScale = battleObject.scale;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public virtual void UpdateObject() {
        if (!Mathf.Approximately(transform.rotation.z, battleObject.rotation))
            SetRotation(battleObject.rotation);
        if (!battleObject.position.Equals(transform.position))
            transform.position = battleObject.position;

    }

    public void SetRotation(float rotation) {
        transform.eulerAngles = new Vector3(0, 0, rotation);
    }
}
