using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

public enum Occupation {
    Civilian,
    Pilot,
    Engineer,
    Marine
}

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

    public Population SetPlanetPopulation(long amount) {
        pilots = (long)(amount * PopulationCenter.pilotPlanetRatio);
        engineers = (long)(amount * PopulationCenter.engineerPlanetRatio);
        marines = (long)(amount * PopulationCenter.marinePlanetRatio);
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

    /// <summary>
    /// Moves amountToMove population from this population to toMoveTo
    /// taking the min of what this population has and amountToMove.
    /// Returns the population that wasn't moved.
    /// </summary>
    public Population MovePopulationTo(Population toMoveTo, Population amountToMove) {
        Population notMoved = new Population(amountToMove);
        notMoved.SubtractPopulation(this);
        toMoveTo.AddPopulation(amountToMove);
        SubtractPopulation(amountToMove);
        toMoveTo.SubtractPopulation(notMoved);
        return notMoved;
    }

    /// <summary>
    /// Sets each occupation to the minimum of this population's value and the other population's value
    /// </summary>
    /// <param name="other"></param>
    public void Min(Population other) {
        civilians = math.min(civilians, other.civilians);
        pilots = math.min(pilots, other.pilots);
        engineers = math.min(engineers, other.engineers);
        marines = math.min(marines, other.marines);
    }

    public long TotalPopulation() {
        return civilians + pilots + engineers + marines;
    }

    public void Add(Occupation occupation, long amount) {
        switch (occupation) {
            case Occupation.Civilian:
                civilians += amount;
                break;
            case Occupation.Pilot:
                pilots += amount;
                break;
            case Occupation.Engineer:
                engineers += amount;
                break;
            case Occupation.Marine:
                marines += amount;
                break;
        }
    }

    public long Get(Occupation occupation) {
        switch (occupation) {
            case Occupation.Civilian:
                return civilians;
            case Occupation.Pilot:
                return pilots;
            case Occupation.Engineer:
                return engineers;
            case Occupation.Marine:
                return marines;
        }
        return -1;
    }

    public override bool Equals(object obj) {
        if (obj is not Population pop) return false;
        return HabitationArea.allOccupations.All(o => Get(o) == pop.Get(o));
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

    public long GetTotalValue(Population population) {
        return (long)HabitationArea.allOccupations.Sum(o => population.Get(o) * Get(o));
    }

    public float Get(Occupation occupation) {
        switch (occupation) {
            case Occupation.Civilian:
                return civilians;
            case Occupation.Pilot:
                return pilots;
            case Occupation.Engineer:
                return engineers;
            case Occupation.Marine:
                return marines;
        }
        return -1;
    }
}

public class HabitationArea : ModuleComponent {
    private HabitationAreaScriptableObject habitationAreaScriptableObject;
    public Population population { get; private set; }
    public static readonly List<Occupation> allOccupations = new List<Occupation>
        { Occupation.Civilian, Occupation.Pilot, Occupation.Engineer, Occupation.Marine };

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

    /// <returns> If the population in habitat should be available to transport to and from. </returns>
    public virtual bool IsTransferHabitat() {
        return true;
    }
}
