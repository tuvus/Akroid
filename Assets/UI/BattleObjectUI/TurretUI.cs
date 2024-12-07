using UnityEngine;

public class TurretUI : BattleObjectUI {
    private Turret turret;

    public override void Setup(BattleObject battleObject) {
        base.Setup(battleObject);
        turret = (Turret)battleObject;
        spriteRenderer.sprite = turret.turretScriptableObject.turretSprite;
        spriteRenderer.enabled = true;
    }

    public override Vector2 GetPosition() {
        return turret.GetPosition();
    }

    public override float GetRotation() {
        return turret.rotation;
    }
}
