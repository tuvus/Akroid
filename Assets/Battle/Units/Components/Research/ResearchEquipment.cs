using UnityEngine;

public class ResearchEquipment : ModuleComponent {
    [SerializeField] private int data;
    private ResearchEquipmentScriptableObject researchEquipmentScriptableObject;
    private float researchTime;

    public ResearchEquipment(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        researchEquipmentScriptableObject = (ResearchEquipmentScriptableObject)componentScriptableObject;

        researchTime = researchEquipmentScriptableObject.researchSpeed;
        data = 0;
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        researchEquipmentScriptableObject = (ResearchEquipmentScriptableObject)componentScriptableObject;
    }

    /// <returns> False if we done collecting data, true otherwise </returns>
    public bool GatherData(Star star, float time) {
        researchTime -= time;
        if (researchTime <= 0) {
            data = Mathf.Min(researchEquipmentScriptableObject.maxData,
                data + researchEquipmentScriptableObject.researchAmount);
            if (data == researchEquipmentScriptableObject.maxData) {
                researchTime = researchEquipmentScriptableObject.researchSpeed;
                return true;
            }

            researchTime += researchEquipmentScriptableObject.researchSpeed;
        }

        return false;
    }

    public int DownloadData() {
        int returnValue = data;
        data = 0;
        return returnValue;
    }

    public bool WantsMoreData() {
        return data != researchEquipmentScriptableObject.maxData;
    }
}
