using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipyardAI : StationAI {
    public override void UpdateAI(float deltaTime) {
        base.UpdateAI(deltaTime);
        UpdateShipyard();
    }

    void UpdateShipyard() {
        if (cargoTime <= 0) {
            for (int i = 0; i < station.GetHanger().ships.Count; i++) {
                Ship ship = station.GetHanger().ships[i];
                if (ship.GetCargoBay() != null && !ship.GetCargoBay().IsCargoEmptyOfType(CargoBay.CargoTypes.Metal)) {
                    station.GetCargoBay().LoadCargoFromBay(ship.GetCargoBay(), CargoBay.CargoTypes.Metal, cargoAmmount);
                }
            }
            cargoTime += cargoSpeed;
        }
    }

    protected override void ManageStationRepair() {
        int repairAmmount = GetShipyard().GetRepairAmmount();
        if (repairAmmount > 0 && station.GetHealth() < station.GetMaxHealth() / 2)
            repairAmmount = station.Repair(repairAmmount);
        for (int i = 0; i < station.GetHanger().GetShips().Count; i++) {
            if (repairAmmount == 0)
                return;
            Ship targetShip = station.GetHanger().GetShips()[i];
            if (targetShip.IsDamaged()) {
                repairAmmount = station.RepairUnit(targetShip, repairAmmount);
            }
        }
        if (repairAmmount > 0 && station.IsDamaged())
            station.RepairUnit(station, repairAmmount);
    }

    public Shipyard GetShipyard() {
        return (Shipyard)station;
    }
}
