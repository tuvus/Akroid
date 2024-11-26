using UnityEngine;

public abstract class UnitUI : BattleObjectUI {
    public Unit unit { get; private set; }
    public UnitSelection unitSelection { get; private set; }

    public override void Setup(BattleObject battleObject) {
        base.Setup(battleObject);
        this.unit = (Unit)battleObject;
        spriteRenderer.sprite = unit.unitScriptableObject.sprite;
        unitSelection = transform.GetChild(0).GetComponent<UnitSelection>();
        unitSelection.SetupSelection(unit);
    }
}
