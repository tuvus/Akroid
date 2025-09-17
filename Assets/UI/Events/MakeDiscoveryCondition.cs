using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MakeDiscoveryCondition : UICondition
{
    private readonly Faction factionToResearch;
    private int discoveries;

    public MakeDiscoveryCondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, Faction factionToResearch,
        bool visualize = false) : base(localPlayer, uiBattleManager, ConditionType.OpenFactionPanel, visualize) {
        this.factionToResearch = factionToResearch;
        discoveries = factionToResearch.discoveries + 1;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        if (factionToResearch == null) return localPlayer.playerUI.playerFactionOverviewUI.factionUI == null;
        return factionToResearch.discoveries >= discoveries;
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        if (!localPlayer.playerUI.playerFactionOverviewUI.gameObject.activeSelf ||
            localPlayer.playerUI.playerFactionOverviewUI.factionUI != localPlayer.GetFactionUI())
            buttonsToVisualize.Add(localPlayer.playerUI.factionOverviewButton);
        else buttonsToVisualize.AddRange(localPlayer.playerUI.playerFactionOverviewUI.discoveryButtons);
    }
}
