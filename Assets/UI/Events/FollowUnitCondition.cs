using System.Collections.Generic;

public class FollowUnitCondition : UIEventCondition {
    private Unit unitToFollow;

    public FollowUnitCondition(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, Unit unitToFollow, bool visualize = false) :
        base(localPlayer, unitSpriteManager, ConditionType.FollowUnit, visualize) {
        this.unitToFollow = unitToFollow;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        if (unitToFollow == null) return localPlayer.GetLocalPlayerInput().followUnit == null;
        return localPlayer.GetLocalPlayerInput().followUnit == unitSpriteManager.units[unitToFollow];
    }

    public override List<ObjectUI> GetVisualizedObjects() {
        return new List<ObjectUI>() { unitSpriteManager.units[unitToFollow] };
    }
}
