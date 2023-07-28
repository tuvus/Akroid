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
            int count = 0;
            Ship ship = station.GetHanger().GetTransportShip(count);
            while (ship != null && ship.GetCargoBay() != null) {
                if (!ship.GetCargoBay().IsCargoEmptyOfType(CargoBay.CargoTypes.Metal)) {
                    station.GetCargoBay().LoadCargoFromBay(ship.GetCargoBay(), CargoBay.CargoTypes.Metal, cargoAmmount);
                    cargoTime += cargoSpeed;
                    break;
                }
                count++;
                ship = station.GetHanger().GetTransportShip(count);
            }
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
