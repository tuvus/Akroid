using UnityEngine;

public abstract class ObjectUI : MonoBehaviour {
    protected SpriteRenderer spriteRenderer { get; private set; }
    public IObject iObject { get; private set; }

    public void Setup(IObject iObject) {
        spriteRenderer = GetComponent<SpriteRenderer>();
        this.iObject = iObject;
        transform.position = iObject.GetPosition();
    }

    public abstract void UpdateObject();

    public void SetRotation(float rotation) {
        transform.localEulerAngles = new Vector3(0, 0, Calculator.SimplifyPositiveRotation360(rotation));
    }

    public abstract bool IsSelectable();

    public Sprite GetSprite() {
        return spriteRenderer.sprite;
    }

    public Color GetSpriteColor() {
        return spriteRenderer.color;
    }

    public virtual void SelectObject(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) { }

    public virtual void UnselectObject() { }
}
