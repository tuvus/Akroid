using System;
using Castle.Components.DictionaryAdapter.Xml;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class Population {
    public long civilians;
    public long pilots;
    public long engineers;
    public long marines;

    public Population(long civilians = 0, long pilots = 0, long engineers = 0, long marines = 0) {
        this.civilians = civilians;
        this.pilots = pilots;
        this.engineers = engineers;
        this.marines = marines;
    }

    public Population SetBasicPopulation(long amount) {
        pilots = (long)(amount * PopulationCenter.pilotRatio);
        engineers = (long)(amount * PopulationCenter.engineerRatio);
        marines = (long)(amount * PopulationCenter.marineRatio);
        civilians = amount - pilots - engineers - marines;
        return this;
    }

    public Population(Population population) {
        civilians = population.civilians;
        pilots = population.pilots;
        engineers = population.engineers;
        marines = population.marines;
    }

    public void AddPopulation(Population pop) {
        civilians += pop.civilians;
        pilots += pop.pilots;
        engineers += pop.engineers;
        marines += pop.marines;
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
    public long MovePopulationTo(Population pop, long otherPopFreeSpace = long.MaxValue) {
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

[Serializable]
public class PopulationFloat {
    public float civilians;
    public float pilots;
    public float engineers;
    public float marines;

    public PopulationFloat(float civilians = 0, float pilots = 0, float engineers = 0, float marines = 0) {
        this.civilians = civilians;
        this.pilots = pilots;
        this.engineers = engineers;
        this.marines = marines;
    }
}

public class HabitationArea : ModuleComponent {
    private HabitationAreaScriptableObject habitationAreaScriptableObject;
    public Population population { get; private set; }

    public HabitationArea(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        habitationAreaScriptableObject = (HabitationAreaScriptableObject)componentScriptableObject;
        population = new Population();
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        habitationAreaScriptableObject = (HabitationAreaScriptableObject)componentScriptableObject;
    }

    public void ColonizePlanet(Planet planet) {
        if (planet.planetFactions.ContainsKey(faction)) {
            population.MovePopulationTo(planet.planetFactions[faction].population);
        } else {
            planet.AddColony(faction, population, "Colony");
        }
    }

    public long GetFreeSpace() {
        return habitationAreaScriptableObject.populationSpace - population.civilians - population.pilots -
            population.engineers - population.marines;
    }

    public long GetCapacity() {
        return habitationAreaScriptableObject.populationSpace;
    }
}
