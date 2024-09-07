using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationAI {
    protected Station station;
    [SerializeField] protected float waitSpeed;
    [SerializeField] protected float cargoSpeed;
    [field: SerializeField] public long cargoAmount { get; private set; }
    protected float waitTime;
    protected float cargoTime;
    public event Action<Ship> onBuildShip;

    public StationAI(Station station) {
        this.station = station;
        onBuildShip = delegate { };
    }

    public virtual void UpdateAI(float deltaTime) {
        waitTime = Mathf.Max(waitTime - deltaTime, 0);
        cargoTime = Mathf.Max(cargoTime - deltaTime, 0);
        if (station.repairTime <= 0) {
            ManageStationRepair();
        }
    }

    protected virtual void ManageStationRepair() {
        if (station.IsDamaged())
            station.RepairUnit(station, station.GetRepairAmmount());
    }

    public virtual void OnShipBuilt(Ship ship) {
        onBuildShip.Invoke(ship);
    }
}