using UnityEngine;

public abstract class ComponentUI : BattleObjectUI {
    public ModuleComponent moduleComponent { get; private set; }
    protected UnitUI unitUI { get; private set; }

    public virtual void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager);
        this.unitUI = unitUI;
        moduleComponent = (ModuleComponent)battleObject;
    }

    public abstract void OnUnitDestroyed();
    public abstract void OnUnitRemoved();

    public override void SetPosition(Vector2 position) {
        transform.localPosition = position;
    }

    public override Vector2 GetPosition() {
        return moduleComponent.GetPosition();
    }

    public override float GetRotation() {
        return moduleComponent.rotation;
    }

    public override bool IsVisible() {
        return base.IsVisible() && unitUI.IsVisible();
    }

    public override void SelectObject(UnitIconUI.SelectionStrength selectionStrength = UnitIconUI.SelectionStrength.Unselected) { }

    public override void UnselectObject() { }
}
