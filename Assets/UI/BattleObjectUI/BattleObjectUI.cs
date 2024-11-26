using UnityEngine;

public abstract class BattleObjectUI : MonoBehaviour {
    public BattleObject battleObject { get; private set; }
    protected SpriteRenderer spriteRenderer { get; private set; }

    public virtual void Setup(BattleObject battleObject) {
        this.battleObject = battleObject;
        transform.position = battleObject.GetPosition();
        transform.localScale = battleObject.scale;
        transform.eulerAngles = new Vector3(0, 0, battleObject.rotation);
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public abstract void UpdateObject();

}
