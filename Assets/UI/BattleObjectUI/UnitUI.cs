using System.Collections.Generic;
using Castle.Components.DictionaryAdapter.Xml;
using UnityEngine;

public abstract class UnitUI : BattleObjectUI {
    public Unit unit { get; private set; }
    public UnitSelection unitSelection { get; private set; }
    public PrefabModuleSystem prefabModuleSystem { get; private set; }
    public List<ComponentUI> components { get; private set; }
    private DestroyEffectUI destroyEffectUI;
    private bool destroyed;

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        this.unit = (Unit)battleObject;
        spriteRenderer.sprite = unit.unitScriptableObject.sprite;
        spriteRenderer.enabled = false;
        unitSelection = transform.GetChild(0).GetComponent<UnitSelection>();
        unitSelection.SetupSelection(this, uIManager);
        components = new List<ComponentUI>();
        prefabModuleSystem = GetComponent<PrefabModuleSystem>();
        destroyEffectUI = transform.GetChild(1).GetComponent<DestroyEffectUI>();
        destroyEffectUI.SetupDestroyEffect(this, unit.unitScriptableObject.destroyEffect, uIManager, spriteRenderer);
        destroyed = false;
        for (var i = 0; i < prefabModuleSystem.modules.Count; i++) {
            if (unit.moduleSystem.modules[i].componentScriptableObject is EmptyScriptableObject) continue;
            var system = unit.moduleSystem.moduleToSystem[unit.moduleSystem.modules[i]];
            if (system.type == PrefabModuleSystem.SystemType.Turret) {
                TurretUI turretUI;
                if (unit.moduleSystem.moduleToSystem[unit.moduleSystem.modules[i]].component is LaserTurretScriptableObject) {
                    turretUI = prefabModuleSystem.modules[i].gameObject.AddComponent<LaserTurretUI>();
                } else {
                    turretUI = prefabModuleSystem.modules[i].gameObject.AddComponent<TurretUI>();
                }

                components.Add(turretUI);
                turretUI.Setup(unit.moduleSystem.modules[i], uIManager, this);
            } else if (system.type == PrefabModuleSystem.SystemType.Thruster) {
                ThrusterUI thrusterUI = prefabModuleSystem.modules[i].gameObject.AddComponent<ThrusterUI>();
                components.Add(thrusterUI);
                thrusterUI.Setup(unit.moduleSystem.modules[i], uIManager, this);
            } else if (system.type == PrefabModuleSystem.SystemType.Utility && system.component is ShieldGeneratorScriptableObject) {
                ShieldGenderatorUI shieldGeneratorUI = prefabModuleSystem.modules[i].gameObject.AddComponent<ShieldGenderatorUI>();
                components.Add(shieldGeneratorUI);
                shieldGeneratorUI.Setup(unit.moduleSystem.modules[i], uIManager, this);
            }
        }
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

            unitSelection.UpdateUnitSelection();
        } else if (!destroyed) {
            if (unit.GetDestroyEffect() != null) {
                if (components != null) components.ForEach(c => c.OnUnitDestroyed());
                destroyed = true;
                destroyEffectUI.Explode(unit.GetDestroyEffect());
                unitSelection.ShowUnitSelection(false);
            }
        } else {
            destroyEffectUI.UpdateExplosion();
        }
    }

    public override void SelectObject(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) {
        unitSelection.SetSelected(selectionStrength);
    }

    public override void UnselectObject() {
        unitSelection.SetSelected();
    }
}
