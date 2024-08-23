using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HabitationArea : ModuleComponent {
    HabitationAreaScriptableObject habitationAreaScriptableObject;
    [field: SerializeField] public long population { get; private set; }


    public override void SetupComponent(Module module, Faction faction, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, faction, componentScriptableObject);
        habitationAreaScriptableObject = (HabitationAreaScriptableObject)componentScriptableObject;
        population = habitationAreaScriptableObject.populationSpace;
    }

    public void ColonizePlanet(Planet planet) {
        if (planet.planetFactions.ContainsKey(faction)) {
            planet.planetFactions[faction].AddPopulation(population);
            population = 0;
        } else {
            planet.AddFaction(faction, new Planet.PlanetTerritory(), population, "Colony");
        }
    }
}
