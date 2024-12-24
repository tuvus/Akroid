using System;
using UnityEngine;

public class StationAI {
    protected Station station;
    [SerializeField] protected float waitSpeed;
    [SerializeField] protected float cargoSpeed;
    protected float waitTime;
    public event Action<Ship> OnBuildShip = delegate { };

    public StationAI(Station station) {
        this.station = station;
    }

    public virtual void UpdateAI(float deltaTime) {
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
