using System;
using UnityEngine;

public class StationAI {
    protected Station station;
    protected float waitTime;

    public StationAI(Station station) {
        this.station = station;
    }
    public event Action<Ship> OnBuildShip = delegate { };

    public void UpdateAI(float deltaTime) {
        waitTime = Mathf.Max(waitTime - deltaTime, 0);
        if (station.repairTime <= 0) {
            ManageStationRepair();
        }
    }

    protected virtual void ManageStationRepair() {
        if (station.IsDamaged())
            station.RepairUnit(station, station.GetRepairAmount());
    }

    public virtual void OnShipBuilt(Ship ship) {
        OnBuildShip.Invoke(ship);
    }
}
