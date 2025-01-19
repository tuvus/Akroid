using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocalPlayerGameInput : LocalPlayerSelectionInput {
    public override void Setup(LocalPlayer localPlayer, UIBattleManager uiBattleManager) {
        base.Setup(localPlayer, uiBattleManager);
        GetPlayerInput().Player.ClearCommands.started += context => ClearCommands();

        GetPlayerInput().Player.PrimaryCommand.performed += context => PrimaryCommandButtonPreformed();
        GetPlayerInput().Player.SeccondaryCommand.performed += context => SecondaryCommandButtonPreformed();
        GetPlayerInput().Player.TertiaryCommand.performed += context => TertiaryCommandButtonPreformed();
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
                localPlayer.playerUI.GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
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
            case ActionType.ColonizeCommand:
                SelectOnlyControllableUnits();
                GenerateColonizationCommand();
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
        if (localPlayer.player.ownedUnits == null)
            return;
        SelectOnlyControllableUnits();
        if (selectedUnits.HasStation() && !selectedUnits.HasShip()) {
            ToggleActionType(ActionType.UndockCombatAtCommand);
        } else if (actionType != ActionType.Selecting && selectedUnits.HasShip()) {
            ToggleActionType(ActionType.MoveCommand);
        }
    }

    void SecondaryCommandButtonPreformed() {
        if (localPlayer.player.ownedUnits == null)
            return;
        SelectOnlyControllableUnits();
        if (selectedUnits.HasStation() && !selectedUnits.HasShip()) {
            ToggleActionType(ActionType.UndockTransportAtCommand);
        } else if (actionType != ActionType.Selecting && selectedUnits.HasShip()) {
            ToggleActionType(ActionType.AttackCommand);
        }
    }

    void TertiaryCommandButtonPreformed() {
        if (localPlayer.player.ownedUnits == null)
            return;
        SelectOnlyControllableUnits();
        if (selectedUnits.HasStation() && !selectedUnits.HasShip()) {
            ToggleActionType(ActionType.UndockAllCombatCommand);
        } else if (actionType != ActionType.Selecting && selectedUnits.HasShip()) {
            if (selectedUnits.ContainsOnlyConstructionShips()) {
                ToggleActionType(ActionType.StationBuilderCommand);
            } else if (selectedUnits.ContainsOnlyScienceShips()) {
                ToggleActionType(ActionType.ResearchCommand);
            } else if (selectedUnits.ContainsOnlyGasCollectionShips()) {
                ToggleActionType(ActionType.CollectGasCommand);
            } else if (selectedUnits.ContainsOnlyTransportShips()) {
                ToggleActionType(ActionType.TransportCommand);
            } else if (selectedUnits.ContainsOnlyColonizerShips()) {
                ToggleActionType(ActionType.ColonizeCommand);
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
        if (mouseOverBattleObject != null && mouseOverBattleObject.battleObject.IsUnit()) {
            LocalPlayer.RelationType relationType = localPlayer.GetRelationToUnit((Unit)mouseOverBattleObject.battleObject);
            if (relationType != LocalPlayer.RelationType.Enemy) {
                if (mouseOverBattleObject.battleObject.IsStation()) {
                    GiveCommandToAllSelectedUnits(Command.CreateDockCommand((Station)mouseOverBattleObject.battleObject),
                        GetCommandAction());
                    localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                } else {
                    GiveCommandToAllSelectedUnits(Command.CreateFollowCommand((Unit)mouseOverBattleObject.battleObject),
                        GetCommandAction());
                    localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                }
            } else {
                GiveCommandToAllSelectedUnits(Command.CreateAttackMoveCommand((Unit)mouseOverBattleObject.battleObject, random),
                    GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.red);
            }

            return;
        }

        GiveCommandToAllSelectedUnits(Command.CreateMoveCommand(GetMouseWorldPosition()), GetCommandAction());
        localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
    }

    void GenerateUndockCombatAtCommand() {
        foreach (var stationUI in selectedUnits.GetAllStations()) {
            Ship firstShip = stationUI.station.GetAllDockedShips().FirstOrDefault(s => s.IsCombatShip());
            if (firstShip != null) {
                if (mouseOverBattleObject != null && mouseOverBattleObject.battleObject.IsUnit()) {
                    if (mouseOverBattleObject.battleObject.IsStation()) {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateDockCommand((Station)mouseOverBattleObject.battleObject),
                            GetCommandAction());
                        localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    } else {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateFollowCommand((Unit)mouseOverBattleObject.battleObject),
                            GetCommandAction());
                        localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    }

                    return;
                }

                firstShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(GetMouseWorldPosition()), GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
            }
        }
    }

    void GenerateAttackCommand() {
        if (mouseOverBattleObject != null && mouseOverBattleObject.battleObject.IsUnit()) {
            LocalPlayer.RelationType relationType = localPlayer.GetRelationToUnit((Unit)mouseOverBattleObject.battleObject);
            if (relationType != LocalPlayer.RelationType.Enemy) {
                GiveCommandToAllSelectedUnits(Command.CreateProtectCommand((Unit)mouseOverBattleObject.battleObject), GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
            } else if (selectedUnits.groupType == SelectionGroup.GroupType.Fleet && mouseOverBattleObject.battleObject.IsShip() &&
                ((Ship)mouseOverBattleObject.battleObject).fleet != null) {
                GiveCommandToAllSelectedUnits(Command.CreateAttackFleetCommand(((Ship)mouseOverBattleObject.battleObject).fleet),
                    GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
            } else {
                GiveCommandToAllSelectedUnits(Command.CreateAttackMoveCommand((Unit)mouseOverBattleObject.battleObject, random),
                    GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.red);
            }

            return;
        }

        GiveCommandToAllSelectedUnits(Command.CreateAttackMoveCommand(GetMouseWorldPosition(), random), GetCommandAction());
        localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.red);
    }

    void GenerateUndockTransportAtCommand() {
        foreach (var stationUI in selectedUnits.GetAllStations()) {
            Ship firstShip = stationUI.station.GetAllDockedShips().FirstOrDefault(s => s.IsTransportShip());
            if (firstShip != null && localPlayer.player.ownedUnits.Contains(firstShip)) {
                if (mouseOverBattleObject != null && mouseOverBattleObject.battleObject.IsUnit()) {
                    if (mouseOverBattleObject.battleObject.IsStation()) {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateDockCommand((Station)mouseOverBattleObject.battleObject),
                            GetCommandAction());
                        localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    } else {
                        firstShip.shipAI.AddUnitAICommand(Command.CreateFollowCommand((Unit)mouseOverBattleObject.battleObject),
                            GetCommandAction());
                        localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                    }

                    return;
                }

                firstShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(GetMouseWorldPosition()), GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
            }
        }
    }

    void GenerateFormationCommand() { }

    void GenerateUndockAllCombatCommand() {
        foreach (var stationUI in selectedUnits.GetAllStations()) {
            foreach (var ship in stationUI.station.GetAllDockedShips().Where(s => s.IsCombatShip())) {
                if (ship != null && localPlayer.player.ownedUnits.Contains(ship)) {
                    if (mouseOverBattleObject != null && mouseOverBattleObject.battleObject.IsUnit()) {
                        if (mouseOverBattleObject.battleObject.IsStation()) {
                            ship.shipAI.AddUnitAICommand(Command.CreateDockCommand((Station)mouseOverBattleObject.battleObject),
                                GetCommandAction());
                            localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                        } else {
                            ship.shipAI.AddUnitAICommand(Command.CreateFollowCommand((Unit)mouseOverBattleObject.battleObject),
                                GetCommandAction());
                            localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.blue);
                        }

                        return;
                    }

                    ship.shipAI.AddUnitAICommand(Command.CreateMoveCommand(GetMouseWorldPosition()), GetCommandAction());
                    localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.green);
                }
            }
        }
    }

    void GenerateStationBuilderCommand() {
        foreach (var shipUI in selectedUnits.GetAllShips().Where(shipUI => shipUI.ship.IsConstructionShip() && shipUI.ship.spawned)) {
            shipUI.ship.shipAI.AddUnitAICommand(
                Command.CreateBuildStationCommand(shipUI.ship.faction, Station.StationType.MiningStation, GetMouseWorldPosition(), random),
                GetCommandAction());
            localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
            return;
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

        if (closestStar == null) return;

        List<ShipUI> allShips = selectedUnits.GetAllShips();
        for (int i = 0; i < allShips.Count; i++) {
            if (allShips[i].ship.IsScienceShip()) {
                allShips[i].ship.shipAI
                    .AddUnitAICommand(Command.CreateResearchCommand(closestStar, localPlayer.player.faction.GetFleetCommand()),
                        GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
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

        if (closestGasCloud == null) return;
        List<ShipUI> allShips = selectedUnits.GetAllShips();
        for (int i = 0; i < allShips.Count; i++) {
            if (allShips[i].ship.IsGasCollectorShip()) {
                allShips[i].ship.shipAI
                    .AddUnitAICommand(
                        Command.CreateCollectGasCommand(closestGasCloud, localPlayer.player.faction.GetFleetCommand()),
                        GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
            }
        }
    }

    void GenerateTransportCommand() {
        Vector2 mousePos = GetMouseWorldPosition();
        Station closestProductionStation = null;
        float closestStationDistance = 0;
        foreach (Station station in localPlayer.GetFaction().stations) {
            float newStationDistance = Vector2.Distance(mousePos, station.position);
            if (closestProductionStation == null || newStationDistance < closestStationDistance) {
                closestProductionStation = station;
                closestStationDistance = newStationDistance;
            }
        }

        if (closestProductionStation == null) return;

        List<ShipUI> allShips = selectedUnits.GetAllShips();
        for (int i = 0; i < allShips.Count; i++) {
            if (allShips[i].ship.IsTransportShip()) {
                allShips[i].ship.shipAI
                    .AddUnitAICommand(
                        Command.CreateTransportCommand(closestProductionStation, localPlayer.GetFaction().GetFleetCommand(),
                            CargoBay.CargoTypes.Metal), GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
            }
        }
    }

    void GenerateColonizationCommand() {
        Vector2 mousePos = GetMouseWorldPosition();
        Planet closestPlanet = null;
        float closestPlanetDistance = 0;
        foreach (Planet planet in BattleManager.Instance.planets) {
            float newPlanetDistance = Vector2.Distance(mousePos, planet.position);
            if (closestPlanet == null || newPlanetDistance < closestPlanetDistance) {
                closestPlanet = planet;
                closestPlanetDistance = newPlanetDistance;
            }
        }

        if (closestPlanet == null) return;
        List<ShipUI> allShips = selectedUnits.GetAllShips();
        for (int i = 0; i < allShips.Count; i++) {
            if (allShips[i].ship.IsColonizerShip()) {
                allShips[i].ship.shipAI.AddUnitAICommand(Command.CreateColonizeCommand(closestPlanet), GetCommandAction());
                localPlayer.GetPlayerUI().GetCommandClick().Click(GetMouseWorldPosition(), Color.yellow);
            }
        }
    }


    void ClearCommands() {
        if (localPlayer.player.ownedUnits == null)
            return;
        SelectOnlyControllableUnits();
        selectedUnits.ClearCommands();
    }

    protected override void CombatUnitButtonPerformed() {
        if (localPlayer.player.ownedUnits == null)
            base.CombatUnitButtonPerformed();
        if (selectedUnits.groupType == SelectionGroup.GroupType.Fleet) {
            if (AdditiveButtonPressed && AltButtonPressed) {
                selectedUnits.fleet.fleet.fleetAI.AddFleetAICommand(Command.CreateDisbandFleetCommand(), Command.CommandAction.Replace);
            } else if (AdditiveButtonPressed) {
                selectedUnits.fleet.fleet.fleetAI.AddFormationCommand(Command.CommandAction.AddToEnd);
            } else if (AltButtonPressed) {
                selectedUnits.fleet.fleet.fleetAI.AddFormationCommand(Command.CommandAction.AddToBegining);
            } else {
                selectedUnits.fleet.fleet.fleetAI.AddFormationCommand();
            }
        } else if (selectedUnits.objects.Any((o) => o.battleObject.IsShip() && ((Ship)o.battleObject).IsCombatShip())) {
            selectedUnits.RemoveAllNonCombatShips();
            selectedUnits.RemoveAnyUnitsNotInHashSet(localPlayer.player.ownedUnits);
            selectedUnits.RemoveAnyNullUnits();
            List<ShipUI> ships = selectedUnits.GetAllShips();
            if (ships.Count > 0) {
                localPlayer.GetFaction().CreateNewFleet("NewFleet", ships.Select(s => s.ship).ToHashSet());
                SelectBattleObjects(new List<BattleObjectUI>() { ships.First() });
            }

            selectedGroup = -1;
        } else {
            base.CombatUnitButtonPerformed();
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
