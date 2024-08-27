using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

public class Generator : ModuleComponent {
    GeneratorScriptableObject generatorScriptableObject;
    private Unit unit;
    private float consumptionTime;

    public override void SetupComponent(Module module, Faction faction, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, faction, componentScriptableObject);
        generatorScriptableObject = (GeneratorScriptableObject)componentScriptableObject;
    }

    public void SetupGenerator(Unit unit) {
        this.unit = unit;
    }

    public void UpdateGenerator(float deltaTime) {
        consumptionTime -= deltaTime;
        if (consumptionTime <= 0) {
            long resourcesToUse = math.min(generatorScriptableObject.consumptionAmount, unit.GetAllCargoOfType(generatorScriptableObject.consumptionType, true) - 2400);
            if (resourcesToUse == 0) {
                consumptionTime = generatorScriptableObject.consumptionSpeed;
                return;
            } else {
                unit.UseCargo(resourcesToUse, generatorScriptableObject.consumptionType);
                unit.faction.AddCredits(generatorScriptableObject.energyGain * (resourcesToUse / generatorScriptableObject.consumptionAmount));
                consumptionTime += generatorScriptableObject.consumptionSpeed;
            }
        }
    }

}
