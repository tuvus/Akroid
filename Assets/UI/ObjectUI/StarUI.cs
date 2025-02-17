
using UnityEngine;

public class StarUI : BattleObjectUI {
    public Star star { get; private set; }
    private SpriteRenderer glare;

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        this.star = (Star)battleObject;
        glare = transform.GetChild(0).GetComponent<SpriteRenderer>();
        uIManager.uiBattleManager.objectsToUpdate.Add(this);
    }

    public override void UpdateObject() {
        spriteRenderer.color = star.color;
        glare.color = star.color;
    }
}
