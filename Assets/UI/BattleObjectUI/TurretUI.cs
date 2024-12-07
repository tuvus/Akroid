using UnityEngine;

public class TurretUI : BattleObjectUI {
    private Turret turret;
    public float rotation;
    public float worldRotation;

    public override void Setup(BattleObject battleObject) {
        base.Setup(battleObject);
        turret = (Turret)battleObject;
        spriteRenderer.sprite = turret.turretScriptableObject.turretSprite;
        spriteRenderer.enabled = true;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        rotation = turret.rotation;
        worldRotation = turret.GetWorldRotation();
    }

    public override Vector2 GetPosition() {
        return turret.GetWorldPosition();
    }

    public override float GetRotation() {
        return turret.rotation;
    }
}
