using System;
using Unity.Mathematics;
using UnityEngine;

public class PopulationCenter : HabitationArea {
    private PopulationCenterScriptableObject populationCenterScriptableObject;

    private float populationFloat;
    private float engineerFloat;
    private float pilotFloat;
    private float marineFloat;

    public static readonly float civilianRatio = .799f;
    public static readonly float engineerRatio = .2f;
    public static readonly float pilotRatio = .001f;
    public static readonly float marineRatio = .1f;


    public PopulationCenter(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        populationCenterScriptableObject = (PopulationCenterScriptableObject)componentScriptableObject;
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
            (long) growth);
        if (civilianGrowth > 0) {
            population.civilians += civilianGrowth;
            hadChange = true;
        }

        long pilotTarget = (long)(population.civilians * pilotRatio);
        float pilotGrowth = (pilotTarget - population.pilots) * deltaTime / 50 + pilotFloat;
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

        if (hadChange && unit.IsStation()) {
            ((Station)unit).updatePopulation = true;
        }
    }
}
