using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalPlayerGameInput : LocalPlayerSelectionInput {

    protected bool setButtonPressed;

    public override void Setup() {
        base.Setup();
        GetPlayerInput().Player.ClearCommands.started += context => ClearCommands();

        GetPlayerInput().Player.SetModifier.started += context => SetButtonDown();
        GetPlayerInput().Player.SetModifier.canceled += context => SetButtonUp();

        GetPlayerInput().Player.PrimaryCommand.performed += context => PrimaryCommandButtonPerfomed();
        GetPlayerInput().Player.SeccondaryCommand.performed += context => SeccondaryCommandButtonPerfomed();
        GetPlayerInput().Player.TertiaryCommand.performed += context => TertiaryCommandButtonPerfomed();
    }

    protected override void PrimaryMouseDown() {
        base.PrimaryMouseDown();
        switch (actionType) {
            case ActionType.MoveCommand:
                SelectOnlyControllableUnits();
                GenerateMoveCommand();
                SelectGroup(-1);
                if (!additiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.UndockCombatAtCommand:
                SelectOnlyControllableUnits();
                GenerateUndockCombatAtCommand();
                if (!additiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.AttackCommand:
                SelectOnlyControllableUnits();
                GenerateAttackCommand();
                SelectGroup(-1);
                if (!additiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.UndockTransportAtCommand:
                SelectOnlyControllableUnits();
                GenerateUndockTransportAtCommand();
                if (!additiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.FormationCommand:
                SelectOnlyControllableUnits();
                GenerateFormationCommand();
                SelectGroup(-1);
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
                if (!additiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.UndockAllCombatCommand:
                SelectOnlyControllableUnits();
                GenerateUndockAllCombatCommand();
                if (!additiveButtonPressed)
                    actionType = ActionType.None;
                break;
        }
    }

    protected override void PrimaryMouseUp() {
        base.PrimaryMouseUp();
        SetDisplayedUnitToStrongest();
    }

    protected override void AdditiveButtonUp() {
        base.AdditiveButtonUp();
        if (actionType == ActionType.MoveCommand || actionType == ActionType.AttackCommand || actionType == ActionType.FormationCommand)
            actionType = ActionType.None;
    }

    void SetButtonDown() {
        setButtonPressed = true;
    }

    void SetButtonUp() {
        setButtonPressed = false;
    }

    void PrimaryCommandButtonPerfomed() {
        if (LocalPlayer.Instance.ownedUnits == null)
            return;
        selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits);
        if (selectedUnits.HasStation() && !selectedUnits.HasShip()) {
            actionType = ActionType.UndockCombatAtCommand;
        } else if (actionType != ActionType.Selecting && selectedUnits.HasShip()) {
            actionType = ActionType.MoveCommand;
        }
    }

    void SeccondaryCommandButtonPerfomed() {
        if (LocalPlayer.Instance.ownedUnits == null)
            return;
        selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits);
        if (selectedUnits.HasStation() && !selectedUnits.HasShip()) {
            actionType = ActionType.UndockTransportAtCommand;
        } else if (actionType != ActionType.Selecting && selectedUnits.HasShip()) {
            actionType = ActionType.AttackCommand;
        }
    }

    void TertiaryCommandButtonPerfomed() {
        if (LocalPlayer.Instance.ownedUnits == null)
            return;
        selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits);
        selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits);
        if (selectedUnits.HasStation() && !selectedUnits.HasShip()) {
            actionType = ActionType.UndockAllCombatCommand;
        } else if (actionType != ActionType.Selecting && selectedUnits.HasShip()) {
            actionType = ActionType.FormationCommand;
        }
    }

    void GenerateMoveCommand() {
        if (mouseOverUnit != null) {
            UnitSelection.SelectionType selectionType = mouseOverUnit.GetSelectionTypeOfUnit();
            if (selectionType != UnitSelection.SelectionType.Enemy) {
                if (mouseOverUnit.IsStation()) {
                    GiveCommandToAllSelectedUnits(new UnitAICommand(UnitAICommand.CommandType.Dock, (Station)mouseOverUnit), GetCommandAction());
                    LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                } else {
                    GiveCommandToAllSelectedUnits(new UnitAICommand(UnitAICommand.CommandType.Follow, mouseOverUnit), GetCommandAction());
                    LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                }
            } else {
                GiveCommandToAllSelectedUnits(new UnitAICommand(UnitAICommand.CommandType.AttackMoveUnit, mouseOverUnit, true), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.red);
            }
            return;
        }
        GiveCommandToAllSelectedUnits(new UnitAICommand(UnitAICommand.CommandType.Move, GetMouseWorldPosition()), GetCommandAction());
        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
    }

    void GenerateUndockCombatAtCommand() {
        foreach (var station in selectedUnits.GetAllStations()) {
            Ship firstShip = station.GetHanger().GetCombatShip();
            if (firstShip != null) {
                if (mouseOverUnit != null) {
                    if (mouseOverUnit.IsStation()) {
                        firstShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, (Station)mouseOverUnit), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    } else {
                        firstShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Follow, mouseOverUnit), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    }
                    return;
                }
                firstShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Move, GetMouseWorldPosition()), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
            }
        }
    }

    void GenerateAttackCommand() {
        if (mouseOverUnit != null) {
            UnitSelection.SelectionType selectionType = mouseOverUnit.GetSelectionTypeOfUnit();
            if (selectionType != UnitSelection.SelectionType.Enemy) {
                GiveCommandToAllSelectedUnits(new UnitAICommand(UnitAICommand.CommandType.Protect, mouseOverUnit), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
            } else {
                GiveCommandToAllSelectedUnits(new UnitAICommand(UnitAICommand.CommandType.AttackMoveUnit, mouseOverUnit), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.red);
            }
            return;
        }
        GiveCommandToAllSelectedUnits(new UnitAICommand(UnitAICommand.CommandType.AttackMove, GetMouseWorldPosition()), GetCommandAction());
        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.red);
    }

    void GenerateUndockTransportAtCommand() {
        foreach (var station in selectedUnits.GetAllStations()) {
            Ship firstShip = station.GetHanger().GetTransportShip();
            if (firstShip != null && LocalPlayer.Instance.ownedUnits.Contains(firstShip)) {
                if (mouseOverUnit != null) {
                    if (mouseOverUnit.IsStation()) {
                        firstShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, (Station)mouseOverUnit), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    } else {
                        firstShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Follow, mouseOverUnit), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    }
                    return;
                }
                firstShip.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Move, GetMouseWorldPosition()), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
            }
        }
    }

    void GenerateFormationCommand() {
    }

    void GenerateUndockAllCombatCommand() {
        foreach (var station in selectedUnits.GetAllStations()) {
            List<Ship> combatShips = station.GetHanger().GetAllCombatShips();
            foreach (var ship in combatShips) {
                if (ship != null && LocalPlayer.Instance.ownedUnits.Contains(ship)) {
                    if (mouseOverUnit != null) {
                        if (mouseOverUnit.IsStation()) {
                            ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, (Station)mouseOverUnit), GetCommandAction());
                            LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                        } else {
                            ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Follow, mouseOverUnit), GetCommandAction());
                            LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                        }
                        return;
                    }
                    ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Move, GetMouseWorldPosition()), GetCommandAction());
                    LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
                }
            }
        }
    }

    void ClearCommands() {
        if (LocalPlayer.Instance.ownedUnits == null)
            return;
        SelectOnlyControllableUnits();
        selectedUnits.ClearCommands();
    }

    protected ShipAI.CommandAction GetCommandAction() {
        if (altButtonPressed)
            return ShipAI.CommandAction.AddToBegining;
        if (additiveButtonPressed)
            return ShipAI.CommandAction.AddToEnd;
        return ShipAI.CommandAction.Replace;
    }

    void GiveCommandToAllSelectedUnits(UnitAICommand command, ShipAI.CommandAction commandAction) {
        selectedUnits.GiveCommand(command, commandAction);
    }
}
