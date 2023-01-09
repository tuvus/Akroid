using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGroupInput : LocalPlayerGameInput {

    public List<UnitGroup> groups = new List<UnitGroup>();

    int fleetNumber = 10;

    public override void Setup() {
        base.Setup();
        for (int i = 0; i < fleetNumber; i++) {
            UnitGroup newGroup = new UnitGroup();
            groups.Add(newGroup);
        }
        GetPlayerInput().Player.ActionGroup1.started += context => GroupButtonPressed(context, 1);
        GetPlayerInput().Player.ActionGroup2.started += context => GroupButtonPressed(context, 2);
        GetPlayerInput().Player.ActionGroup3.started += context => GroupButtonPressed(context, 3);
        GetPlayerInput().Player.ActionGroup4.started += context => GroupButtonPressed(context, 4);
        GetPlayerInput().Player.ActionGroup5.started += context => GroupButtonPressed(context, 5);
        GetPlayerInput().Player.ActionGroup6.started += context => GroupButtonPressed(context, 6);
        GetPlayerInput().Player.ActionGroup7.started += context => GroupButtonPressed(context, 7);
        GetPlayerInput().Player.ActionGroup8.started += context => GroupButtonPressed(context, 8);
        GetPlayerInput().Player.ActionGroup9.started += context => GroupButtonPressed(context, 9);
        GetPlayerInput().Player.ActionGroup0.started += context => GroupButtonPressed(context, 0);
    }

    public override void ChangeFaction() {
        base.ChangeFaction();
        for (int i = 0; i < groups.Count; i++) {
            groups.Clear();
        }
    }

    void GroupButtonPressed(InputAction.CallbackContext context, int buttonNumber) {
        if (LocalPlayer.Instance.ownedUnits == null)
            return;
        if (AdditiveButtonPressed) {
            SelectOnlyControllableUnits();
            AddUnitsToGroup(selectedUnits, buttonNumber);
        } else if (SetButtonPressed) {
            SelectOnlyControllableUnits();
            SetGroupToUnits(selectedUnits, buttonNumber);
        } else if (!SelectGroup(buttonNumber)) {
            selectedUnits.UnselectAllUnits();
            selectedUnits.ClearGroup();
            selectedUnits.CopyGroup(groups[buttonNumber]);
            selectedUnits.SelectAllUnits(UnitSelection.SelectionStrength.Selected);
            if (groups[buttonNumber].groupType == UnitGroup.GroupType.Fleet)
                SetDisplayedFleet(groups[buttonNumber].fleet);
            else
                SetDisplayedUnit();
            selectedGroup = buttonNumber;
        }
    }

    protected override void ClearSelectedUnits() {
        base.ClearSelectedUnits();
        selectedGroup = -1;
    }


    public List<Unit> ConvertShipsToUnits(List<Ship> shipList) {
        List<Unit> unitList = new List<Unit>();
        foreach (var ship in shipList) {
            if (ship != null)
                unitList.Add(ship.GetComponent<Unit>());
        }
        return unitList;
    }

    public void SetGroupToUnits(UnitGroup newGroup, int groupNumber) {
        if (CheckGroupInt(groupNumber)) {
            groups[groupNumber].ClearGroup();
            groups[groupNumber].CopyGroup(newGroup);
        }
    }

    public void AddUnitsToGroup(UnitGroup newGroup, int groupNumber) {
        if (CheckGroupInt(groupNumber)) {
            groups[groupNumber].AddUnits(newGroup);
        }
    }

    bool CheckGroupInt(int groupNumber) {
        if (groupNumber <= fleetNumber - 1 && groupNumber >= 0) {
            return true;
        }
        Debug.LogWarning("fleetNumber given was not in a valid range.");
        return false;

    }
}
