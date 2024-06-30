using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class LocalPlayerGameInput : LocalPlayerSelectionInput {


    public override void Setup() {
        base.Setup();
        GetPlayerInput().Player.ClearCommands.started += context => ClearCommands();

        GetPlayerInput().Player.PrimaryCommand.performed += context => PrimaryCommandButtonPreformed();
        GetPlayerInput().Player.SeccondaryCommand.performed += context => SecondaryCommandButtonPreformed();
        GetPlayerInput().Player.TertiaryCommand.performed += context => TertiaryCommandButtonPreformed();

        GetPlayerInput().Player.CreateFleet.performed += context => CreateFleetCommand();
    }

    protected override void PrimaryMouseDown() {
        if (actionType != ActionType.None) doingUnitClickAction = true;
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
            case ActionType.StationBuilderCommand:
                SelectOnlyControllableUnits();
                GenerateStationBuilderCommand();
                if (!AdditiveButtonPressed)
                    actionType = ActionType.None;
                break;
            case ActionType.ResearchCommand:
                SelectOnlyControllableUnits();
                GenerateResearchCommand();
                actionType = ActionType.None;
                break;
            case ActionType.CollectGasCommand:
                SelectOnlyControllableUnits();
                GenerateCollectGasCommand();
                actionType = ActionType.None;
                break;
            case ActionType.TransportCommand:
                SelectOnlyControllableUnits();
                GenerateTransportCommand();
                actionType = ActionType.None;
                break;
        }
    }

    protected override void PrimaryMouseUp() {
        base.PrimaryMouseUp();
        doingUnitClickAction = false;
    }

    protected override void AdditiveButtonUp() {
        base.AdditiveButtonUp();
        if (actionType == ActionType.MoveCommand || actionType == ActionType.AttackCommand || actionType == ActionType.FormationCommand)
            actionType = ActionType.None;
    }

    void PrimaryCommandButtonPreformed() {
        if (LocalPlayer.Instance.ownedUnits == null)
            return;
        selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits.ToList());
        if (selectedUnits.HasStation() && !selectedUnits.HasShip()) {
            ToggleActionType(ActionType.UndockCombatAtCommand);
        } else if (actionType != ActionType.Selecting && selectedUnits.HasShip()) {
            ToggleActionType(ActionType.MoveCommand);
        }
    }

    void SecondaryCommandButtonPreformed() {
        if (LocalPlayer.Instance.ownedUnits == null)
            return;
        selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits.ToList());
        if (selectedUnits.HasStation() && !selectedUnits.HasShip()) {
            ToggleActionType(ActionType.UndockTransportAtCommand);
        } else if (actionType != ActionType.Selecting && selectedUnits.HasShip()) {
            ToggleActionType(ActionType.AttackCommand);
        }
    }

    void TertiaryCommandButtonPreformed() {
        if (LocalPlayer.Instance.ownedUnits == null)
            return;
        selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits.ToList());
        selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits.ToList());
        if (selectedUnits.HasStation() && !selectedUnits.HasShip()) {
            ToggleActionType(ActionType.UndockAllCombatCommand);
        } else if (actionType != ActionType.Selecting && selectedUnits.HasShip()) {
            if (selectedUnits.ContainsOnlyConstructionShips()) {
                ToggleActionType(ActionType.StationBuilderCommand);
            } else if (selectedUnits.ContainsOnlyScienceShips()) {
                ToggleActionType(ActionType.ResearchCommand);
            } else if (selectedUnits.ContainsOnlyGasCollectionShips()) {
                ToggleActionType(ActionType.CollectGasCommand);
            } else if (selectedUnits.ContainsOntlyTransportShips()) {
                ToggleActionType(ActionType.TransportCommand);
            } else {
                ToggleActionType(ActionType.FormationCommand);
            }
        }
    }

    private void ToggleActionType(ActionType newActionType) {
        if (actionType == newActionType) {
            actionType = ActionType.None;
        } else {
            actionType = newActionType;
        }
    }

    void GenerateMoveCommand() {
        if (mouseOverBattleObject != null && mouseOverBattleObject.IsUnit()) {
            LocalPlayer.RelationType relationType = LocalPlayer.Instance.GetRelationToUnit((Unit)mouseOverBattleObject);
            if (relationType != LocalPlayer.RelationType.Enemy) {
                if (mouseOverBattleObject.IsStation()) {
                    GiveCommandToAllSelectedUnits(Command.CreateDockCommand((Station)mouseOverBattleObject), GetCommandAction());
                    LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                } else {
                    GiveCommandToAllSelectedUnits(Command.CreateFollowCommand((Unit)mouseOverBattleObject), GetCommandAction());
                    LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                }
            } else {
                GiveCommandToAllSelectedUnits(Command.CreateAttackMoveCommand((Unit)mouseOverBattleObject), GetCommandAction());
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
                if (mouseOverBattleObject != null && mouseOverBattleObject.IsUnit()) {
                    if (mouseOverBattleObject.IsStation()) {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateDockCommand((Station)mouseOverBattleObject), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    } else {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateFollowCommand((Unit)mouseOverBattleObject), GetCommandAction());
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
        if (mouseOverBattleObject != null && mouseOverBattleObject.IsUnit()) {
            LocalPlayer.RelationType relationType = LocalPlayer.Instance.GetRelationToUnit((Unit)mouseOverBattleObject);
            if (relationType != LocalPlayer.RelationType.Enemy) {
                GiveCommandToAllSelectedUnits(Command.CreateProtectCommand((Unit)mouseOverBattleObject), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
            } else if (selectedUnits.groupType == SelectionGroup.GroupType.Fleet && mouseOverBattleObject.IsShip() && ((Ship)mouseOverBattleObject).fleet != null) {
                GiveCommandToAllSelectedUnits(Command.CreateAttackFleetCommand(((Ship)mouseOverBattleObject).fleet), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
            } else {
                GiveCommandToAllSelectedUnits(Command.CreateAttackMoveCommand((Unit)mouseOverBattleObject), GetCommandAction());
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
                if (mouseOverBattleObject != null && mouseOverBattleObject.IsUnit()) {
                    if (mouseOverBattleObject.IsStation()) {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateDockCommand((Station)mouseOverBattleObject), GetCommandAction());
                        LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    } else {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateFollowCommand((Unit)mouseOverBattleObject), GetCommandAction());
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
            HashSet<Ship> combatShips = station.GetHanger().GetAllCombatShips();
            foreach (var ship in combatShips) {
                if (ship != null && LocalPlayer.Instance.ownedUnits.Contains(ship)) {
                    if (mouseOverBattleObject != null && mouseOverBattleObject.IsUnit()) {
                        if (mouseOverBattleObject.IsStation()) {
                            ship.shipAI.AddUnitAICommand(Command.CreateDockCommand((Station)mouseOverBattleObject), GetCommandAction());
                            LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                        } else {
                            ship.shipAI.AddUnitAICommand(Command.CreateFollowCommand((Unit)mouseOverBattleObject), GetCommandAction());
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

    void GenerateStationBuilderCommand() {
        List<Ship> allShips = selectedUnits.GetAllShips();
        for (int i = 0; i < allShips.Count; i++) {
            if (allShips[i].IsConstructionShip() && ((ConstructionShip)allShips[i]).targetStationBlueprint == null) {
                Station newMiningStation = ((ConstructionShip)allShips[i]).CreateStation(GetMouseWorldPosition());
                allShips[i].shipAI.AddUnitAICommand(Command.CreateMoveCommand(newMiningStation.GetPosition()), Command.CommandAction.Replace);
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
                return;
            }
        }
        for (int i = 0; i < allShips.Count; i++) {
            if (allShips[i].IsConstructionShip()) {
                Station newMiningStation = ((ConstructionShip)allShips[i]).CreateStation(GetMouseWorldPosition());
                allShips[i].shipAI.AddUnitAICommand(Command.CreateMoveCommand(newMiningStation.GetPosition()), Command.CommandAction.Replace);
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
                return;
            }
        }
    }

    void GenerateResearchCommand() {
        Vector2 mousePos = GetMouseWorldPosition();
        Star closestStar = null;
        float closestStarDistance = 0;
        foreach (Star star in BattleManager.Instance.stars) {
            float newStarDistance = Vector2.Distance(mousePos, star.position);
            if (closestStar == null || newStarDistance < closestStarDistance) {
                closestStar = star;
                closestStarDistance = newStarDistance;
            }
        }
        List<Ship> allShips = selectedUnits.GetAllShips();
        for (int i = 0; i < allShips.Count; i++) {
            if (allShips[i].IsScienceShip()) {
                allShips[i].shipAI.AddUnitAICommand(Command.CreateResearchCommand(closestStar, LocalPlayer.Instance.faction.GetFleetCommand()), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
            }
        }
    }

    void GenerateCollectGasCommand() {
        Vector2 mousePos = GetMouseWorldPosition();
        GasCloud closestGasCloud = null;
        float closestGasCloudDistance = 0;
        foreach (GasCloud gasCloud in BattleManager.Instance.gasClouds) {
            float newGasCloud = Vector2.Distance(mousePos, gasCloud.position);
            if (closestGasCloud == null || newGasCloud < closestGasCloudDistance) {
                closestGasCloud = gasCloud;
                closestGasCloudDistance = newGasCloud;
            }
        }
        List<Ship> allShips = selectedUnits.GetAllShips();
        for (int i = 0; i < allShips.Count; i++) {
            if (allShips[i].IsGasCollectorShip()) {
                allShips[i].shipAI.AddUnitAICommand(Command.CreateCollectGasCommand(closestGasCloud, LocalPlayer.Instance.faction.GetFleetCommand()), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
            }
        }
    }

    void GenerateTransportCommand() {
        Vector2 mousePos = GetMouseWorldPosition();
        Station closestProductionStation = null;
        float closestStationDistance = 0;
        foreach (Station station in LocalPlayer.Instance.GetFaction().stations) {
            float newStationDistance = Vector2.Distance(mousePos, station.position);
            if (closestProductionStation == null || newStationDistance < closestStationDistance) {
                closestProductionStation = station;
                closestStationDistance = newStationDistance;
            }
        }
        List<Ship> allShips = selectedUnits.GetAllShips();
        for (int i = 0; i < allShips.Count; i++) {
            if (allShips[i].IsTransportShip()) {
                allShips[i].shipAI.AddUnitAICommand(Command.CreateTransportCommand(closestProductionStation, LocalPlayer.Instance.GetFaction().GetFleetCommand()), GetCommandAction());
                LocalPlayer.Instance.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
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
            if (AdditiveButtonPressed && AltButtonPressed) {
                selectedUnits.fleet.FleetAI.AddFleetAICommand(Command.CreateDisbandFleetCommand(), Command.CommandAction.Replace);
            } else if (AdditiveButtonPressed) {
                selectedUnits.fleet.FleetAI.AddFormationCommand(Command.CommandAction.AddToEnd);
            } else if (AltButtonPressed) {
                selectedUnits.fleet.FleetAI.AddFormationCommand(Command.CommandAction.AddToBegining);
            } else {
                selectedUnits.fleet.FleetAI.AddFormationCommand();
            }
        } else {
            selectedUnits.RemoveAllNonCombatShips();
            selectedUnits.RemoveAnyUnitsNotInList(LocalPlayer.Instance.ownedUnits.ToList());
            selectedUnits.RemoveAnyNullUnits();
            HashSet<Ship> ships = selectedUnits.GetAllShips().ToHashSet();
            if (ships.Count > 0) {
                LocalPlayer.Instance.GetFaction().CreateNewFleet("NewFleet", ships);
                SelectBattleObjects(new List<BattleObject>() { ships.First() });
            }
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
