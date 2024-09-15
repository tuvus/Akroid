using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBattleManager : BattleManager {
    public Unit target;
    protected override void Start() {
        Instance = this;
        // transform.parent.Find("Player").GetComponent<LocalPlayer>().SetUpPlayer();
        // foreach (var faction in GetComponentsInChildren<Faction>()) {
            // faction.SetUpFaction(this, new Faction.FactionData(), new PositionGiver(Vector2.zero), 0);
        // }
    }

    public override void FixedUpdate() {
        if (LocalPlayer.Instance.GetLocalPlayerInput().GetPlayerInput().Player.PrimaryMouseButton.IsPressed()) {
            // target.position = LocalPlayer.Instance.GetLocalPlayerInput().GetMouseWorldPosition();
        }
        foreach (var usedProjectile in usedProjectiles) {
            usedProjectile.UpdateProjectile(Time.fixedDeltaTime);
        }
        foreach (var usedMissile in usedMissiles) {
            usedMissile.UpdateMissile(Time.fixedDeltaTime);
        }
    }
}
