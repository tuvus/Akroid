using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchEquiptment : MonoBehaviour {
    Ship ship;
    public int maxData;
    [SerializeField] int data;
    public int researchAmmount;
    public float researchSpeed;
    float researchTime;

    public void SetupResearchEquiptment(Ship ship) {
        this.ship = ship;
        researchTime = researchSpeed;
        data = 0;
    }

    public bool GatherData(Star star, float time) {
        researchTime -= time;
        if (researchTime <= 0) {
            data = Mathf.Min(maxData, data + researchAmmount);
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
