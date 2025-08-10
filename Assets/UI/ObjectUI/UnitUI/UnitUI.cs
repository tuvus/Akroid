using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public abstract class UnitUI : BattleObjectUI {
    private bool destroyed;
    private DestroyEffectUI destroyEffectUI;
    public Unit unit { get; private set; }
    public UnitIconUI unitIconUI { get; private set; }
    public PrefabModuleSystem prefabModuleSystem { get; private set; }
    public List<ComponentUI> componentUIs { get; private set; }
    private bool componentsChanged;

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        unit = (Unit)battleObject;
        spriteRenderer.sprite = unit.unitScriptableObject.sprite;
        spriteRenderer.enabled = false;
        unitIconUI = transform.GetChild(0).GetComponent<UnitIconUI>();
        unitIconUI.SetupIconUI(this, uIManager);
        componentUIs = new List<ComponentUI>();
        prefabModuleSystem = GetComponent<PrefabModuleSystem>();
        destroyEffectUI = transform.GetChild(1).GetComponent<DestroyEffectUI>();
        destroyEffectUI.SetupDestroyEffect(this, unit.unitScriptableObject.destroyEffect, uIManager, spriteRenderer,
            unit.unitScriptableObject.explosionSound, 1, 2, 1);
        destroyed = false;
        for (int i = 0; i < prefabModuleSystem.modules.Count; i++) {
            AddComponentUIToModule(prefabModuleSystem.modules[i], unit.moduleSystem.modules[i], componentUIs.Count);
        }
        uIManager.uiBattleManager.objectsToUpdate.Add(this);
        unit.moduleSystem.OnSystemReplaced += () => componentsChanged = true;
        componentsChanged = false;
    }

    private bool AddComponentUIToModule(Module module, ModuleComponent moduleComponent, int index) {
        if (moduleComponent.componentScriptableObject is EmptyScriptableObject) return false;
        ModuleSystem.System system = unit.moduleSystem.moduleToSystem[moduleComponent];

        if (system.type == ModuleSystem.SystemType.Turret) {
            TurretUI turretUI;
            if (unit.moduleSystem.moduleToSystem[moduleComponent].component is LaserTurretScriptableObject) {
                turretUI = module.gameObject.AddComponent<LaserTurretUI>();
            } else if (unit.moduleSystem.moduleToSystem[moduleComponent].component is
                ProjectileTurretScriptableObject) {
                turretUI = module.gameObject.AddComponent<ProjectileTurretUI>();
            } else {
                turretUI = module.gameObject.AddComponent<TurretUI>();
            }

            componentUIs.Insert(index, turretUI);
            turretUI.Setup(moduleComponent, uIManager, this, index);
            return true;
        } else if (system.type == ModuleSystem.SystemType.Thruster) {
            ThrusterUI thrusterUI = module.gameObject.AddComponent<ThrusterUI>();
            componentUIs.Insert(index, thrusterUI);
            thrusterUI.Setup(moduleComponent, uIManager, this, index);
            return true;
        } else if (system.type == ModuleSystem.SystemType.Utility &&
            system.component is ShieldGeneratorScriptableObject) {
            ShieldGenderatorUI shieldGeneratorUI = module.gameObject.AddComponent<ShieldGenderatorUI>();
            componentUIs.Insert(index, shieldGeneratorUI);
            shieldGeneratorUI.Setup(moduleComponent, uIManager, this, index);
            return true;
        }
        return false;
    }

    public override float GetRotation() {
        return battleObject.rotation;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (!unit.Destroyed()) {
            if (componentUIs != null) {
                if (componentsChanged) ComponentsChanged();
                componentUIs.ForEach(c => c.UpdateObject());
            }

            if (uIManager.GetFactionColoringShown()) spriteRenderer.color = unit.faction.GetColorTint();
            else spriteRenderer.color = Color.white;

            unitIconUI.UpdateUnitIconUI();
        } else if (!destroyed) {
            if (unit.GetDestroyEffect() != null) {
                componentUIs.ForEach(c => c.OnUnitDestroyed());
                destroyed = true;
                destroyEffectUI.Explode(unit.GetDestroyEffect());
                unitIconUI.ShowUnitIconUI(false);
                UnselectObject();
            }
        }

        if (destroyed) {
            destroyEffectUI.UpdateExplosion();
        }
    }

    public void ComponentsChanged() {
        int componentUIIndex = 0;
        for (var i = 0; i < unit.moduleSystem.modules.Count; i++) {
            if (componentUIIndex == componentUIs.Count || componentUIs[componentUIIndex].componentIndex != i) {
                // Try adding the new component (it might not have any UI to it)
                if (AddComponentUIToModule(prefabModuleSystem.modules[i], unit.moduleSystem.modules[i],
                    componentUIIndex))
                    componentUIIndex++;
                continue;
            }

            if (componentUIs[componentUIIndex].moduleComponent.componentScriptableObject ==
                unit.moduleSystem.modules[i].componentScriptableObject)
                continue;

            // Replace the component with the new one
            componentUIs[componentUIIndex].RemoveComponent();
            DestroyImmediate(componentUIs[componentUIIndex]);
            componentUIs.RemoveAt(componentUIIndex);
            if (AddComponentUIToModule(prefabModuleSystem.modules[i], unit.moduleSystem.modules[i], i))
                componentUIIndex++;
        }
        componentsChanged = false;
    }

    public override void SelectObject(
        UnitIconUI.SelectionStrength selectionStrength = UnitIconUI.SelectionStrength.Unselected) {
        if (!destroyed) unitIconUI.SetSelected(selectionStrength);
    }

    public override void UnselectObject() {
        unitIconUI.SetSelected();
    }

    public override void OnBattleObjectRemoved() {
        // The unit might have been removed before we registered that it was destroyed in the UI update loop
        // So make sure we do any extra cleanup before removing
        if (!destroyed) {
            componentUIs.ForEach(c => c.OnUnitDestroyed());
            destroyed = true;
            UnselectObject();
        }
        base.OnBattleObjectRemoved();
        destroyEffectUI.OnBattleObjectRemoved();
        componentUIs.ForEach(c => c.OnUnitRemoved());
    }
}
