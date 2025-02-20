using UnityEngine;

/**
 * UnitIconUI manages the icons of units so that they can be visible even when the player is zoomed out.
 * UnitIconUI also manages the selection outline around the unit when the player interacts with the unit.
 */
public class UnitIconUI : MonoBehaviour {
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
    private SpriteRenderer iconUI;
    private float unitsize;

    public void SetupIconUI(UnitUI unitUI, UIManager uIManager) {
        this.unitUI = unitUI;
        this.uIManager = uIManager;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, unselectedAlpha);
        spriteRenderer.sprite = unitUI.unit.unitScriptableObject.sprite;
        engagedVisual = GetComponentInChildren<EngagedVisual>();
        engagedVisual.SetupEngagedVisual(unitUI, uIManager);
        iconUI = transform.GetChild(1).GetComponent<SpriteRenderer>();

        // We want to make the selection outline the right size, however we also want the outline thickness to scale with the size.
        // We can do this by reducing the outline size and increasing the scale of the object
        unitsize = unitUI.unit.GetSize();
        if (unitUI.unit.IsStation()) unitsize *= 2f / 3;
        iconUI.size = unitUI.unit.unitScriptableObject.spriteBounds * unitUI.unit.scale * 6 / unitsize + new Vector2(5, 5);
        iconUI.transform.localScale = Vector2.one * unitsize / 6 / unitUI.unit.scale;
        UpdateFactionColor();
        SetSelected();
    }

    public void UpdateUnitIconUI() {
        if (UpdateIcon()) {
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
    /// Updates the selection strength and size of the icon, may also hide it
    /// </summary>
    /// <returns>True if the icon is visible, false otherwise</returns>
    private bool UpdateIcon() {
        if (uIManager.localPlayer.playerUI.GetShowUnitZoomIndicators() == false ||
            (unitUI.unit.IsStation() && !((Station)unitUI.unit).IsBuilt())
            || !unitUI.IsVisible()) {
            ShowUnitIconUI(false);
            iconUI.enabled = false;
            return false;
        }

        float realSize = uIManager.localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize;
        if (realSize > 1000) {
            // In this case we are zoomed out pretty far and the icon is displayed over the unit
            float imageSize = unitsize * realSize * realSize * 10;
            float size = (Mathf.Pow(imageSize, 1f / 4f) + 0.1f) / unitsize;
            spriteRenderer.sortingOrder = 10;
            spriteRenderer.enabled = true;
            transform.localScale = new Vector2(size, size);
            engagedVisual.UpdateEngagedVisual();
            iconUI.transform.localScale = Vector2.one * unitsize / 8 / unitUI.unit.scale;
        } else if (realSize > 500f) {
            // In this case the is is visible, however it is displayed underneath the unit
            // so that the player can see the real size of the unit
            float size = Mathf.Max(1.2f, realSize / 10 / unitsize);
            spriteRenderer.sortingOrder = -10;
            spriteRenderer.enabled = true;
            transform.localScale = new Vector2(size, size);
            engagedVisual.UpdateEngagedVisual();
            iconUI.transform.localScale = Vector2.one * unitsize / 8 / unitUI.unit.scale;
        } else {
            // In this case the camera is zoomed in so close that we don't want to display the icon at all
            transform.localScale = new Vector2(1, 1);
            spriteRenderer.enabled = false;
            engagedVisual.ShowEngagedVisual(false);
            iconUI.transform.localScale = Vector2.one * unitsize / 6 / unitUI.unit.scale;
        }

        iconUI.enabled = true;

        return true;
    }

    public void ShowUnitIconUI(bool show) {
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
                iconUI.color = new Color(iconUI.color.r, iconUI.color.g, iconUI.color.b, 0f);
                break;
            case SelectionStrength.Selected:
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, selectedAlpha);
                iconUI.color = new Color(iconUI.color.r, iconUI.color.g, iconUI.color.b, 1f);
                break;
            case SelectionStrength.Highlighted:
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, highlightedAlpha);
                iconUI.color = new Color(iconUI.color.r, iconUI.color.g, iconUI.color.b, .4f);
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
