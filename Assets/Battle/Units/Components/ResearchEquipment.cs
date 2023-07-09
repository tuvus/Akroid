using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchEquipment : ModuleComponent {
    Ship ship;
    ResearchEquipmentScriptableObject researchEquipmentScriptableObject;
    public int maxData;
    [SerializeField] int data;
    public int researchAmount;
    public float researchSpeed;
    float researchTime;

    public override void SetupComponent(Module module, ComponentScriptableObject componentScriptableObject) {
        researchEquipmentScriptableObject = (ResearchEquipmentScriptableObject)componentScriptableObject;
    }

    public void SetupResearchEquipment(Ship ship) {
        this.ship = ship;
        researchTime = researchSpeed;
        data = 0;
    }

    public bool GatherData(Star star, float time) {
        researchTime -= time;
        if (researchTime <= 0) {
            data = Mathf.Min(maxData, data + researchAmount);
            if (data == maxData) {
                researchTime = researchSpeed;
                return true;
            }
            researchTime += researchSpeed;
        }
        return false;
    }

    public int DownloadData() {
        int returnValue = data;
        data = 0;
        return returnValue;
    }

    public bool WantMoreData() {
        return data != maxData;
    }
}
