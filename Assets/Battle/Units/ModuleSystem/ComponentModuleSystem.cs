using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///     Manages runtime queries to the unit's components.
///     Caches the components for efficientcy.
/// </summary>
public class ComponentModuleSystem : ModuleSystem {
    private static readonly HashSet<Type> ComponentTypes = new HashSet<Type> {
        typeof(Turret), typeof(LaserTurret), typeof(ProjectileTurret), typeof(MissileLauncher),
        typeof(ShieldGenerator), typeof(Thruster), typeof(Generator), typeof(CargoBay), typeof(Hangar),
        typeof(ConstructionBay), typeof(HabitationArea), typeof(ResearchEquipment), typeof(GasCollector),
        typeof(EmptyComponent)
    };
    private readonly Dictionary<Type, List<ModuleComponent>> components;

    public ComponentModuleSystem(BattleManager battleManager, Unit unit, UnitScriptableObject unitScriptableObject) :
        base(battleManager, unit, unitScriptableObject) {
        components = new Dictionary<Type, List<ModuleComponent>>();
        foreach (ModuleComponent moduleComponent in modules) {
            Type currentType = moduleComponent.GetType();
            while (currentType != null) {
                if (ComponentTypes.Contains(currentType)) {
                    if (components.ContainsKey(currentType)) {
                        components[currentType].Add(moduleComponent);
                    } else {
                        components.Add(currentType, new List<ModuleComponent> { moduleComponent });
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

        return new List<T>();
    }
}
