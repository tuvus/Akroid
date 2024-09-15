using UnityEngine;

public class EngagedVisual : MonoBehaviour {
    Unit unit;
    SpriteRenderer spriteRenderer;
    static float visualTimeMax = .3f;
    static float visualCooldownMax = 3;
    float visualTime;
    float visualCooldownTime;

    public void SetupEngagedVisual(Unit unit) {
        this.unit = unit;
        spriteRenderer = GetComponent<SpriteRenderer>();
        visualTime = 0;
        visualCooldownTime = 0;
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, .5f);
    }

    public void UpdateEngagedVisual() {
        if (!LocalPlayer.Instance.GetPlayerUI().ShowUnitCombatIndicators() ||
            !LocalPlayer.Instance.GetPlayerUI().GetShowUnitZoomIndicators()) {
            spriteRenderer.enabled = false;
            return;
        }

        if (Time.timeScale == 0)
            return;
        if (visualTime > 0) {
            visualTime = Mathf.Max(0, visualTime - Time.unscaledDeltaTime);
            if (visualTime <= 0) {
                visualTime = 0;
                spriteRenderer.enabled = false;
            } else {
                UpdateVisualScale();
            }
        } else {
            if (visualCooldownTime > 0) {
                visualCooldownTime = Mathf.Max(0, visualCooldownTime - Time.unscaledDeltaTime);
            }

            if (visualCooldownTime <= 0 && ShouldShowEngageVisual()) {
                visualTime = visualTimeMax;
                visualCooldownTime = visualCooldownMax;
                ShowEngagedVisual(true);
                UpdateVisualScale();
            }
        }
    }

    bool ShouldShowEngageVisual() {
        if (unit.GetEnemyUnitsInRange().Count > 0)
            return true;
        return false;
    }

    void UpdateVisualScale() {
        float scale = visualTime / visualTimeMax * unit.GetSize();
        if (unit.IsShip())
            scale /= 2;
        transform.localScale = new Vector2(scale, scale);
    }

    public void ShowEngagedVisual(bool show) {
        if (LocalPlayer.Instance.GetPlayerUI().ShowUnitCombatIndicators() &&
            LocalPlayer.Instance.GetPlayerUI().GetShowUnitZoomIndicators()) {
            if (visualTime > 0) {
                spriteRenderer.enabled = show;
            } else {
                spriteRenderer.enabled = false;
            }
        } else {
            visualTime = 0;
            spriteRenderer.enabled = false;
        }
    }
}
