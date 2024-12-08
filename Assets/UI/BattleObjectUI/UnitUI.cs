
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitUI : BattleObjectUI {
    public Unit unit { get; private set; }
    public UnitSelection unitSelection { get; private set; }
    public PrefabModuleSystem prefabModuleSystem { get; private set; }
    public List<BattleObjectUI> components { get; private set; }

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        this.unit = (Unit)battleObject;
        spriteRenderer.sprite = unit.unitScriptableObject.sprite;
        unitSelection = transform.GetChild(0).GetComponent<UnitSelection>();
        unitSelection.SetupSelection(this, uIManager);
        components = new List<BattleObjectUI>();
        prefabModuleSystem = GetComponent<PrefabModuleSystem>();
        for (var i = 0; i < prefabModuleSystem.modules.Count; i++) {
            if (unit.moduleSystem.moduleToSystem[unit.moduleSystem.modules[i]].type == PrefabModuleSystem.SystemType.Turret) {
                TurretUI turretUI = prefabModuleSystem.modules[i].gameObject.AddComponent<TurretUI>();
                components.Add(turretUI);
                turretUI.Setup(unit.moduleSystem.modules[i], uIManager, this);
            }
        }
    }

    public override float GetRotation() {
        return battleObject.rotation;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (components != null) components.ForEach(c => c.UpdateObject());

        if (uIManager.GetFactionColoringShown()) spriteRenderer.color = unit.faction.GetColorTint();
        else spriteRenderer.color = Color.white;

        unitSelection.UpdateUnitSelection();
    }

    public override bool IsVisible() {
        return battleObject.visible;
    }
}
