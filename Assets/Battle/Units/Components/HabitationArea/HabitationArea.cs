using UnityEngine;

public class HabitationArea : ModuleComponent {
    HabitationAreaScriptableObject habitationAreaScriptableObject;
    [field: SerializeField] public long population { get; private set; }


    public HabitationArea(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        habitationAreaScriptableObject = (HabitationAreaScriptableObject)componentScriptableObject;

        population = habitationAreaScriptableObject.populationSpace;
    }

    public void ColonizePlanet(Planet planet) {
        if (planet.planetFactions.ContainsKey(faction)) {
            planet.planetFactions[faction].AddPopulation(population);
            population = 0;
        } else {
            planet.AddColony(faction, population, "Colony");
        }
    }
}
