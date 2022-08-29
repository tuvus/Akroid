using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class FleetCommandAI : StationAI {
    public override void UpdateAI() {
        base.UpdateAI();
        UpdateFleetCommand();
    }

    private void UpdateFleetCommand() {
        Profiler.BeginSample("FleetCommandAI");
        if (cargoTime <= 0) {
            foreach (var ship in station.GetHanger().GetShips()) {
                if (ship.GetShipClass() == Ship.ShipClass.Transport && !ship.GetCargoBay().IsCargoEmptyOfType(CargoBay.CargoTypes.Metal)) {
                    station.GetCargoBay().LoadCargoFromBay(ship.GetCargoBay(), CargoBay.CargoTypes.Metal, 300);
                    cargoTime = 1;
                    break;
                }
            }

        }
        if (waitTime <= 0) {
            Ship scienceShip = station.GetHanger().GetResearchShip();
            if (scienceShip != null && !scienceShip.IsDammaged()) {
                station.faction.AddScience(scienceShip.GetResearchEquiptment().DownloadData());
            }
            waitTime += 3;
        }
        Profiler.EndSample();
    }

    public override void OnShipBuilt(Ship ship) {

    }

    protected override void ManageStationRepair() {
        int repairAmmount = GetShipyard().GetRepairAmmount();
        if (station.GetHealth() < station.GetMaxHealth() / 2)
            repairAmmount = station.Repair(repairAmmount);
        for (int i = 0; i < station.GetHanger().GetShips().Count; i++) {
            Ship targetShip = station.GetHanger().GetShips()[i];
            if (targetShip.IsDammaged()) {
                repairAmmount = station.RepairUnit(targetShip, repairAmmount);
            }
        }
        if (station.IsDammaged())
            station.RepairUnit(station, repairAmmount);
        station.repairTime += station.repairSpeed;
    }

    public Shipyard GetShipyard() {
        return (Shipyard)station;
    }
}
