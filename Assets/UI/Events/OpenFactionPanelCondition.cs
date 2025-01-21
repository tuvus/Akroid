using System.Collections.Generic;
using UnityEngine.UI;

public class OpenFactionPanelCondition : UIEventCondition {
    private Faction factionToSelect;

    public OpenFactionPanelCondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, Faction factionToSelect,
        bool visualize = false) : base(localPlayer, uiBattleManager, ConditionType.OpenFactionPanel, visualize) {
        this.factionToSelect = factionToSelect;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        if (factionToSelect == null) return localPlayer.playerUI.playerFactionOverviewUI.displayedObject == null;
        return localPlayer.playerUI.playerFactionOverviewUI.displayedObject == uiBattleManager.factionUIs[factionToSelect];
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        if (!localPlayer.playerUI.playerFactionOverviewUI.gameObject.activeSelf ||
            localPlayer.playerUI.playerFactionOverviewUI.displayedObject != localPlayer.GetFactionUI())
            buttonsToVisualize.Add(localPlayer.playerUI.factionOverviewButton);
    }
}
