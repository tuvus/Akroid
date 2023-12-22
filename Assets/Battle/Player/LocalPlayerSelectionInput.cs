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

    [SerializeField] protected SelectionGroup selectedUnits;
    private SelectionGroup objectsInSelectionBox;

    protected int selectedGroup = -1;
    float selectedGroupTime = 0;

    public override void Setup() {
        base.Setup();
        selectedUnits = new SelectionGroup();
        objectsInSelectionBox = new SelectionGroup();

        GetPlayerInput().Player.SetModifier.started += context => SetButtonDown();
        GetPlayerInput().Player.SetModifier.canceled += context => SetButtonUp();

        GetPlayerInput().Player.AdditiveModifier.started += context => AdditiveButtonDown();
        GetPlayerInput().Player.AdditiveModifier.canceled += context => AdditiveButtonUp();

        GetPlayerInput().Player.AllCombatUnits.performed += context => AllCombatUnitsButtonPressed();
    }

    public override void ChangeFaction() {
        base.ChangeFaction();
        EndBoxSelection();
        selectedUnits.UnselectAllBattleObjects();
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
            if (rightClickedBattleObject == null) {
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
            selectedUnits.UnselectAllBattleObjects();
            selectedUnits.ClearGroup();
            if (LocalPlayer.Instance.ownedUnits == null)
                return;
            foreach (var unit in LocalPlayer.Instance.ownedUnits) {
                if (unit.IsShip() && ((Ship)unit).IsCombatShip()) {
                    selectedUnits.AddShip((Ship)unit);
                }
            }
            selectedUnits.SelectAllBattleObjects(UnitSelection.SelectionStrength.Selected);
            SetDisplayedUnit();
        }
    }

    void StartBoxSelection(Vector2 mousePosition) {
        actionType = ActionType.Selecting;
        boxStartPosition = mousePosition;
        if (!AdditiveButtonPressed) {
            ClearSelectedBattleObjects();
        }
    }

    void UpdateBoxSelection(Vector2 mousePosition) {
        if (!AdditiveButtonPressed) {
            objectsInSelectionBox.UnselectAllBattleObjects();
            selectedUnits.SelectAllBattleObjects();
        }
        objectsInSelectionBox.ClearGroup();
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

        if (rightClickedBattleObject != null)
            objectsInSelectionBox.AddBattleObject(rightClickedBattleObject);
        if (mouseOverBattleObject != null)
            objectsInSelectionBox.AddBattleObject(mouseOverBattleObject);

        foreach (Unit unit in BattleManager.Instance.GetAllUnits()) {
            if (!unit.IsSelectable() || unit == rightClickedBattleObject || unit == mouseOverBattleObject)
                continue;
            Vector2 screenPosition = GetCamera().WorldToScreenPoint(unit.transform.position);
            if (screenPosition.x > bottomLeft.x && screenPosition.x < topRight.x && screenPosition.y > bottomLeft.y && screenPosition.y < topRight.y)
                objectsInSelectionBox.AddUnit(unit);
        }
        objectsInSelectionBox.SelectAllBattleObjects(UnitSelection.SelectionStrength.Highlighted);
    }

    void EndBoxSelection() {
        actionType = ActionType.None;
        selectionBox.gameObject.SetActive(false);
        if (Vector2.Distance(GetMousePosition(), boxStartPosition) < 25) {
            if (mouseOverBattleObject != null) {
                selectedGroup = -1;
                if (AdditiveButtonPressed && !(selectedUnits.groupType == SelectionGroup.GroupType.Fleet && mouseOverBattleObject.IsShip() && ((Ship)mouseOverBattleObject).fleet == selectedUnits.fleet)) {
                    ToggleSelectedUnit(mouseOverBattleObject);
                } else {
                    SelectBattleObjects(mouseOverBattleObject);
                }
            } else if (!AdditiveButtonPressed) {
                objectsInSelectionBox.SelectAllBattleObjects(UnitSelection.SelectionStrength.Unselected);
                objectsInSelectionBox.ClearGroup();
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
            AddSelectedBattleObjects(objectsInSelectionBox.objects);
        } else {
            SelectBattleObjects(objectsInSelectionBox.objects);
        }
        objectsInSelectionBox.ClearGroup();
    }

    public void SelectBattleObjects(BattleObject battleObject) {
        SelectBattleObjects(new List<BattleObject>() { battleObject });
    }

    public void SelectBattleObjects(List<BattleObject> newBattleObjects) {
        ClearSelectedBattleObjects();
        selectedUnits.AddBattleObjects(newBattleObjects);
        if (!AdditiveButtonPressed && !AltButtonPressed && AreUnitsInUnitGroupInOneFleet(selectedUnits)) {
            Fleet fleet = selectedUnits.GetShip().fleet;
            ClearSelectedBattleObjects();
            selectedUnits.SetFleet(fleet);
            selectedUnits.SelectAllBattleObjects(UnitSelection.SelectionStrength.Selected);
            SetDisplayedFleet(fleet);
        } else {
            selectedUnits.SelectAllBattleObjects(UnitSelection.SelectionStrength.Selected);
            SetDisplayedUnit();
        }
    }

    public void ToggleSelectedUnit(BattleObject newBattleObject) {
        if (selectedUnits.ContainsObject(newBattleObject)) {
            selectedUnits.RemoveBattleObject(newBattleObject);
            newBattleObject.SelectObject(UnitSelection.SelectionStrength.Unselected);
            SetDisplayedUnit();
        } else {
            AddSelectedBattleObjects(newBattleObject);
        }
    }

    public void AddSelectedBattleObjects(BattleObject newBatleObject) {
        AddSelectedBattleObjects(new List<BattleObject>() { newBatleObject });
    }

    public void AddSelectedBattleObjects(List<BattleObject> newBattleObjects) {
        for (int i = newBattleObjects.Count - 1; i >= 0; i--) {
            if (selectedUnits.ContainsObject(newBattleObjects[i]))
                newBattleObjects.RemoveAt(i);
        }
        selectedUnits.AddBattleObjects(newBattleObjects);
        selectedUnits.SelectAllBattleObjects(UnitSelection.SelectionStrength.Selected);
        SetDisplayedUnit();
    }

    /// <summary>
    /// Check the unitGroup to see if it composed of only ships and if all ships belong to the same fleet
    /// </summary>
    /// <returns>true if all ships belong to the same fleet, otherswise returns false </returns>
    bool AreUnitsInUnitGroupInOneFleet(SelectionGroup unitGroup) {
        List<Ship> allShips = unitGroup.GetAllShips();
        if (allShips.Count == 0 || allShips.Count < unitGroup.objects.Count || allShips[0].fleet == null)
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
        displayedBattleObject = strongestUnit;
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
            if (displayedBattleObject != null && selectedGroup != -1)
                SetCameraPosition(displayedBattleObject.GetPosition());
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
    protected virtual void ClearSelectedBattleObjects() {
        selectedUnits.SelectAllBattleObjects(UnitSelection.SelectionStrength.Unselected);
        selectedUnits.ClearGroup();
    }

    public override BattleObject GetDisplayedBattleObject() {
        if ((displayedBattleObject == null || !displayedBattleObject.IsSpawned()) && displayedFleet != null) {
            SetDisplayedFleet(displayedFleet);
        }
        return base.GetDisplayedBattleObject();
    }

    public override void UnitDestroyed(Unit unit) {
        base.UnitDestroyed(unit);
        selectedUnits.RemoveUnit(unit);
    }

    public SelectionGroup GetSelectedUnits() {
        return selectedUnits;
    }
}
