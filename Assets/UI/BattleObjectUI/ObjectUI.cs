using UnityEngine;

public abstract class ObjectUI : MonoBehaviour {
    protected SpriteRenderer spriteRenderer { get; private set; }

    public void Setup() {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public abstract void UpdateObject();

    public void SetRotation(float rotation) {
        transform.eulerAngles = new Vector3(0, 0, rotation);
    }

    public abstract bool IsSelectable();

    public Sprite GetSprite() {
        return spriteRenderer.sprite;
    }
}
