using UnityEngine;

public class MoveCameraToIObject : MoveCamera {
    private IObject iObject;
    public MoveCameraToIObject(LocalPlayer localPlayer, UIBattleManager uiBattleManager, Vector2 startPos,
        IObject iObject, float startZoom, float endZoom, float duration) : base(localPlayer, uiBattleManager, startPos,
        iObject.GetPosition(), startZoom, endZoom, duration) {
        this.iObject = iObject;
    }

    public override void UpdateUI(EventManager eventManager) {
        endPos = iObject.GetPosition();
        base.UpdateUI(eventManager);
    }

    public override bool CheckUICondition(EventManager eventManager) {
        if (iObject == null) return false;
        return base.CheckUICondition(eventManager);
    }
}
