using UnityEngine;

public abstract class ObjectUI : MonoBehaviour {
    protected SpriteRenderer spriteRenderer { get; private set; }

    public void Setup() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public abstract void UpdateObject();

    public void SetRotation(float rotation) {
        transform.localEulerAngles = new Vector3(0, 0, Calculator.SimplifyPositiveRotation360(rotation));
    }

    public abstract bool IsSelectable();

    public Sprite GetSprite() {
        return spriteRenderer.sprite;
    }
}
