using UnityEngine;

public class ProjectileTurretUI : TurretUI {
    private ProjectileTurret projectileTurret;
    private SpriteRenderer flash;

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager, unitUI);
        projectileTurret = (ProjectileTurret)battleObject;
        spriteRenderer.sprite = projectileTurret.turretScriptableObject.turretSprite;
        spriteRenderer.enabled = true;

        flash = Instantiate(Resources.Load<GameObject>("Prefabs/Highlight"), transform).GetComponent<SpriteRenderer>();
        flash.transform.localScale = new Vector2(.2f,.2f);
        flash.transform.localPosition = new Vector2(0, projectileTurret.projectileTurretScriptableObject.turretOffset);
        flash.enabled = false;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (uIManager.GetEffectsShown() && uIManager.battleManager.GetSimulationTime() - projectileTurret.lastFlashTime <
            projectileTurret.projectileTurretScriptableObject.flashSpeed) {
            flash.enabled = true;
            flash.color = new Color(1, 1, 1,
                (float)(1 - (uIManager.battleManager.GetSimulationTime() -
                    projectileTurret.lastFlashTime) / projectileTurret.projectileTurretScriptableObject.flashSpeed));
        } else {
            flash.enabled = false;
        }
    }
}
