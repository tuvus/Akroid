using System;
using UnityEngine;

public class StationAI {
    [SerializeField] protected float cargoSpeed;
    protected Station station;
    [SerializeField] protected float waitSpeed;
    protected float waitTime;

    public StationAI(Station station) {
        this.station = station;
    }
    public event Action<Ship> OnBuildShip = delegate { };

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
