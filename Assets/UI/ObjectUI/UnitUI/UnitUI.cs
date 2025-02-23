using System.Collections.Generic;
using Castle.Components.DictionaryAdapter.Xml;
using UnityEngine;

public abstract class UnitUI : BattleObjectUI {
    public Unit unit { get; private set; }
    public UnitIconUI unitIconUI { get; private set; }
    public PrefabModuleSystem prefabModuleSystem { get; private set; }
    public List<ComponentUI> components { get; private set; }
    private DestroyEffectUI destroyEffectUI;
    private bool destroyed;

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        this.unit = (Unit)battleObject;
        spriteRenderer.sprite = unit.unitScriptableObject.sprite;
        spriteRenderer.enabled = false;
        unitIconUI = transform.GetChild(0).GetComponent<UnitIconUI>();
        unitIconUI.SetupIconUI(this, uIManager);
        components = new List<ComponentUI>();
        prefabModuleSystem = GetComponent<PrefabModuleSystem>();
        destroyEffectUI = transform.GetChild(1).GetComponent<DestroyEffectUI>();
        destroyEffectUI.SetupDestroyEffect(this, unit.unitScriptableObject.destroyEffect, uIManager, spriteRenderer);
        destroyed = false;
        for (var i = 0; i < prefabModuleSystem.modules.Count; i++) {
            ModuleComponent moduleComponent = unit.moduleSystem.modules[i];
            if (moduleComponent.componentScriptableObject is EmptyScriptableObject) continue;
            Module module = prefabModuleSystem.modules[i];
            var system = unit.moduleSystem.moduleToSystem[moduleComponent];

            if (system.type == PrefabModuleSystem.SystemType.Turret) {
                TurretUI turretUI;
                if (unit.moduleSystem.moduleToSystem[moduleComponent].component is LaserTurretScriptableObject) {
                    turretUI = module.gameObject.AddComponent<LaserTurretUI>();
                } else if (unit.moduleSystem.moduleToSystem[moduleComponent].component is ProjectileTurretScriptableObject) {
                    turretUI = module.gameObject.AddComponent<ProjectileTurretUI>();
                } else {
                    turretUI = module.gameObject.AddComponent<TurretUI>();
                }

                components.Add(turretUI);
                turretUI.Setup(moduleComponent, uIManager, this);
            } else if (system.type == PrefabModuleSystem.SystemType.Thruster) {
                ThrusterUI thrusterUI = module.gameObject.AddComponent<ThrusterUI>();
                components.Add(thrusterUI);
                thrusterUI.Setup(moduleComponent, uIManager, this);
            } else if (system.type == PrefabModuleSystem.SystemType.Utility && system.component is ShieldGeneratorScriptableObject) {
                ShieldGenderatorUI shieldGeneratorUI = module.gameObject.AddComponent<ShieldGenderatorUI>();
                components.Add(shieldGeneratorUI);
                shieldGeneratorUI.Setup(moduleComponent, uIManager, this);
            }
        }
        uIManager.uiBattleManager.objectsToUpdate.Add(this);
    }

    public override float GetRotation() {
        return battleObject.rotation;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (!unit.Destroyed()) {
            if (components != null) components.ForEach(c => c.UpdateObject());

            if (uIManager.GetFactionColoringShown()) spriteRenderer.color = unit.faction.GetColorTint();
            else spriteRenderer.color = Color.white;

            unitIconUI.UpdateUnitIconUI();
        } else if (!destroyed) {
            if (unit.GetDestroyEffect() != null) {
                components.ForEach(c => c.OnUnitDestroyed());
                destroyed = true;
                destroyEffectUI.Explode(unit.GetDestroyEffect());
                unitIconUI.ShowUnitIconUI(false);
                UnselectObject();
            }
        } else {
            destroyEffectUI.UpdateExplosion();
        }
    }

    public override void SelectObject(UnitIconUI.SelectionStrength selectionStrength = UnitIconUI.SelectionStrength.Unselected) {
        if (!destroyed) unitIconUI.SetSelected(selectionStrength);
    }

    public override void UnselectObject() {
        unitIconUI.SetSelected();
    }

    public override void OnBattleObjectRemoved() {
        // The unit might have been removed before we registered that it was destroyed in the UI update loop
        // So make sure we do any extra cleanup before removing
        if (!destroyed) {
            components.ForEach(c => c.OnUnitDestroyed());
            destroyed = true;
            UnselectObject();
        }
        base.OnBattleObjectRemoved();
        destroyEffectUI.OnBattleObjectRemoved();
        components.ForEach(c => c.OnUnitRemoved());
    }
}
