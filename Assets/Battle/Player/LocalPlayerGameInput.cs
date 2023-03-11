using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalPlayerGameInput : LocalPlayerSelectionInput {


    public override void Setup() {
        base.Setup();
        GetPlayerInput().Player.ClearCommands.started += context => ClearCommands();

        GetPlayerInput().Player.PrimaryCommand.performed += context => PrimaryCommandButtonPerfomed();
        GetPlayerInput().Player.SeccondaryCommand.performed += context => SeccondaryCommandButtonPerfomed();
        GetPlayerInput().Player.TertiaryCommand.performed += context => TertiaryCommandButtonPerfomed();

        GetPlayerInput().Player.CreateFleet.performed += context => CreateFleetCommand();
    }

    protected override void PrimaryMouseDown() {
        base.PrimaryMouseDown();
        switch (actionType) {
            case ActionType.MoveCommand:
                SelectOnlyControllableUnits();
                GenerateMoveCommand();
                SelectGroup(-1);
                if (!AdditiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.UndockCombatAtCommand:
                SelectOnlyControllableUnits();
                GenerateUndockCombatAtCommand();
                if (!AdditiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.AttackCommand:
                SelectOnlyControllableUnits();
                GenerateAttackCommand();
                SelectGroup(-1);
                if (!AdditiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.UndockTransportAtCommand:
                SelectOnlyControllableUnits();
                GenerateUndockTransportAtCommand();
                if (!AdditiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.FormationCommand:
                SelectOnlyControllableUnits();
                GenerateFormationCommand();
                SelectGroup(-1);
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
                if (!AdditiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.UndockAllCombatCommand:
                SelectOnlyControllableUnits();
                GenerateUndockAllCombatCommand();
                if (!AdditiveButtonPressed)
                    actionType = ActionType.None;
                break;
        }
    }

    protected override void PrimaryMouseUp() {
        base.PrimaryMouseUp();
    }

    protected override void AdditiveButtonUp() {
        base.AdditiveButtonUp();
        if (actionType == ActionType.MoveCommand || actionType == ActionType.AttackCommand || actionType == ActionType.FormationCommand)
            actionType = ActionType.None;
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
                    GiveCommandToAllSelectedUnits(Command.CreateDockCommand((Station)mouseOverUnit), GetCommandAction());
                    LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                } else {
                    GiveCommandToAllSelectedUnits(Command.CreateFollowCommand(mouseOverUnit), GetCommandAction());
                    LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                }
            } else {
                GiveCommandToAllSelectedUnits(Command.CreateAttackMoveCommand(mouseOverUnit), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.red);
            }
            return;
        }
        GiveCommandToAllSelectedUnits(Command.CreateMoveCommand(GetMouseWorldPosition()), GetCommandAction());
        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
    }

    void GenerateUndockCombatAtCommand() {
        foreach (var station in selectedUnits.GetAllStations()) {
            Ship firstShip = station.GetHanger().GetCombatShip();
            if (firstShip != null) {
                if (mouseOverUnit != null) {
                    if (mouseOverUnit.IsStation()) {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateDockCommand((Station)mouseOverUnit), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    } else {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateFollowCommand(mouseOverUnit), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    }
                    return;
                }
                firstShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(GetMouseWorldPosition()), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
            }
        }
    }

    void GenerateAttackCommand() {
        if (mouseOverUnit != null) {
            UnitSelection.SelectionType selectionType = mouseOverUnit.GetSelectionTypeOfUnit();
            if (selectionType != UnitSelection.SelectionType.Enemy) {
                GiveCommandToAllSelectedUnits(Command.CreateProtectCommand(mouseOverUnit), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
            } else if (selectedUnits.groupType == SelectionGroup.GroupType.Fleet && mouseOverUnit.IsShip() && ((Ship)mouseOverUnit).fleet != null) {
                GiveCommandToAllSelectedUnits(Command.CreateAttackFleetCommand(((Ship)mouseOverUnit).fleet), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
            } else {
                GiveCommandToAllSelectedUnits(Command.CreateAttackMoveCommand(mouseOverUnit), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.red);
            }
            return;
        }
        GiveCommandToAllSelectedUnits(Command.CreateAttackMoveCommand(GetMouseWorldPosition()), GetCommandAction());
        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.red);
    }

    void GenerateUndockTransportAtCommand() {
        foreach (var station in selectedUnits.GetAllStations()) {
            Ship firstShip = station.GetHanger().GetTransportShip();
            if (firstShip != null && LocalPlayer.Instance.ownedUnits.Contains(firstShip)) {
                if (mouseOverUnit != null) {
                    if (mouseOverUnit.IsStation()) {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateDockCommand((Station)mouseOverUnit), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    } else {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateFollowCommand(mouseOverUnit), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    }
                    return;
                }
                firstShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(GetMouseWorldPosition()), GetCommandAction());
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
                            ship.shipAI.AddUnitAICommand(Command.CreateDockCommand((Station)mouseOverUnit), GetCommandAction());
                            LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                        } else {
                            ship.shipAI.AddUnitAICommand(Command.CreateFollowCommand(mouseOverUnit), GetCommandAction());
                            LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                        }
                        return;
                    }
                    ship.shipAI.AddUnitAICommand(Command.CreateMoveCommand(GetMouseWorldPosition()), GetCommandAction());
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

    protected virtual void CreateFleetCommand() {
        if (LocalPlayer.Instance.ownedUnits == null)
            return;
        if (selectedUnits.groupType == SelectionGroup.GroupType.Fleet) {
            if (AdditiveButtonPressed)
                selectedUnits.fleet.FleetAI.AddFormationCommand(Command.CommandAction.AddToEnd);
            else if (AltButtonPressed)
                selectedUnits.fleet.FleetAI.AddFormationCommand(Command.CommandAction.AddToBegining);
            else
                selectedUnits.fleet.FleetAI.AddFormationCommand();
        } else {
            selectedUnits.RemoveAllNonCombatShips();
            selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits);
            List<Ship> ships = selectedUnits.GetAllShips();
            if (ships.Count > 0) {
                selectedUnits.SetFleet(LocalPlayer.Instance.GetFaction().CreateNewFleet("NewFleet", ships));
            }
            SetDisplayedUnit();
            selectedGroup = -1;
        }
    }

    protected Command.CommandAction GetCommandAction() {
        if (AltButtonPressed)
            return Command.CommandAction.AddToBegining;
        if (AdditiveButtonPressed)
            return Command.CommandAction.AddToEnd;
        return Command.CommandAction.Replace;
    }

    void GiveCommandToAllSelectedUnits(Command command, Command.CommandAction commandAction) {
        selectedUnits.GiveCommand(command, commandAction);
    }
}
