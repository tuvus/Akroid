using System;
using System.Collections.Generic;
using System.Linq;

public class DockShipsAtUnitUICondition : UIWrapperEventCondition<DockShipsAtUnitCondition> {
    public DockShipsAtUnitUICondition(DockShipsAtUnitCondition conditionLogic, LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager,
        bool visualize = false) : base(conditionLogic, localPlayer, unitSpriteManager, visualize) { }

    public override List<ObjectUI> GetVisualizedObjects() {
        List<ObjectUI> objectsToVisualize = new List<ObjectUI>();
        Predicate<Ship> shipHasDockCommandToUnit = ship => ship.shipAI.commands.Any((command) =>
            command.commandType == Command.CommandType.Dock && command.destinationStation == conditionLogic.unitToDockTo);
        HashSet<UnitUI> selectedUnits = localPlayer.GetLocalPlayerGameInput().GetSelectedUnits().GetAllUnits().ToHashSet();

        if (conditionLogic.shipsToDock.Any(s => s.dockedStation != conditionLogic.unitToDockTo && !shipHasDockCommandToUnit(s))) {
            objectsToVisualize.Add(unitSpriteManager.units[conditionLogic.unitToDockTo]);
        }

        objectsToVisualize.AddRange(conditionLogic.shipsToDock.Where(s => s.dockedStation != conditionLogic.unitToDockTo &&
            !selectedUnits.Contains(unitSpriteManager.units[s]) && !shipHasDockCommandToUnit(s)).Select(s => unitSpriteManager.units[s]));
        return objectsToVisualize;
    }
}
