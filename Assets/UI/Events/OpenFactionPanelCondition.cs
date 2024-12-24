using System.Collections.Generic;

public class OpenFactionPanelCondition : UIEventCondition {
    private Faction factionToSelect;

    public OpenFactionPanelCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, Faction factionToSelect,
        bool visualize = false) : base(localPlayer, unitSpriteManager, ConditionType.OpenFactionPanel, visualize) {
        this.factionToSelect = factionToSelect;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        if (factionToSelect == null) return localPlayer.playerUI.playerFactionOverviewUI.displayedObject == null;
        return localPlayer.playerUI.playerFactionOverviewUI.displayedObject == unitSpriteManager.factionUIs[factionToSelect];
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize) { }
}
