using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class StationAI : MonoBehaviour {
    protected Station station;
    [SerializeField] protected float waitSpeed;
    [SerializeField] protected float cargoSpeed;
    [SerializeField] protected long cargoAmmount;
    protected float waitTime;
    protected float cargoTime;

    public virtual void SetupStationAI(Station station) {
        this.station = station;
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
        if (ship.faction == station.faction) {
            ship.faction.GetFactionAI().OnShipBuilt(ship);
        } else {
            ship.faction.GetFactionAI().OnShipBuilt(ship);
            station.faction.GetFactionAI().OnShipBuiltForAnotherFaction(ship, ship.faction);
        }
    }
}