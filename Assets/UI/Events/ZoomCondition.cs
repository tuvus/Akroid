using System.Collections.Generic;

public class ZoomCondtion : UIEventCondition {
    private float zoomTo;
    private float startingZoom;

    public ZoomCondtion(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, float zoomTo) : base(localPlayer, unitSpriteManager,
        ConditionType.Zoom) {
        this.zoomTo = zoomTo;
        startingZoom = localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        return startingZoom < zoomTo
            ? localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize >= zoomTo
            : localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize <= zoomTo;
    }

    public override List<ObjectUI> GetVisualizedObjects() {
        return new List<ObjectUI>();
    }
}
