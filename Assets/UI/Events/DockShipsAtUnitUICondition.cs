using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class DockShipsAtUnitUICondition : UIWrapperEventCondition<DockShipsAtUnitCondition> {
    public DockShipsAtUnitUICondition(DockShipsAtUnitCondition conditionLogic, LocalPlayer localPlayer, UIBattleManager uiBattleManager,
        bool visualize = false) : base(conditionLogic, localPlayer, uiBattleManager, visualize) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        Predicate<Ship> shipHasDockCommandToUnit = ship => ship.shipAI.commands.Any((command) =>
            command.commandType == Command.CommandType.Dock && command.destinationStation == conditionLogic.unitToDockTo);

        if (conditionLogic.shipsToDock.Any(s => s.dockedStation != conditionLogic.unitToDockTo && !shipHasDockCommandToUnit(s)))
            objectsToVisualize.Add(uiBattleManager.units[conditionLogic.unitToDockTo]);

        AddShipsToSelect(conditionLogic.shipsToDock
            .Where(s => s.dockedStation != conditionLogic.unitToDockTo && !shipHasDockCommandToUnit(s))
            .Select(s => (ShipUI)uiBattleManager.units[s]).ToList(), objectsToVisualize, buttonsToVisualize);
    }
}
