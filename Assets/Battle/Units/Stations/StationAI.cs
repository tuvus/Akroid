using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class StationAI : MonoBehaviour {
    protected Station station;
    protected float waitTime;
    protected float cargoTime;

    public virtual void SetupStationAI(Station station) {
        this.station = station;
    }

    public virtual void UpdateAI() {
        Profiler.BeginSample("StationAI");
        waitTime = Mathf.Max(waitTime - Time.fixedDeltaTime * BattleManager.Instance.timeScale, 0);
        cargoTime = Mathf.Max(cargoTime - Time.fixedDeltaTime * BattleManager.Instance.timeScale, 0);
        if (station.repairTime <= 0) {
            ManageStationRepair();
        }
        Profiler.EndSample();
    }

    protected virtual void ManageStationRepair() {
        if (station.IsDammaged())
            station.RepairUnit(station,station.repairAmmount);
        station.repairTime += station.repairSpeed;
    }

}
