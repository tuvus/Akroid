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
            foreach (var ship in station.GetHanger().ships) {
                if (ship.GetAllCargoOfType(CargoBay.CargoTypes.Metal) > 0) {
                    station.LoadCargoFromUnit(cargoAmount, CargoBay.CargoTypes.Metal, ship);
                } else if (ship.GetAllCargoOfType(CargoBay.CargoTypes.Gas) > 0) {
                    station.LoadCargoFromUnit(cargoAmount, CargoBay.CargoTypes.Gas, ship);
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
