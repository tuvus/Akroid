using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class Population {
    public long civilians;
    public long pilots;
    public long engineers;
    public long marines;

    public Population() {
        civilians = 0;
        pilots = 0;
        engineers = 0;
        marines = 0;
    }

    public Population(Population population) {
        civilians = population.civilians;
        pilots = population.pilots;
        engineers = population.engineers;
        marines = population.marines;
    }

    public void SubtractPopulation(Population pop) {
        civilians = math.max(0, civilians - pop.civilians);
        pilots = math.max(0, pilots - pop.pilots);
        engineers = math.max(0, engineers - pop.engineers);
        marines = math.max(0, marines - pop.marines);
    }

    /// <summary>
    /// Move some of this population to the other population until the other population is full.
    /// Returns the leftover population
    /// </summary>
    public long MovePopulationTo(Population pop, long otherPopFreeSpace) {
        long popToMove = math.min(civilians, otherPopFreeSpace);
        pop.civilians += popToMove;
        civilians -= popToMove;
        otherPopFreeSpace -= popToMove;
        popToMove = math.min(pilots, otherPopFreeSpace);
        pop.pilots += popToMove;
        pilots -= popToMove;
        otherPopFreeSpace -= popToMove;
        popToMove = math.min(engineers, otherPopFreeSpace);
        pop.engineers += popToMove;
        engineers -= popToMove;
        otherPopFreeSpace -= popToMove;
        popToMove = math.min(marines, otherPopFreeSpace);
        pop.marines += popToMove;
        marines -= popToMove;
        otherPopFreeSpace -= popToMove;
        return otherPopFreeSpace;
    }

    public long TotalPopulation() {
        return civilians + pilots + engineers + marines;
    }
}

public class HabitationArea : ModuleComponent {
    private HabitationAreaScriptableObject habitationAreaScriptableObject;
    [field: SerializeField] public long population { get; private set; }


    public HabitationArea(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        habitationAreaScriptableObject = (HabitationAreaScriptableObject)componentScriptableObject;

        population = habitationAreaScriptableObject.populationSpace;
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        habitationAreaScriptableObject = (HabitationAreaScriptableObject)componentScriptableObject;
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
