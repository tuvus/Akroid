using System.Collections.Generic;

public class LateUICondition : UIWrapperEventCondition<LateCondition> {
    public LateUICondition(LateCondition conditionLogic, LocalPlayer localPlayer,
        UIBattleManager uiBattleManager) : base(conditionLogic, localPlayer, uiBattleManager, true) { }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        bool condition = base.CheckCondition(eventManager, deltaTime);
        visualize = conditionLogic.visualize;
        return condition;
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualise) {
        if (conditionLogic.eventCondition == null || conditionLogic.eventCondition is not UIEventCondition) return;
        ((UIEventCondition)conditionLogic.eventCondition).GetVisualizedObjects(objectsToVisualise);
    }
}
