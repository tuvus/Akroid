using System;
using System.Collections.Generic;
using UnityEngine;

public class UIEventManager : EventManager {
    private LocalPlayer localPlayer;
    private PlayerUI playerUI;
    private LocalPlayerGameInput playerGameInput;
    private UnitSpriteManager unitSpriteManager;
    private float panDelta;
    private List<Action> uIEventList;

    public UIEventManager(LocalPlayer localPlayer, LocalPlayerGameInput playerGameInput, UnitSpriteManager unitSpriteManager) {
        this.localPlayer = localPlayer;
        playerUI = localPlayer.GetPlayerUI();
        this.playerGameInput = playerGameInput;
        this.unitSpriteManager = unitSpriteManager;
        uIEventList = new List<Action>();
        playerUI.playerEventUI.SetEventManager(this);
        localPlayer.GetLocalPlayerInput().OnPanEvent += (oldPos, newPos) => panDelta = Vector2.Distance(oldPos, newPos);
    }

    public override void UpdateEvents(float deltaTime) {
        base.UpdateEvents(deltaTime);
        panDelta = 0;
    }


    /// <summary>
    /// Applies UI actions during the UI update.
    /// </summary>
    public void UpdateUIEvents() {
        uIEventList.ForEach(a => a.Invoke());
        uIEventList.Clear();
    }

    public override void SetPlayerZoom(float zoom) {
        uIEventList.Add(() => playerGameInput.SetZoom(zoom));
    }

    public override void CenterPlayerCamera() {
        uIEventList.Add(() => playerGameInput.CenterCamera());
    }

    public override void StartFollowingUnit(Unit unit) {
        uIEventList.Add(() => playerGameInput.StartFollowingUnit(unitSpriteManager.units[unit]));
    }
}
