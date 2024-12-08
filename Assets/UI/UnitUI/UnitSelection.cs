using UnityEngine;

public class UnitSelection : MonoBehaviour {
    public enum SelectionStrength {
        Unselected = 0,
        Selected = 1,
        Highlighted = 2,
    }

    Color neutralColor = new Color(1, 1, 1);
    Color friendlyColor = new Color(0, 1, 0);
    Color enemyColor = new Color(1, 0.4f, 0.35f);
    Color ownedColor = new Color(0, 1f, 1f);
    float unselectedAlpha = .6f;
    float highlightedAlpha = .8f;
    float selectedAlpha = 1f;
    SpriteRenderer spriteRenderer;
    private EngagedVisual engagedVisual;
    private UnitUI unitUI;
    private UIManager uIManager;

    public void SetupSelection(UnitUI unitUI, UIManager uIManager) {
        this.unitUI = unitUI;
        this.uIManager = uIManager;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
        UpdateFactionColor();
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, unselectedAlpha);
        engagedVisual = GetComponentInChildren<EngagedVisual>();
        engagedVisual.SetupEngagedVisual(unitUI, uIManager);
    }

    public void UpdateFactionColor() {
        float previousAlpha = spriteRenderer.color.a;
        if (uIManager.GetFactionColoringShown()) {
            spriteRenderer.color = unitUI.unit.faction.GetColorTint();
        } else {
            spriteRenderer.color = uIManager.localPlayer.GetColorOfRelationType(uIManager.localPlayer.GetRelationToUnit(unitUI.unit));
        }

        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, previousAlpha);
    }

    public void SetSelected(SelectionStrength selectionStrength = SelectionStrength.Unselected) {
        switch (selectionStrength) {
            case SelectionStrength.Unselected:
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, unselectedAlpha);
                break;
            case SelectionStrength.Selected:
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, selectedAlpha);
                break;
            case SelectionStrength.Highlighted:
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, highlightedAlpha);
                break;
        }
    }

    public void UpdateUnitSelection(bool showIndicator) {
        if (showIndicator == false || (unitUI.unit.IsStation() && !((Station)unitUI.unit).IsBuilt())
            || !unitUI.IsVisible()) {
            ShowUnitSelection(false);
            return;
        }

        float realSize = uIManager.localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize;
        float imageSize = unitUI.unit.GetSize() * realSize * realSize * 10;
        if (realSize > 1000) {
            float size = (Mathf.Pow(imageSize, 1f / 4f) + 0.1f) / unitUI.unit.GetSize();
            spriteRenderer.sortingOrder = 10;
            spriteRenderer.enabled = true;
            transform.localScale = new Vector2(size, size);
            engagedVisual.UpdateEngagedVisual();
        } else if (realSize > 500f) {
            float size = Mathf.Max(1.2f, realSize / 10 / unitUI.unit.GetSize());
            spriteRenderer.sortingOrder = -10;
            spriteRenderer.enabled = true;
            transform.localScale = new Vector2(size, size);
            engagedVisual.UpdateEngagedVisual();
        } else {
            spriteRenderer.enabled = false;
            engagedVisual.ShowEngagedVisual(false);
        }
    }

    public void ShowUnitSelection(bool show) {
        if (uIManager.localPlayer.GetPlayerUI().GetShowUnitZoomIndicators()) {
            spriteRenderer.enabled = show;
            engagedVisual.ShowEngagedVisual(show);
        } else {
            spriteRenderer.enabled = false;
            engagedVisual.ShowEngagedVisual(false);
        }
    }

    public float GetSize() {
        if (!spriteRenderer.enabled)
            return 0;
        return transform.localScale.y;
    }

    public Color GetColor() {
        return spriteRenderer.color;
    }
}
