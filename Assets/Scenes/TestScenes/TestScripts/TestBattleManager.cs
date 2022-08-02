using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBattleManager : BattleManager {
    public Unit target;
    protected override void Start() {
        Instance = this;
        transform.parent.Find("Player").GetComponent<LocalPlayer>().SetUpPlayer();
    }

    public override void FixedUpdate() {
        if (LocalPlayer.Instance.GetLocalPlayerInput().GetPlayerInput().Player.PrimaryMouseButton.IsPressed()) {
            target.transform.position = LocalPlayer.Instance.GetLocalPlayerInput().GetMouseWorldPosition();
        }
        for (int i = 0; i < usedProjectiles.Count; i++) {
            projectiles[usedProjectiles[i]].UpdateProjectile();
        }
    }
}
