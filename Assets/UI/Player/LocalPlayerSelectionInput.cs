using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalPlayerSelectionInput : LocalPlayerInput {
    public bool AdditiveButtonPressed { get; private set; }
    public bool SetButtonPressed { get; private set; }

    [SerializeField] RectTransform selectionBox;
    Vector2 boxStartPosition;

    [SerializeField] protected SelectionGroup selectedUnits;
    private SelectionGroup objectsInSelectionBox;

    protected int selectedGroup = -1;
    float selectedGroupTime = 0;

    public override void Setup(BattleManager battleManager, LocalPlayer localPlayer, UIBattleManager uiBattleManager) {
        base.Setup(battleManager, localPlayer, uiBattleManager);
        selectedUnits = new SelectionGroup();
        objectsInSelectionBox = new SelectionGroup();

        GetPlayerInput().Player.SetModifier.started += context => SetButtonDown();
        GetPlayerInput().Player.SetModifier.canceled += context => SetButtonUp();

        GetPlayerInput().Player.AdditiveModifier.started += context => AdditiveButtonDown();
        GetPlayerInput().Player.AdditiveModifier.canceled += context => AdditiveButtonUp();

        GetPlayerInput().Player.Deselct.performed += context => DeselectButtonPerformed();
        GetPlayerInput().Player.CombatUnitCommand.performed += context => CombatUnitButtonPerformed();
    }

    public override void ChangeFaction() {
        base.ChangeFaction();
        EndBoxSelection();
        selectedUnits.UnselectAllBattleObjects();
        SelectGroup(-1);
    }

    public override void UpdatePlayer() {
        if (mouseOverBattleObject != null && !selectedUnits.ContainsObject(mouseOverBattleObject) &&
            !selectedUnits.ContainsObject(mouseOverBattleObject)) {
            mouseOverBattleObject.UnselectObject();
        }

        base.UpdatePlayer();

        if (mouseOverBattleObject != null && !selectedUnits.ContainsObject(mouseOverBattleObject) &&
            !selectedUnits.ContainsObject(mouseOverBattleObject) && !localPlayer.playerUI.IsAMenueShown()) {
            mouseOverBattleObject.SelectObject(UnitIconUI.SelectionStrength.Highlighted);
        }

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
        if (localPlayer.GetPlayerUI().IsAMenueShown())
            return;
        if (actionType == ActionType.None)
            StartBoxSelection(GetMousePosition());
    }

    protected override void PrimaryMouseHeld() {
        base.PrimaryMouseHeld();
        if (localPlayer.GetPlayerUI().IsAMenueShown())
            return;
        if (actionType == ActionType.Selecting)
            UpdateBoxSelection(GetMousePosition());
    }

    protected override void PrimaryMouseUp() {
        base.PrimaryMouseUp();
        if (localPlayer.GetPlayerUI().IsAMenueShown())
            return;
        if (actionType == ActionType.Selecting)
            EndBoxSelection();
    }

    protected override void SecondaryMouseUp() {
        if (localPlayer.playerUI.IsAMenueShown() || maxRightClickDistance >= 10 + mainCamera.orthographicSize / 100) {
            base.SecondaryMouseUp();
        } else {
            base.SecondaryMouseUp();
            if (displayedBattleObject == null) EndBoxSelection();
        }
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

    protected virtual void DeselectButtonPerformed() {
        SelectGroup(-1);
        ClearSelectedBattleObjects();
        SetDisplayedUnit();
    }

    /// <summary>
    /// Handles operations related to fleets and combat ships.
    /// If no combat ship is selected it selects all combat ships.
    /// If there is a combat ship selected then it creates a fleet.
    /// If there is a fleet selected it tells them to go into a formation.
    /// </summary>
    protected virtual void CombatUnitButtonPerformed() {
        if (!SelectGroup(10)) {
            selectedUnits.UnselectAllBattleObjects();
            selectedUnits.ClearGroup();
            if (localPlayer.player.ownedUnits == null)
                return;
            foreach (var unit in localPlayer.player.ownedUnits) {
                if (unit.IsShip() && ((Ship)unit).IsCombatShip()) {
                    selectedUnits.AddShip((ShipUI)uiBattleManager.units[unit]);
                }
            }

            selectedUnits.SelectAllBattleObjects(UnitIconUI.SelectionStrength.Selected);
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
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(boxWidth), Mathf.Abs(boxHeight)) / GetScreenScale();
        selectionBox.position = boxStartPosition + new Vector2(boxWidth / 2, boxHeight / 2);
        Vector2 bottomLeft = new Vector2(Mathf.Min(boxStartPosition.x, mousePosition.x), Mathf.Min(boxStartPosition.y, mousePosition.y));
        Vector2 topRight = new Vector2(Mathf.Max(boxStartPosition.x, mousePosition.x), Mathf.Max(boxStartPosition.y, mousePosition.y));

        if (rightClickedBattleObject != null)
            objectsInSelectionBox.AddBattleObject(rightClickedBattleObject);
        if (mouseOverBattleObject != null)
            objectsInSelectionBox.AddBattleObject(mouseOverBattleObject);

        foreach (UnitUI unitUI in uiBattleManager.units.Values) {
            if (!unitUI.IsSelectable() || unitUI == rightClickedBattleObject || unitUI == mouseOverBattleObject)
                continue;
            Vector2 screenPosition = GetCamera().WorldToScreenPoint(unitUI.unit.position);
            if (screenPosition.x > bottomLeft.x && screenPosition.x < topRight.x && screenPosition.y > bottomLeft.y &&
                screenPosition.y < topRight.y)
                objectsInSelectionBox.AddUnit(unitUI);
        }

        objectsInSelectionBox.SelectAllBattleObjects(UnitIconUI.SelectionStrength.Highlighted);
    }


    void EndBoxSelection() {
        actionType = ActionType.None;
        selectionBox.gameObject.SetActive(false);
        rightClickedBattleObject = null;
        if (Vector2.Distance(GetMousePosition(), boxStartPosition) < 25) {
            if (mouseOverBattleObject != null) {
                selectedGroup = -1;
                if (AdditiveButtonPressed && !(selectedUnits.groupType == SelectionGroup.GroupType.Fleet &&
                    mouseOverBattleObject.battleObject.IsShip() &&
                    ((Ship)mouseOverBattleObject.battleObject).fleet == selectedUnits.fleet.fleet)) {
                    ToggleSelectedUnit(mouseOverBattleObject);
                } else {
                    SelectBattleObjects(mouseOverBattleObject);
                }
            } else if (!AdditiveButtonPressed) {
                objectsInSelectionBox.SelectAllBattleObjects(UnitIconUI.SelectionStrength.Unselected);
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

    public void SelectBattleObjects(BattleObjectUI battleObject) {
        SelectBattleObjects(new List<BattleObjectUI>() { battleObject });
    }

    public void SelectBattleObjects(List<BattleObjectUI> newBattleObjects) {
        ClearSelectedBattleObjects();
        selectedUnits.AddBattleObjects(newBattleObjects);
        if (!AdditiveButtonPressed && !AltButtonPressed && AreUnitsInUnitGroupInOneFleet(selectedUnits)) {
            FleetUI fleetUI = uiBattleManager.fleetUIs[selectedUnits.GetShip().ship.fleet];
            ClearSelectedBattleObjects();
            selectedUnits.SetFleet(fleetUI);
            selectedUnits.SelectAllBattleObjects(UnitIconUI.SelectionStrength.Selected);
            SetDisplayedFleet(fleetUI);
        } else {
            selectedUnits.SelectAllBattleObjects(UnitIconUI.SelectionStrength.Selected);
            SetDisplayedUnit();
        }
    }

    public void ToggleSelectedUnit(BattleObjectUI newBattleObject) {
        if (selectedUnits.ContainsObject(newBattleObject)) {
            selectedUnits.RemoveBattleObject(newBattleObject);
            // newBattleObject.SelectObject(UnitSelection.SelectionStrength.Unselected);
            SetDisplayedUnit();
        } else {
            AddSelectedBattleObjects(newBattleObject);
        }
    }

    public void AddSelectedBattleObjects(BattleObjectUI newBattleObject) {
        AddSelectedBattleObjects(new List<BattleObjectUI>() { newBattleObject });
    }

    public void AddSelectedBattleObjects(List<BattleObjectUI> newBattleObjects) {
        for (int i = newBattleObjects.Count - 1; i >= 0; i--) {
            if (selectedUnits.ContainsObject(newBattleObjects[i]))
                newBattleObjects.RemoveAt(i);
        }

        selectedUnits.AddBattleObjects(newBattleObjects);
        selectedUnits.SelectAllBattleObjects(UnitIconUI.SelectionStrength.Selected);
        SetDisplayedUnit();
    }

    /// <summary>
    /// Check the unitGroup to see if it composed of only ships and if all ships belong to the same fleet
    /// </summary>
    /// <returns>true if all ships belong to the same fleet, otherswise returns false </returns>
    bool AreUnitsInUnitGroupInOneFleet(SelectionGroup unitGroup) {
        List<ShipUI> allShips = unitGroup.GetAllShips();
        if (allShips.Count == 0 || allShips.Count < unitGroup.objects.Count || allShips[0].ship.fleet == null)
            return false;
        for (int i = 1; i < allShips.Count; i++) {
            if (allShips[i].ship.fleet != allShips[0].ship.fleet)
                return false;
        }

        return true;
    }

    public void SetDisplayedUnit() {
        displayedFleet = null;
        UnitUI strongestUnit = null;
        foreach (var unitUI in selectedUnits.GetAllUnits()) {
            if (strongestUnit == null || unitUI.unit.GetMaxHealth() > strongestUnit.unit.GetMaxHealth())
                strongestUnit = unitUI;
        }

        displayedBattleObject = strongestUnit;
        if (displayedBattleObject == null && selectedUnits.objects.Count > 0) displayedBattleObject = selectedUnits.objects.First();
    }

    public void SetDisplayedFleet(FleetUI fleet) {
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
                SetCameraPosition(displayedBattleObject.battleObject.GetPosition());
            this.selectedGroup = -1;
            return true;
        } else {
            this.selectedGroup = selectedGroup;
            selectedGroupTime = 1f;
            return false;
        }
    }

    public void SelectOnlyControllableUnits() {
        selectedUnits.RemoveAnyUnitsNotInHashSet(localPlayer.player.ownedUnits);
        if (selectedUnits.fleet != null && (selectedUnits.fleet.fleet.faction != localPlayer.player.faction
            || !selectedUnits.fleet.fleet.ships.Any(s => localPlayer.player.ownedUnits.Contains(s)))) {
            selectedUnits.UnselectAllBattleObjects();
            selectedUnits.fleet = null;
            selectedUnits.groupType = SelectionGroup.GroupType.None;
        }

        if (selectedUnits.GetAllUnits().Count == 0) {
            selectedUnits.fleet = null;
            selectedUnits.groupType = SelectionGroup.GroupType.None;
            displayedFleet = null;
            displayedBattleObject = null;
        }
    }

    /// <summary>
    /// Unselects all selected units and then clears the selectUnits group
    /// </summary>
    protected virtual void ClearSelectedBattleObjects() {
        selectedUnits.SelectAllBattleObjects(UnitIconUI.SelectionStrength.Unselected);
        selectedUnits.ClearGroup();
    }

    public override BattleObjectUI GetDisplayedBattleObject() {
        if ((displayedBattleObject == null || !displayedBattleObject.battleObject.IsSpawned()) && displayedFleet != null) {
            SetDisplayedFleet(displayedFleet);
        }

        return base.GetDisplayedBattleObject();
    }

    public override void UnitDestroyed(Unit unit) {
        base.UnitDestroyed(unit);
        selectedUnits.RemoveUnit(uiBattleManager.units[unit]);
    }

    public SelectionGroup GetSelectedUnits() {
        return selectedUnits;
    }
}
