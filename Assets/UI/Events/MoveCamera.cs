using Unity.Mathematics;
using UnityEngine;

public class MoveCamera : UIEvent {
    private Vector2 startPos;
    protected Vector2 endPos;
    private float startZoom;
    private float endZoom;
    private float duration;
    private float elapsed;

    public MoveCamera(LocalPlayer localPlayer, UIBattleManager uiBattleManager, Vector2 startPos, Vector2 endPos,
        float startZoom, float endZoom, float duration) : base(localPlayer, uiBattleManager, ConditionType.MoveCamera) {
        this.startPos = startPos;
        this.endPos = endPos;
        this.startZoom = startZoom;
        this.endZoom = endZoom;
        this.duration = duration;
        elapsed = 0;
    }

    public override void UpdateUI(EventManager eventManager) {
        LocalPlayerInput playerInput = uiBattleManager.uIManager.playerUI.GetLocalPlayerInput();
        playerInput.SetCameraPosition(math.lerp(startPos, endPos, elapsed / duration));
        playerInput.SetZoom(math.lerp(startZoom, endZoom, elapsed / duration));
        elapsed += Time.deltaTime;
    }

    public override bool CheckUICondition(EventManager eventManager) {
        return elapsed >= duration;
    }
}
