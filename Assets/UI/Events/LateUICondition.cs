using System.Collections.Generic;

public class LateUICondition : UIWrapperEventCondition<LateCondition> {
    public LateUICondition(LateCondition conditionLogic, LocalPlayer localPlayer,
        UnitSpriteManager unitSpriteManager) : base(conditionLogic, localPlayer, unitSpriteManager, true) { }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualise) {
        if (conditionLogic.eventCondition == null || conditionLogic.eventCondition is not UIEventCondition) return;
        ((UIEventCondition)conditionLogic.eventCondition).GetVisualizedObjects(objectsToVisualise);
    }
}
