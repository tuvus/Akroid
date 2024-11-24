using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGroupInput : LocalPlayerGameInput {
    public List<SelectionGroup> groups = new List<SelectionGroup>();

    int fleetNumber = 10;

    public override void Setup() {
        base.Setup();
        for (int i = 0; i < fleetNumber; i++) {
            SelectionGroup newGroup = new SelectionGroup();
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
        if (LocalPlayer.Instance.player.ownedUnits == null)
            return;
        if (AdditiveButtonPressed) {
            SelectOnlyControllableUnits();
            AddUnitsToGroup(selectedUnits, buttonNumber);
        } else if (SetButtonPressed) {
            SelectOnlyControllableUnits();
            SetGroupToUnits(selectedUnits, buttonNumber);
        } else if (!SelectGroup(buttonNumber)) {
            selectedUnits.UnselectAllBattleObjects();
            selectedUnits.ClearGroup();
            selectedUnits.CopyGroup(groups[buttonNumber]);
            selectedUnits.SelectAllBattleObjects(UnitSelection.SelectionStrength.Selected);
            if (groups[buttonNumber].groupType == SelectionGroup.GroupType.Fleet)
                SetDisplayedFleet(groups[buttonNumber].fleet);
            else
                SetDisplayedUnit();
            selectedGroup = buttonNumber;
        }
    }

    protected override void ClearSelectedBattleObjects() {
        base.ClearSelectedBattleObjects();
        selectedGroup = -1;
    }


    public List<Unit> ConvertShipsToUnits(List<Ship> shipList) {
        return shipList.Cast<Unit>().ToList();
    }

    public void SetGroupToUnits(SelectionGroup newGroup, int groupNumber) {
        if (CheckGroupInt(groupNumber)) {
            groups[groupNumber].ClearGroup();
            groups[groupNumber].CopyGroup(newGroup);
        }
    }

    public void AddUnitsToGroup(SelectionGroup newGroup, int groupNumber) {
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
