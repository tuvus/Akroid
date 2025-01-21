using System.Collections.Generic;
using UnityEngine.UI;

public class FollowUnitCondition : UIEventCondition {
    private Unit unitToFollow;

    public FollowUnitCondition(LocalPlayer localPlayer, UIBattleManager uiBattleManager, Unit unitToFollow, bool visualize = false) :
        base(localPlayer, uiBattleManager, ConditionType.FollowUnit, visualize) {
        this.unitToFollow = unitToFollow;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        if (unitToFollow == null) return localPlayer.GetLocalPlayerInput().followUnit == null;
        return localPlayer.GetLocalPlayerInput().followUnit == uiBattleManager.units[unitToFollow];
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) {
        objectsToVisualize.Add(uiBattleManager.units[unitToFollow]);
    }
}
