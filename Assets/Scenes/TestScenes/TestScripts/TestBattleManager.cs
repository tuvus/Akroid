using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBattleManager : BattleManager {
    public Unit target;
    protected override void Start() {
        Instance = this;
        transform.parent.Find("Player").GetComponent<LocalPlayer>().SetUpPlayer();
        foreach (var faction in GetComponentsInChildren<Faction>()) {
            faction.SetUpFaction(0, new Faction.FactionData(), new PositionGiver(Vector2.zero), 0);
        }
    }

    public override void FixedUpdate() {
        if (LocalPlayer.Instance.GetLocalPlayerInput().GetPlayerInput().Player.PrimaryMouseButton.IsPressed()) {
            target.transform.position = LocalPlayer.Instance.GetLocalPlayerInput().GetMouseWorldPosition();
        }
        for (int i = 0; i < usedProjectiles.Count; i++) {
            projectiles[usedProjectiles[i]].UpdateProjectile(Time.fixedDeltaTime);
        }

        for (int i = 0; i < usedMissiles.Count; i++) {
            missiles[usedMissiles[i]].UpdateMissile(Time.fixedDeltaTime);
        }
    }
}
