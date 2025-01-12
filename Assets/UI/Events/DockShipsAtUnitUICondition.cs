using System;
using System.Collections.Generic;
using System.Linq;

public class DockShipsAtUnitUICondition : UIWrapperEventCondition<DockShipsAtUnitCondition> {
    public DockShipsAtUnitUICondition(DockShipsAtUnitCondition conditionLogic, LocalPlayer localPlayer, UIBattleManager uiBattleManager,
        bool visualize = false) : base(conditionLogic, localPlayer, uiBattleManager, visualize) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize) {
        Predicate<Ship> shipHasDockCommandToUnit = ship => ship.shipAI.commands.Any((command) =>
            command.commandType == Command.CommandType.Dock && command.destinationStation == conditionLogic.unitToDockTo);
        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();

        if (conditionLogic.shipsToDock.Any(s => s.dockedStation != conditionLogic.unitToDockTo && !shipHasDockCommandToUnit(s))) {
            objectsToVisualize.Add(uiBattleManager.units[conditionLogic.unitToDockTo]);
        }

        objectsToVisualize.AddRange(conditionLogic.shipsToDock.Where(s => s.dockedStation != conditionLogic.unitToDockTo &&
            !selectedUnits.Contains(uiBattleManager.units[s]) && !shipHasDockCommandToUnit(s)).Select(s => uiBattleManager.units[s]));
    }
}
