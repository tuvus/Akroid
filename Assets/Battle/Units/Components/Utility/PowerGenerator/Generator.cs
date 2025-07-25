using Unity.Mathematics;

public class Generator : ModuleComponent {
    private float consumptionTime;
    private GeneratorScriptableObject generatorScriptableObject;

    public Generator(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        generatorScriptableObject = (GeneratorScriptableObject)componentScriptableObject;
        if (unit.IsStation()) ((Station)unit).ReserveCargo(1200 * 4, CargoBay.CargoType.Gas);
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        generatorScriptableObject = (GeneratorScriptableObject)componentScriptableObject;
    }

    public void UpdateGenerator(float deltaTime) {
        consumptionTime -= deltaTime;
        if (consumptionTime <= 0) {
            long resourcesToUse = math.max(0, math.min(generatorScriptableObject.consumptionAmount,
                unit.GetAllCargoOfType(generatorScriptableObject.consumptionType, true) - 2400));
            if (resourcesToUse == 0) {
                consumptionTime = generatorScriptableObject.consumptionSpeed;
                return;
            }

            unit.UseCargo(resourcesToUse, generatorScriptableObject.consumptionType);
            unit.faction.AddCredits(generatorScriptableObject.energyGain *
                (resourcesToUse / generatorScriptableObject.consumptionAmount));
            consumptionTime += generatorScriptableObject.consumptionSpeed;
        }
    }
}
