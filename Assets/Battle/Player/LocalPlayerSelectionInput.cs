using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class LocalPlayerSelectionInput : LocalPlayerInput {

    public bool AdditiveButtonPressed { get; private set; }
    public bool SetButtonPressed { get; private set; }

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

        GetPlayerInput().Player.SetModifier.started += context => SetButtonDown();
        GetPlayerInput().Player.SetModifier.canceled += context => SetButtonUp();

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
            if (rightClickedUnit == null) {
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

    void SetButtonDown() {
        SetButtonPressed = true;
    }

    void SetButtonUp() {
        SetButtonPressed = false;
    }

    protected virtual void AdditiveButtonDown() {
        AdditiveButtonPressed = true;
    }

    protected virtual void AdditiveButtonUp() {
        AdditiveButtonPressed = false;
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
        if (!AdditiveButtonPressed) {
            ClearSelectedUnits();
        }
    }

    void UpdateBoxSelection(Vector2 mousePosition) {
        if (!AdditiveButtonPressed) {
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

        if (rightClickedUnit != null)
            unitsInSelectionBox.AddUnit(rightClickedUnit);
        if (mouseOverUnit != null)
            unitsInSelectionBox.AddUnit(mouseOverUnit);

        foreach (Unit unit in BattleManager.Instance.GetAllUnits()) {
            if (!unit.IsSelectable() || unit == rightClickedUnit || unit == mouseOverUnit)
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
        if (Vector2.Distance(GetMousePosition(), boxStartPosition) < 25) {
            if (mouseOverUnit != null) {
                selectedGroup = -1;
                if (AdditiveButtonPressed) {
                    ToggleSelectedUnit(mouseOverUnit);
                } else {
                    SelectUnits(mouseOverUnit);
                }
            } else if (!AdditiveButtonPressed) {
                unitsInSelectionBox.SelectAllUnits(UnitSelection.SelectionStrength.Unselected);
                unitsInSelectionBox.ClearGroup();
            } else {
                if (displayedFleet != null) {
                    SetDisplayedFleet(displayedFleet);
                } else {
                    SetDisplayedUnit();
                }
            }
            return;
        }
        selectedGroup = -1;
        if (AdditiveButtonPressed) {
            AddSelectedUnits(unitsInSelectionBox.GetAllUnits());
        } else {
            SelectUnits(unitsInSelectionBox.GetAllUnits());
        }
        unitsInSelectionBox.ClearGroup();
    }

    public void SelectUnits(Unit unit) {
        SelectUnits(new List<Unit>() { unit });
    }

    public void SelectUnits(List<Unit> newUnits) {
        ClearSelectedUnits();
        selectedUnits.AddUnits(newUnits);
        if (!AdditiveButtonPressed && !AltButtonPressed && AreUnitsInUnitGroupInOneFleet(selectedUnits)) {
            Fleet fleet = selectedUnits.GetShip().fleet;
            ClearSelectedUnits();
            selectedUnits.SetFleet(fleet);
            selectedUnits.SelectAllUnits(UnitSelection.SelectionStrength.Selected);
            SetDisplayedFleet(fleet);
        } else {
            selectedUnits.SelectAllUnits(UnitSelection.SelectionStrength.Selected);
            SetDisplayedUnit();
        }
    }

    public void ToggleSelectedUnit(Unit newUnit) {
        if (selectedUnits.ContainsUnit(newUnit)) {
            selectedUnits.RemoveUnit(newUnit);
            newUnit.SelectUnit(UnitSelection.SelectionStrength.Unselected);
            SetDisplayedUnit();
        } else {
            AddSelectedUnits(newUnit);
        }
    }

    public void AddSelectedUnits(Unit newUnit) {
        AddSelectedUnits(new List<Unit>() { newUnit });
    }

    public void AddSelectedUnits(List<Unit> newUnits) {
        for (int i = newUnits.Count - 1; i >= 0; i--) {
            if (selectedUnits.ContainsUnit(newUnits[i]))
                newUnits.RemoveAt(i);
        }
        selectedUnits.AddUnits(newUnits);
        selectedUnits.SelectAllUnits(UnitSelection.SelectionStrength.Selected);
        SetDisplayedUnit();
    }

    /// <summary>
    /// Check the unitGroup to see if it composed of only ships and if all ships belong to the same fleet
    /// </summary>
    /// <returns>true if all ships belong to the same fleet, otherswise returns false </returns>
    bool AreUnitsInUnitGroupInOneFleet(UnitGroup unitGroup) {
        List<Ship> allShips = unitGroup.GetAllShips();
        if (allShips.Count == 0 || allShips.Count < unitGroup.units.Count || allShips[0].fleet == null)
            return false;
        for (int i = 1; i < allShips.Count; i++) {
            if (allShips[i].fleet != allShips[0].fleet)
                return false;
        }
        return true;
    }

    public void SetDisplayedUnit() {
        displayedFleet = null;
        Unit strongestUnit = null;
        foreach (var unit in selectedUnits.GetAllUnits()) {
            if (strongestUnit == null || unit.GetMaxHealth() > strongestUnit.GetMaxHealth())
                strongestUnit = unit;
        }
        displayedUnit = strongestUnit;
    }

    public void SetDisplayedFleet(Fleet fleet) {
        SetDisplayedUnit();
        displayedFleet = fleet;
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

    /// <summary>
    /// Unselects all selected units and then clears the selectUnits group
    /// </summary>
    protected virtual void ClearSelectedUnits() {
        selectedUnits.SelectAllUnits(UnitSelection.SelectionStrength.Unselected);
        selectedUnits.ClearGroup();
    }

    public override Unit GetDisplayedUnit() {
        if ((displayedUnit == null || !displayedUnit.IsSpawned()) && displayedFleet != null) {
            SetDisplayedFleet(displayedFleet);
        }
        return base.GetDisplayedUnit();
    }

    public override void UnitDestroyed(Unit unit) {
        base.UnitDestroyed(unit);
        selectedUnits.RemoveUnit(unit);
    }

    public UnitGroup GetSelectedUnits() {
        return selectedUnits;
    }
}
