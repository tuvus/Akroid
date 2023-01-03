using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalPlayerSelectionInput : LocalPlayerInput {

    protected bool additiveButtonPressed;

    [SerializeField] RectTransform selectionBox;
    Vector2 boxStartPosition;
    Vector2 rightClickStartPosition;
    float maxRightClickDistance;

    [SerializeField] protected UnitGroup selectedUnits;
    private UnitGroup unitsInSelectionBox;

    protected int selectedGroup = -1;
    float selectedGroupTime = 0;

    public override void Setup() {
        base.Setup();
        selectedUnits = new UnitGroup();
        unitsInSelectionBox = new UnitGroup();

        GetPlayerInput().Player.AdditiveModifier.started += context => AdditiveButtonDown();
        GetPlayerInput().Player.AdditiveModifier.canceled += context => AdditiveButtonUp();

        GetPlayerInput().Player.AllCombatUnits.performed += context => AllCombatUnitsButtonPressed();
    }

    public override void ChangeFaction() {
        base.ChangeFaction();
        EndBoxSelection();
        selectedUnits.UnselectAllUnits();
        SelectGroup(-1);
    }

    public override void UpdatePlayer() {
        base.UpdatePlayer();
        if (selectedGroup != -1) {
            selectedGroupTime -= Time.unscaledDeltaTime;
            if (selectedGroupTime < 0) {
                selectedGroup = -1;
                selectedGroupTime = 0;
            }
        }
    }

    protected override void PrimaryMouseDown() {
        base.PrimaryMouseDown();
        if (LocalPlayer.Instance.GetPlayerUI().IsAMenueShown())
            return;
        if (actionType == ActionType.None)
            StartBoxSelection(GetMousePosition());
    }

    protected override void PrimaryMouseHeld() {
        base.PrimaryMouseHeld();
        if (LocalPlayer.Instance.GetPlayerUI().IsAMenueShown())
            return;
        if (actionType == ActionType.Selecting)
            UpdateBoxSelection(GetMousePosition());
    }

    protected override void PrimaryMouseUp() {
        base.PrimaryMouseUp();
        if (LocalPlayer.Instance.GetPlayerUI().IsAMenueShown())
            return;
        if (actionType == ActionType.Selecting)
            EndBoxSelection();
    }

    protected override void SecondaryMouseDown() {
        base.SecondaryMouseDown();
        rightClickStartPosition = GetMousePosition();
    }

    protected override void SecondaryMouseHeld() {
        base.SecondaryMouseHeld();
        maxRightClickDistance = Mathf.Max(maxRightClickDistance, Vector2.Distance(rightClickStartPosition, GetMousePosition()));
    }

    protected override void SecondaryMouseUp() {
        base.SecondaryMouseUp();
        //EndBoxSelection();
        if (maxRightClickDistance < 1) {
            if (rightClickedShip == null) {
                //CreateShipCommand(GetMouseWorldPosition());
            } else {
                if (selectedUnits.GetAllShips().Count > 0) {
                    //CreateShipCommand(GetMouseWorldPosition());
                } else {
                    //StartFollowingUnit(rightClickedUnit);
                    //rightClickedUnit = null;
                }
            }
        }
        maxRightClickDistance = 0;
    }

    protected virtual void AdditiveButtonDown() {
        additiveButtonPressed = true;
    }

    protected virtual void AdditiveButtonUp() {
        additiveButtonPressed = false;
    }

    protected virtual void AllCombatUnitsButtonPressed() {
        if (!SelectGroup(10)) {
            selectedUnits.UnselectAllUnits();
            selectedUnits.ClearGroup();
            if (LocalPlayer.Instance.ownedUnits == null)
                return;
            foreach (var unit in LocalPlayer.Instance.ownedUnits) {
                if (unit.IsShip() && ((Ship)unit).IsCombatShip()) {
                    selectedUnits.AddShip((Ship)unit);
                }
            }
            selectedUnits.SelectAllUnits(UnitSelection.SelectionStrength.Selected);
            SetDisplayedUnit();
        }
    }

    void StartBoxSelection(Vector2 mousePosition) {
        actionType = ActionType.Selecting;
        boxStartPosition = mousePosition;
        if (!additiveButtonPressed) {
            ClearSelectedUnits();
        }
    }

    void UpdateBoxSelection(Vector2 mousePosition) {
        if (!additiveButtonPressed) {
            unitsInSelectionBox.UnselectAllUnits();
            selectedUnits.SelectAllUnits();
        }
        unitsInSelectionBox.ClearGroup();
        if (Vector2.Distance(mousePosition, boxStartPosition) < 2) {
            selectionBox.gameObject.SetActive(false);
            return;
        }
        selectionBox.gameObject.SetActive(true);
        float boxWidth = mousePosition.x - boxStartPosition.x;
        float boxHeight = mousePosition.y - boxStartPosition.y;
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(boxWidth), Mathf.Abs(boxHeight));
        selectionBox.anchoredPosition = boxStartPosition + new Vector2(boxWidth / 2, boxHeight / 2);
        Vector2 bottomLeft = selectionBox.anchoredPosition - (selectionBox.sizeDelta / 2);
        Vector2 topRight = selectionBox.anchoredPosition + (selectionBox.sizeDelta / 2);

        if (rightClickedShip != null)
            unitsInSelectionBox.AddUnit(rightClickedShip);
        if (mouseOverUnit != null)
            unitsInSelectionBox.AddUnit(mouseOverUnit);

        foreach (Unit unit in BattleManager.Instance.GetAllUnits()) {
            if (!unit.IsSelectable() || unit == rightClickedShip || unit == mouseOverUnit)
                continue;
            Vector2 screenPosition = GetCamera().WorldToScreenPoint(unit.transform.position);
            if (screenPosition.x > bottomLeft.x && screenPosition.x < topRight.x && screenPosition.y > bottomLeft.y && screenPosition.y < topRight.y)
                unitsInSelectionBox.AddUnit(unit);
        }
        unitsInSelectionBox.SelectAllUnits(UnitSelection.SelectionStrength.Highlighted);
    }

    void EndBoxSelection() {
        actionType = ActionType.None;
        selectionBox.gameObject.SetActive(false);
        if (Vector2.Distance(GetMousePosition(), boxStartPosition) < 2) {
            unitsInSelectionBox.SelectAllUnits(UnitSelection.SelectionStrength.Unselected);
            unitsInSelectionBox.ClearGroup();
            if (mouseOverUnit != null) {
                selectedGroup = -1;
                if (selectedUnits.ContainsUnit(mouseOverUnit)) {
                    if (additiveButtonPressed) {
                        mouseOverUnit.UnselectUnit();
                        selectedUnits.RemoveUnit(mouseOverUnit);
                    }
                    return;
                }
                if (!additiveButtonPressed) {
                    ClearSelectedUnits();
                }
                selectedUnits.AddUnit(mouseOverUnit);
                selectedUnits.SelectAllUnits(UnitSelection.SelectionStrength.Selected);
                displayedUnit = mouseOverUnit;
            }
            return;
        }
        selectedGroup = -1;
        if (!additiveButtonPressed) {
            ClearSelectedUnits();
        }
        selectedUnits.AddUnits(unitsInSelectionBox);
        selectedUnits.SelectAllUnits(UnitSelection.SelectionStrength.Selected);
        unitsInSelectionBox.ClearGroup();
        SetDisplayedUnit();
    }

    public void SetDisplayedUnit() {
        Unit strongestUnit = null;
        foreach (var unit in selectedUnits.GetAllUnits()) {
            if (strongestUnit == null || unit.GetMaxHealth() > strongestUnit.GetMaxHealth())
                strongestUnit = unit;
        }
        displayedUnit = strongestUnit;
    }

    /// <summary>
    /// Sets the selected group to the given integer.
    /// If the group was already selected focuses on the displayed unit and returns true.
    /// </summary>
    /// <param name="selectedGroup"></param>
    /// <returns>True if the group was alreay selected</returns>
    public bool SelectGroup(int selectedGroup) {
        if (this.selectedGroup == selectedGroup) {
            if (displayedUnit != null && selectedGroup != -1)
                SetCameraPosition(displayedUnit.GetPosition());
            this.selectedGroup = -1;
            return true;
        } else {
            this.selectedGroup = selectedGroup;
            selectedGroupTime = 1f;
            return false;
        }
    }

    public void SelectOnlyControllableUnits() {
        selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits);
    }

    protected virtual void ClearSelectedUnits() {
        selectedUnits.SelectAllUnits(UnitSelection.SelectionStrength.Unselected);
        selectedUnits.ClearGroup();
    }

    public override void UnitDestroyed(Unit unit) {
        base.UnitDestroyed(unit);
        selectedUnits.RemoveUnit(unit);
    }

    public UnitGroup GetSelectedUnits() {
        return selectedUnits;
    }
}
