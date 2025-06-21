using System.Linq;

public class ShipyardAI : StationAI {
    public bool autoCollectCargo;

    public ShipyardAI(Station station) : base(station) {
        autoCollectCargo = true;
        station.SetDesiredFreeCargoRange(CargoBay.CargoTypes.Metal, 1200 * 6, 1200 * 18);
        station.SetDesiredFreeCargoRange(CargoBay.CargoTypes.Gas, 1200 * 4, 1200 * 12);
    }

    public override void UpdateAI(float deltaTime) {
        base.UpdateAI(deltaTime);
    }

    protected override void ManageStationRepair() {
        int repairAmmount = (int)(GetShipyard().GetRepairAmount() *
            station.faction.GetImprovementModifier(Faction.ImprovementAreas.HullStrength));
        if (repairAmmount > 0 && station.GetHealth() < station.GetMaxHealth() / 2)
            repairAmmount = station.Repair(repairAmmount);
        foreach (Ship ship in station.GetAllDockedShips()) {
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
