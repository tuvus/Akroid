using System.Collections.Generic;

public class BuildShipsAtStationUICondition : UIWrapperEventCondition<BuildShipsAtStation> {
    public BuildShipsAtStationUICondition(BuildShipsAtStation conditionLogic, LocalPlayer localPlayer,
        UnitSpriteManager unitSpriteManager, bool visualize = false) : base(conditionLogic, localPlayer, unitSpriteManager, visualize) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualise) {
        // TODO: Add more robust blueprint logic here
        objectsToVisualise.Add(unitSpriteManager.units[conditionLogic.station]);
    }
}
