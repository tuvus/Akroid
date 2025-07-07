using System.Linq;

public class OtherMiningFactionAI : FactionAI {
    private Chapter1 chapter1;
    private MiningStation otherMiningStation;
    private ShipyardFactionAI shipyardFactionAI;
    private Station tradeStation;

    public OtherMiningFactionAI(BattleManager battleManager, Faction faction) : base(battleManager, faction) { }


    public void Setup(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, MiningStation otherMiningStation,
        Station tradeStation) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.otherMiningStation = otherMiningStation;
        this.tradeStation = tradeStation;
        // We need to re-add the Idle ships since we are setting up after creating them
        faction.ships.ToList().ForEach(s => idleShips.Add(s));
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        if (otherMiningStation.faction == faction) {
            BuyMiningShips();
        }

        ManageIdleShips();
    }

    private void BuyMiningShips() {
        if (otherMiningStation.GetMiningStationAI().GetWantedTransportShips() >
            shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction)) {
            Ship.ShipBlueprint shipBlueprint = battleManager.GetShipBlueprint(Ship.ShipClass.Transport);
            long metalToUse =
                shipBlueprint.shipScriptableObject.resourceCosts[
                    shipBlueprint.shipScriptableObject.resourceTypes.IndexOf(CargoBay.CargoTypes.Metal)];
            long metalCost = (long)(metalToUse * battleManager.baseResourcePrice[CargoBay.CargoTypes.Metal] * 1.2f);
            long transportCost = shipBlueprint.shipScriptableObject.cost + metalCost;
            long transportCount =
                faction.ships.Count +
                chapter1.shipyard.GetConstructionBay().buildQueue.Count(s => s.faction == faction);
            if (transportCount < 4 && faction.credits > 10000 * transportCount + transportCost) {
                chapter1.shipyard.GetConstructionBay()
                    .AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction, shipBlueprint));
                faction.TransferCredits(metalCost, chapter1.shipyardFaction);
            }
        }
    }

    private void ManageIdleShips() {
        foreach (Ship idleShip in idleShips) {
            if (idleShip.IsTransportShip()) {
                idleShip.shipAI.AddUnitAICommand(Command.CreateTradeCommand(otherMiningStation));
            }
        }
    }
}
