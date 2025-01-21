using System.Collections.Generic;
using UnityEngine.UI;

public class OpenObjectPanelCondition : UIEventCondition {
    private BattleObject objectToSelect;

    public OpenObjectPanelCondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, BattleObject objectToSelect,
        bool visualize = false) : base(localPlayer, uiBattleManager, ConditionType.OpenObjectPanel, visualize) {
        this.objectToSelect = objectToSelect;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        if (objectToSelect == null) return localPlayer.GetLocalPlayerGameInput().rightClickedBattleObject == null;
        return localPlayer.GetLocalPlayerGameInput().rightClickedBattleObject == uiBattleManager.battleObjects[objectToSelect];
    }


    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        if (objectToSelect == null) return;
        if (objectToSelect.IsShip())
            AddShipsToSelect(new List<ShipUI>() { (ShipUI)uiBattleManager.battleObjects[objectToSelect] }, objectsToVisualize,
                buttonsToVisualize, false);
        else objectsToVisualize.Add(uiBattleManager.battleObjects[objectToSelect]);
    }
}
