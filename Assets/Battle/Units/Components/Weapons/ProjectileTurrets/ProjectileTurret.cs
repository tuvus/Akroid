using UnityEngine;

public class ProjectileTurret : Turret {
    ProjectileTurretScriptableObject projectileTurretScriptableObject;

    public ProjectileTurret(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        projectileTurretScriptableObject = (ProjectileTurretScriptableObject)componentScriptableObject;
    }

    public override bool Fire() {
        base.Fire();
        if (!battleManager.instantHit) {
            Projectile projectile = battleManager.GetNewProjectile();
            projectile.SetProjectile(unit.faction, GetWorldPosition(),
                GetWorldRotation() + Random.Range(-projectileTurretScriptableObject.fireAccuracy, projectileTurretScriptableObject.fireAccuracy),
                unit.GetVelocity(), projectileTurretScriptableObject.fireVelocity,
                Mathf.RoundToInt(Random.Range(projectileTurretScriptableObject.minDamage, projectileTurretScriptableObject.maxDamage) *
                                 unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileDamage)),
                projectileTurretScriptableObject.projectileRange *
                unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileRange), GetTurretOffSet() * rotation, scale.y);
        } else {
            targetUnit.TakeDamage(Mathf.RoundToInt(
                Random.Range(projectileTurretScriptableObject.minDamage, projectileTurretScriptableObject.maxDamage) *
                unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileDamage)));
        }

        return reloadController.Empty();
    }

    public override Vector2 GetTargetPosition(Unit target) {
        return Calculator.GetTargetPositionAfterTimeAndVelocity(unit.GetPosition(), target.GetPosition(), unit.GetVelocity(),
            target.GetVelocity(), projectileTurretScriptableObject.fireVelocity, GetTurretOffSet());
    }

    public override float GetRange() {
        return base.GetRange() * unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileRange);
    }

    public override float GetReloadTimeModifier() {
        return unit.faction.GetImprovementModifier(Faction.ImprovementAreas.ProjectileReload);
    }

    public override float GetDamagePerSecond() {
        reloadController = new ReloadController(projectileTurretScriptableObject.fireSpeed, projectileTurretScriptableObject.reloadSpeed,
            projectileTurretScriptableObject.maxAmmo);
        float time = reloadController.reloadSpeed;
        if (reloadController.maxAmmo > 1) {
            time += reloadController.maxAmmo * reloadController.fireSpeed;
        }

        float damage = (projectileTurretScriptableObject.minDamage + projectileTurretScriptableObject.maxDamage) / 2f *
                       reloadController.maxAmmo;
        return damage / time;
    }

    [ContextMenu("GetDamagePerSecond")]
    public void PrintDamagePerSecond() {
        Debug.Log(GetDamagePerSecond());
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/ProjectileTurret");
    }
}
