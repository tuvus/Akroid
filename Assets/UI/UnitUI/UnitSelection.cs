using UnityEngine;

public class UnitSelection : MonoBehaviour {
    public enum SelectionStrength {
        Unselected = 0,
        Selected = 1,
        Highlighted = 2,
    }

    private const float unselectedAlpha = .6f;
    private const float highlightedAlpha = .8f;
    private const float selectedAlpha = 1f;
    private SpriteRenderer spriteRenderer;
    private EngagedVisual engagedVisual;
    private UnitUI unitUI;
    private UIManager uIManager;
    private SpriteRenderer selectionOutline;

    public void SetupSelection(UnitUI unitUI, UIManager uIManager) {
        this.unitUI = unitUI;
        this.uIManager = uIManager;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, unselectedAlpha);
        spriteRenderer.sprite = unitUI.unit.unitScriptableObject.sprite;
        engagedVisual = GetComponentInChildren<EngagedVisual>();
        engagedVisual.SetupEngagedVisual(unitUI, uIManager);
        selectionOutline = transform.GetChild(1).GetComponent<SpriteRenderer>();

        // We want to make the selection outline the right size, however we also want the outline thickness to scale with the size.
        // We can do this by reducing the outline size and increasing the scale of the object
        float size = unitUI.unit.GetSize();
        selectionOutline.size = unitUI.unit.unitScriptableObject.spriteBounds * 10 / size + new Vector2(5, 5);
        selectionOutline.transform.localScale *= size / 10;
        UpdateFactionColor();
        SetSelected();
    }

    public void UpdateUnitSelection() {
        if (UpdateSelection()) {
            UpdateFactionColor();
        }
    }

    private void UpdateFactionColor() {
        float previousAlpha = spriteRenderer.color.a;
        if (uIManager.GetFactionColoringShown()) {
            spriteRenderer.color = unitUI.unit.faction.GetColorBackgroundTint(previousAlpha);
        } else {
            var relationColor = uIManager.localPlayer.GetColorOfRelationType(uIManager.localPlayer.GetRelationToUnit(unitUI.unit));
            spriteRenderer.color = new Color(relationColor.r, relationColor.g, relationColor.b, previousAlpha);
        }
    }

    /// <summary>
    /// Updates the selection strength of the indicator, may also hide it
    /// </summary>
    /// <returns>True if the indicator is visible, false otherwise</returns>
    private bool UpdateSelection() {
        if (uIManager.localPlayer.playerUI.GetShowUnitZoomIndicators() == false ||
            (unitUI.unit.IsStation() && !((Station)unitUI.unit).IsBuilt())
            || !unitUI.IsVisible()) {
            ShowUnitSelection(false);
            selectionOutline.enabled = false;
            return false;
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
            transform.localScale = new Vector2(1, 1);
            spriteRenderer.enabled = false;
            engagedVisual.ShowEngagedVisual(false);
        }

        selectionOutline.enabled = true;

        return true;
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

    public void SetSelected(SelectionStrength selectionStrength = SelectionStrength.Unselected) {
        switch (selectionStrength) {
            case SelectionStrength.Unselected:
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, unselectedAlpha);
                selectionOutline.color = new Color(selectionOutline.color.r, selectionOutline.color.g, selectionOutline.color.b, 0f);
                break;
            case SelectionStrength.Selected:
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, selectedAlpha);
                selectionOutline.color = new Color(selectionOutline.color.r, selectionOutline.color.g, selectionOutline.color.b, 1f);
                break;
            case SelectionStrength.Highlighted:
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, highlightedAlpha);
                selectionOutline.color = new Color(selectionOutline.color.r, selectionOutline.color.g, selectionOutline.color.b, .4f);
                break;
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
