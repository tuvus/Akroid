using UnityEngine;

/**
 * UnitIconUI manages the icons of units so that they can be visible even when the player is zoomed out.
 * UnitIconUI also manages the selection outline around the unit when the player interacts with the unit.
 */
public class UnitIconUI : MonoBehaviour {
    public enum SelectionStrength {
        Unselected = 0,
        Selected = 1,
        Highlighted = 2
    }

    private const float unselectedAlpha = .6f;
    private const float highlightedAlpha = .8f;
    private const float selectedAlpha = 1f;
    private EngagedVisual engagedVisual;
    private SpriteRenderer selectionOutline;
    private SpriteRenderer unitIcon;
    private UIManager uIManager;
    private float unitsize;
    private UnitUI unitUI;

    public void SetupIconUI(UnitUI unitUI, UIManager uIManager) {
        this.unitUI = unitUI;
        this.uIManager = uIManager;
        unitIcon = GetComponent<SpriteRenderer>();
        unitIcon.enabled = false;
        unitIcon.color = new Color(unitIcon.color.r, unitIcon.color.g, unitIcon.color.b,
            unselectedAlpha);
        unitIcon.sprite = unitUI.unit.unitScriptableObject.sprite;
        engagedVisual = GetComponentInChildren<EngagedVisual>();
        engagedVisual.SetupEngagedVisual(unitUI, uIManager);
        selectionOutline = transform.GetChild(1).GetComponent<SpriteRenderer>();

        // We want to make the selection outline the right size, however we also want the outline thickness to scale with the size.
        // We can do this by reducing the outline size and increasing the scale of the object
        unitsize = unitUI.unit.GetSize();
        if (unitUI.unit.IsStation()) unitsize *= 2f / 3;
        selectionOutline.size = unitUI.unit.unitScriptableObject.spriteBounds * unitUI.unit.scale * 6 / unitsize +
            new Vector2(5, 5);
        selectionOutline.transform.localScale = Vector2.one * unitsize / 6 / unitUI.unit.scale;
        UpdateFactionColor();
        SetSelected();
    }

    public void UpdateUnitIconUI() {
        if (UpdateIcon()) {
            UpdateFactionColor();
        }
    }

    public void UpdateFactionColor() {
        float previousAlpha = unitIcon.color.a;
        if (uIManager.GetFactionColoringShown()) {
            unitIcon.color = unitUI.unit.faction.GetColorBackgroundTint(previousAlpha);
        } else {
            Color relationColor =
                uIManager.localPlayer.GetColorOfRelationType(uIManager.localPlayer.GetRelationToUnit(unitUI.unit));
            unitIcon.color = new Color(relationColor.r, relationColor.g, relationColor.b, previousAlpha);
        }
    }

    /// <summary>
    ///     Updates the selection strength and size of the icon, may also hide it
    /// </summary>
    /// <returns>True if the icon is visible, false otherwise</returns>
    private bool UpdateIcon() {
        if (uIManager.localPlayer.playerUI.GetShowUnitZoomIndicators() == false ||
            unitUI.unit.IsStation() && !((Station)unitUI.unit).IsBuilt()
            || !unitUI.IsVisible()) {
            ShowUnitIconUI(false);
            selectionOutline.enabled = false;
            return false;
        }

        float cameraSize = uIManager.localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize;
        if (cameraSize <= 500) {
            // In this case the camera is zoomed in so close that we don't want to display the icon at all
            transform.localScale = new Vector2(1, 1);
            unitIcon.enabled = false;
            engagedVisual.ShowEngagedVisual(false);
            selectionOutline.transform.localScale = Vector2.one * unitsize / 6 / unitUI.unit.scale;
            selectionOutline.enabled = true;
            return false;
        }

        float imageSize = unitsize * cameraSize * cameraSize * 2.23f;
        float size = (Mathf.Pow(imageSize, 1f / 4f) + 0.1f) / unitsize;
        unitIcon.enabled = true;
        transform.localScale = new Vector2(size, size);
        engagedVisual.UpdateEngagedVisual();
        selectionOutline.transform.localScale = Vector2.one * unitsize / 8 / unitUI.unit.scale;
        selectionOutline.enabled = true;

        if (cameraSize > 1000) {
            // In this case we are zoomed out pretty far and the icon is displayed over the unit
            unitIcon.sortingOrder = 10;
        } else {
            // In this case the it is visible, however it is displayed underneath the unit
            // so that the player can see the real size of the unit
            unitIcon.sortingOrder = -10;
        }

        return true;
    }

    public void ShowUnitIconUI(bool show) {
        if (uIManager.localPlayer.GetPlayerUI().GetShowUnitZoomIndicators()) {
            unitIcon.enabled = show;
            engagedVisual.ShowEngagedVisual(show);
        } else {
            unitIcon.enabled = false;
            engagedVisual.ShowEngagedVisual(false);
        }
    }

    public void SetSelected(SelectionStrength selectionStrength = SelectionStrength.Unselected) {
        switch (selectionStrength) {
            case SelectionStrength.Unselected:
                unitIcon.color = new Color(unitIcon.color.r, unitIcon.color.g, unitIcon.color.b,
                    unselectedAlpha);
                selectionOutline.color = new Color(selectionOutline.color.r, selectionOutline.color.g, selectionOutline.color.b, 0f);
                break;
            case SelectionStrength.Selected:
                unitIcon.color = new Color(unitIcon.color.r, unitIcon.color.g, unitIcon.color.b,
                    selectedAlpha);
                selectionOutline.color = new Color(selectionOutline.color.r, selectionOutline.color.g, selectionOutline.color.b, 1f);
                break;
            case SelectionStrength.Highlighted:
                unitIcon.color = new Color(unitIcon.color.r, unitIcon.color.g, unitIcon.color.b,
                    highlightedAlpha);
                selectionOutline.color = new Color(selectionOutline.color.r, selectionOutline.color.g, selectionOutline.color.b, .4f);
                break;
        }
    }

    public float GetSize() {
        if (!unitIcon.enabled)
            return 0;
        return transform.localScale.y;
    }

    public Color GetColor() {
        if (!unitUI.unit.visible) UpdateFactionColor();
        return unitIcon.color;
    }
}
