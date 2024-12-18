public class ZoomCondtion : UIEventCondition {
    private float zoomTo;
    private float startingZoom;

    public ZoomCondtion(LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager, ConditionType conditionType, float zoomTo) :
        base(localPlayer, unitSpriteManager, conditionType) {
        this.zoomTo = zoomTo;
        startingZoom = localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        return startingZoom < zoomTo
            ? localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize >= zoomTo
            : localPlayer.GetLocalPlayerInput().GetCamera().orthographicSize <= zoomTo;
    }
}
