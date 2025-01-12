using System.Collections.Generic;

public class BuildShipsAtStationUICondition : UIWrapperEventCondition<BuildShipsAtStation> {
    public BuildShipsAtStationUICondition(BuildShipsAtStation conditionLogic, LocalPlayer localPlayer,
        UIBattleManager uiBattleManager, bool visualize = false) : base(conditionLogic, localPlayer, uiBattleManager, visualize) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualise) {
        // TODO: Add more robust blueprint logic here
        objectsToVisualise.Add(uiBattleManager.units[conditionLogic.station]);
    }
}
