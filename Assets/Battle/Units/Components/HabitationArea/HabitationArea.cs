using UnityEngine;

public class HabitationArea : ModuleComponent {
    private readonly HabitationAreaScriptableObject habitationAreaScriptableObject;


    public HabitationArea(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        habitationAreaScriptableObject = (HabitationAreaScriptableObject)componentScriptableObject;

        population = habitationAreaScriptableObject.populationSpace;
    }
    [field: SerializeField] public long population { get; private set; }

    public void ColonizePlanet(Planet planet) {
        if (planet.planetFactions.ContainsKey(faction)) {
            planet.planetFactions[faction].AddPopulation(population);
            population = 0;
        } else {
            planet.AddColony(faction, population, "Colony");
        }
    }
}
