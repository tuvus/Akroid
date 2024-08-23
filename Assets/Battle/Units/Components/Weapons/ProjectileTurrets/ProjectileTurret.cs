using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileTurret : Turret {
    ProjectileTurretScriptableObject projectileTurretScriptableObject;
    private SpriteRenderer flash;
    static float flashSpeed = 0.5f;
    private float flashTime;

    public override void SetupComponent(Module module, Faction faction, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, faction, componentScriptableObject);
        projectileTurretScriptableObject = (ProjectileTurretScriptableObject)componentScriptableObject;
    }

    public override void SetupTurret(Unit unit) {
        base.SetupTurret(unit);
        flash = Instantiate(Resources.Load<GameObject>("Prefabs/Highlight"), transform).GetComponent<SpriteRenderer>();
        flash.transform.localScale = new Vector2(.2f,.2f);
        flash.transform.localPosition = new Vector2(0, projectileTurretScriptableObject.turretOffset);
        flash.enabled = false;
    }

    public override bool Fire() {
        base.Fire();
        if (!BattleManager.Instance.instantHit) {
            Projectile projectile = BattleManager.Instance.GetNewProjectile();
            projectile.SetProjectile(unit.faction, transform.position, transform.eulerAngles.z + Random.Range(-projectileTurretScriptableObject.fireAccuracy, projectileTurretScriptableObject.fireAccuracy), unit.GetVelocity(), projectileTurretScriptableObject.fireVelocity, Mathf.RoundToInt(Random.Range(projectileTurretScriptableObject.minDamage, projectileTurretScriptableObject.maxDamage) * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileDamage)), projectileTurretScriptableObject.projectileRange * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileRange), GetTurretOffSet() * transform.localScale.y, transform.localScale.y * GetUnitScale());
            flashTime = flashSpeed;
            flash.enabled = BattleManager.Instance.GetEffectsShown();
            flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, 1);
        } else {
            targetUnit.TakeDamage(Mathf.RoundToInt(Random.Range(projectileTurretScriptableObject.minDamage, projectileTurretScriptableObject.maxDamage) * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileDamage)));
        }
        return reloadController.Empty();
    }

    public override void UpdateTurret(float deltaTime) {
        base.UpdateTurret(deltaTime);
        if (flash.enabled) {
            flashTime -= deltaTime;
            if (flashTime <= 0) {
                flashTime = 0;
                flash.enabled = false;
            } else {
                flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, flashTime / flashSpeed);
            }
        }
    }
    protected override bool TurretHibernationStatus() {
        return base.TurretHibernationStatus() && !flash.enabled;
    }

    public override Vector2 GetTargetPosition(Unit target) {
        return Calculator.GetTargetPositionAfterTimeAndVelocity(unit.GetPosition(), target.GetPosition(), unit.GetVelocity(), target.GetVelocity(), projectileTurretScriptableObject.fireVelocity, GetTurretOffSet());
    }

    public override float GetRange() {
        return base.GetRange() * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileRange);
    }

    public override float GetReloadTimeModifier() {
        return unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileReload);
    }

    public override float GetDamagePerSecond() {
        reloadController = GetComponent<ReloadController>();
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }
        float damage = (projectileTurretScriptableObject.minDamage + projectileTurretScriptableObject.maxDamage) / 2f * reloadController.maxAmmo;
        return damage / time;
    }

    public override void StopFiring() {
        base.StopFiring();
        flash.enabled = false;
    }

    public override void ShowEffects(bool shown) {
        base.ShowEffects(shown);
        if (flash.enabled) {
            flash.enabled = shown;
        }
    }

    [ContextMenu("GetDamagePerSecond")]
    public void PrintDamagePerSecond() {
        print(GetDamagePerSecond());
    }
}