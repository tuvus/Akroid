using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchEquipment : ModuleComponent {
    Ship ship;
    ResearchEquipmentScriptableObject researchEquipmentScriptableObject;
    [SerializeField] int data;
    float researchTime;

    public override void SetupComponent(Module module, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, componentScriptableObject);
        researchEquipmentScriptableObject = (ResearchEquipmentScriptableObject)componentScriptableObject;
    }

    public void SetupResearchEquipment(Ship ship) {
        this.ship = ship;
        researchTime = researchEquipmentScriptableObject.researchSpeed;
        data = 0;
    }

    /// <returns>False if we done collecting data, true otherwise</returns>
    public bool GatherData(Star star, float time) {
        researchTime -= time;
        if (researchTime <= 0) {
            data = Mathf.Min(researchEquipmentScriptableObject.maxData, data + researchEquipmentScriptableObject.researchAmount);
            if (data == researchEquipmentScriptableObject.maxData) {
                researchTime = researchEquipmentScriptableObject.researchSpeed;
                return false;
            }
            researchTime += researchEquipmentScriptableObject.researchSpeed;
        }
        return true;
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
