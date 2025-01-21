using System.Collections.Generic;
using UnityEngine.UI;

public class LateUICondition : UIWrapperEventCondition<LateCondition> {
    public LateUICondition(LateCondition conditionLogic, LocalPlayer localPlayer,
        UIBattleManager uiBattleManager) : base(conditionLogic, localPlayer, uiBattleManager, true) { }

    public override bool CheckCondition(EventManager eventManager, float deltaTime) {
        bool condition = base.CheckCondition(eventManager, deltaTime);
        visualize = conditionLogic.visualize;
        return condition;
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualise, List<Button> buttonsToVisualize) {
        if (conditionLogic.eventCondition == null || conditionLogic.eventCondition is not UIEventCondition) return;
        ((UIEventCondition)conditionLogic.eventCondition).GetVisualizedObjects(objectsToVisualise, buttonsToVisualize);
    }
}
