public class ShipyardAI : StationAI {
    public bool autoCollectCargo;

    public ShipyardAI(Station station) : base(station) {
        autoCollectCargo = true;
    }

    public override void UpdateAI(float deltaTime) {
        base.UpdateAI(deltaTime);
        UpdateShipyard();
    }

    void UpdateShipyard() {
        if (cargoTime <= 0 && autoCollectCargo) {
            foreach (var ship in station.GetAllDockedShips()) {
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
        int repairAmmount = GetShipyard().GetRepairAmount();
        if (repairAmmount > 0 && station.GetHealth() < station.GetMaxHealth() / 2)
            repairAmmount = station.Repair(repairAmmount);
        foreach (var ship in station.GetAllDockedShips()) {
            if (ship.IsDamaged()) {
                repairAmmount = station.RepairUnit(ship, repairAmmount);
            }
        }

        if (repairAmmount > 0 && station.IsDamaged())
            station.RepairUnit(station, repairAmmount);
    }

    public Shipyard GetShipyard() {
        return (Shipyard)station;
    }
}
