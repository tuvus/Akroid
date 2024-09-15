using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages runtime queries to the unit's components.
/// Caches the components for efficientcy.
/// </summary>
public class ComponentModuleSystem : ModuleSystem {
    private Dictionary<Type, List<ModuleComponent>> components;

    private static readonly HashSet<Type> ComponentTypes = new() {
        typeof(Turret), typeof(LaserTurret), typeof(ProjectileTurret), typeof(MissileLauncher),
        typeof(ShieldGenerator), typeof(Thruster), typeof(Generator), typeof(CargoBay), typeof(Hangar),
        typeof(ConstructionBay), typeof(HabitationArea), typeof(ResearchEquipment), typeof(GasCollector),
        typeof(EmptyComponent)
    };

    public ComponentModuleSystem(BattleManager battleManager, Unit unit, UnitScriptableObject unitScriptableObject):
        base(battleManager, unit, unitScriptableObject){

        components = new Dictionary<Type, List<ModuleComponent>>();
        foreach (var moduleComponent in modules) {
            Type currentType = moduleComponent.GetType();
            while (currentType != null) {
                if (ComponentTypes.Contains(currentType)) {
                    if (components.ContainsKey(currentType)) {
                        components[currentType].Add(moduleComponent);
                    } else {
                        components.Add(currentType, new() { moduleComponent });
                    }
                }

                currentType = currentType.BaseType;
            }
        }
    }

    public List<T> Get<T>() where T : ModuleComponent {
        if (components.ContainsKey(typeof(T))) {
            return components[typeof(T)].Cast<T>().ToList();
        }
        return new();
    }
}
