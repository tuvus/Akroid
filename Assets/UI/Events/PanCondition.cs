using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanCondtion : UIEventCondition {
    private float distanceToPan;

    public PanCondtion(LocalPlayer localPlayer, UIBattleManager uiBattleManager, float distanceToPan) : base(localPlayer,
        uiBattleManager, ConditionType.Pan) {
        this.distanceToPan = distanceToPan;
        localPlayer.GetInputManager().OnPanEvent += OnPan;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        return distanceToPan <= 0;
    }

    private void OnPan(Vector2 oldPos, Vector2 newPos) {
        distanceToPan -= Vector2.Distance(oldPos, newPos);
        if (distanceToPan <= 0) localPlayer.GetInputManager().OnPanEvent -= OnPan;
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) { }
}
