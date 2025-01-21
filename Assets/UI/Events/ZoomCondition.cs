using System.Collections.Generic;
using UnityEngine.UI;

public class ZoomCondtion : UIEventCondition {
    private float zoomTo;
    private float startingZoom;

    public ZoomCondtion(LocalPlayer localPlayer, UIBattleManager uiBattleManager, float zoomTo) : base(localPlayer, uiBattleManager,
        ConditionType.Zoom) {
        this.zoomTo = zoomTo;
        startingZoom = localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        return startingZoom < zoomTo
            ? localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize >= zoomTo
            : localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize <= zoomTo;
    }

    public override void GetVisualizedObjects(List<ObjectUI> objectsToVisualize, List<Button> buttonsToVisualize) { }
}
