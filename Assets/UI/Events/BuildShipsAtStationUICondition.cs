using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class BuildShipsAtStationUICondition : UIWrapperEventCondition<BuildShipsAtStation> {
    public BuildShipsAtStationUICondition(BuildShipsAtStation conditionLogic, LocalPlayer localPlayer,
        UIBattleManager uiBattleManager, bool visualize = false) : base(conditionLogic, localPlayer, uiBattleManager, visualize) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualise, List<Button> buttonsToVisualize) {
        List<Ship.ShipBlueprint> shipBlueprintsToBuild = new List<Ship.ShipBlueprint>(conditionLogic.shipBlueprintsToBuild);
        conditionLogic.station.moduleSystem.Get<ConstructionBay>().ForEach(c => c.buildQueue.ForEach(constructionBayBlueprint => {
            Ship.ShipBlueprint constructionBlueprint = shipBlueprintsToBuild.FirstOrDefault(s =>
                constructionBayBlueprint.faction == s.faction &&
                constructionBayBlueprint.shipScriptableObject == s.shipScriptableObject);
            if (constructionBlueprint != null) shipBlueprintsToBuild.Remove(constructionBlueprint);
        }));
        if (shipBlueprintsToBuild.Count == 0) return;

        PlayerStationUI playerStationUI = (PlayerStationUI)localPlayer.playerUI.uIMenus[typeof(StationUI)];
        if (playerStationUI.gameObject.activeSelf && playerStationUI.displayedObject.station == conditionLogic.station) {
            shipBlueprintsToBuild.ForEach(b => {
                Button button = playerStationUI.GetButtonOfShipBlueprint(b);
                if (button != null && !buttonsToVisualize.Contains(button)) buttonsToVisualize.Add(button);
            });
        } else {
            objectsToVisualise.Add(uiBattleManager.units[conditionLogic.station]);
        }
    }
}
