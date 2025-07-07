
public class ShipyardAI : StationAI {

    public ShipyardAI(Station station) : base(station) {
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
