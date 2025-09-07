using System;
using Unity.Mathematics;

public class PopulationCenter : HabitationArea {
    public static readonly float civilianRatio = .79f;
    public static readonly float engineerRatio = .2f;
    public static readonly float pilotRatio = .01f;
    public static readonly float marineRatio = .1f;
    private float engineerFloat;
    private float marineFloat;
    private float pilotFloat;
    private PopulationCenterScriptableObject populationCenterScriptableObject;

    private float populationFloat;

    public PopulationCenter(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        populationCenterScriptableObject = (PopulationCenterScriptableObject)componentScriptableObject;
    }

    public static float GetOccupationRatio(Occupation o) {
        switch (o) {
            case Occupation.Civilian:
                return civilianRatio;
            case Occupation.Pilot:
                return pilotRatio;
            case Occupation.Engineer:
                return engineerRatio;
            case Occupation.Marine:
                return marineRatio;
            default:
                throw new ArgumentOutOfRangeException(nameof(o), o, null);
        }
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        populationCenterScriptableObject = (PopulationCenterScriptableObject)componentScriptableObject;
    }

    public void UpdatePopulationCenter(float deltaTime) {
        float growth = population.TotalPopulation() * .1f * deltaTime + populationFloat;
        populationFloat = growth - (long)growth;
        bool hadChange = false;

        long civilianGrowth = math.min(populationCenterScriptableObject.populationSpace - population.TotalPopulation(),
            (long)growth);
        if (civilianGrowth > 0) {
            population.civilians += civilianGrowth;
            hadChange = true;
        }

        long pilotTarget = (long)(population.civilians * pilotRatio);
        float pilotGrowth = (pilotTarget - population.pilots) * deltaTime / 20 + pilotFloat;
        if ((long)pilotGrowth > 0) {
            population.civilians -= (long)pilotGrowth;
            population.pilots += (long)pilotGrowth;
            hadChange = true;
        }
        pilotFloat = pilotGrowth - (long)pilotGrowth;

        long engineerTarget = (long)(population.civilians * engineerRatio);
        float engineerGrowth = (engineerTarget - population.engineers) * deltaTime / 50 + engineerFloat;
        if ((long)engineerGrowth > 0) {
            population.civilians -= (long)engineerGrowth;
            population.engineers += (long)engineerGrowth;
            hadChange = true;
        }
        engineerFloat = engineerGrowth - (long)engineerGrowth;

        long marineTarget = (long)(population.civilians * marineRatio);
        float marineGrowth = (marineTarget - population.marines) * deltaTime / 50 + marineFloat;
        if ((long)marineGrowth > 0) {
            population.civilians -= (long)marineGrowth;
            population.marines += (long)marineGrowth;
            hadChange = true;
        }
        marineFloat = marineGrowth - (long)marineGrowth;

        long civilianRequested = (long)(GetCapacity() * civilianRatio / 2 - population.civilians);
        if (civilianRequested > 0 && unit is Station station && !(station.personnelRequests.ContainsKey(this) &&
            station.personnelRequests[this].civilians == civilianRequested)) {
            hadChange = true;
            if (station.personnelRequests.ContainsKey(this)) {
                station.personnelRequests[this].civilians = civilianRequested;
            } else {
                station.personnelRequests.Add(this, new Population(civilianRequested));
            }
        }

        if (hadChange && unit.IsStation()) {
            ((Station)unit).updatePopulation = true;
        }
    }

    public void FillBasicPop(float percentage) {
        population.AddPopulation(new Population().SetBasicPopulation((long)(GetFreeSpace() * percentage)));
    }
}
