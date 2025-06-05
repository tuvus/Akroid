using UnityEngine;

public class CommandClick : MonoBehaviour {
    public float maxClickTime;
    private float clickTime;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    public void SetupCommandClick(Camera mainCamera) {
        this.mainCamera = mainCamera;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Click(Vector2 position, Color color) {
        transform.localScale = new Vector2(mainCamera.orthographicSize / 100, mainCamera.orthographicSize / 100);
        transform.position = position;
        clickTime = maxClickTime;
        spriteRenderer.enabled = true;
        spriteRenderer.color = color;
    }

    public void UpdateCommandClick() {
        if (clickTime > 0) {
            clickTime = Mathf.Max(0, clickTime - Time.unscaledDeltaTime);
            if (clickTime == 0) {
                spriteRenderer.enabled = false;
            } else {
                transform.localScale =
                    new Vector2(mainCamera.orthographicSize / 100, mainCamera.orthographicSize / 100);
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b,
                    clickTime / maxClickTime);
            }
        }
    }
}
