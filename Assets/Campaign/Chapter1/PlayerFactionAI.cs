using System.Collections.Generic;
using System.Linq;

public class PlayerFactionAI : FactionAI {
    private Chapter1 chapter1;
    private FactionCommManager commManager;
    private bool nextState;
    private int nextStationToSendTo;
    private MiningStation playerMiningStation;
    private readonly List<Station> tradeRoutes;

    public PlayerFactionAI(BattleManager battleManager, Faction faction) : base(battleManager, faction) {
        tradeRoutes = new List<Station>();
        nextStationToSendTo = 0;
        commManager = faction.GetFactionCommManager();
        autoResearch = false;
    }

    public void Setup(Chapter1 chapter1, MiningStation playerMiningStation) {
        this.chapter1 = chapter1;
        this.playerMiningStation = playerMiningStation;
    }

    public override void UpdateFactionAI(float deltaTime) {
        ManageIdleShips();
        foreach (Ship ship in playerMiningStation.GetAllDockedShips()) {
            if (ship.IsScienceShip() && !ship.IsDamaged()) {
                ship.moduleSystem.Get<ResearchEquipment>().ForEach(r => faction.AddScience(r.DownloadData()));
            }
        }
    }

    private void ManageIdleShips() {
        foreach (Ship ship in idleShips.Where(s => s.IsIdle() && s.IsTransportShip() && s.fleet == null)) {
            if (tradeRoutes.Count == 0) break;
            nextStationToSendTo++;
            if (nextStationToSendTo >= tradeRoutes.Count)
                nextStationToSendTo = 0;
            ship.shipAI.AddUnitAICommand(
                Command.CreateTransportCommand(playerMiningStation, tradeRoutes[nextStationToSendTo],
                    CargoBay.CargoTypes.All, true, false));
        }
    }

    public override void OnShipBuilt(Ship ship) {
        if (ship.IsCombatShip()) {
            battleManager.GetLocalPlayer().AddOwnedUnit(ship);
        }

        ship.shipAI.AddUnitAICommand(Command.CreateDockCommand(playerMiningStation));
    }

    public bool WantMoreTransportShips() {
        if (playerMiningStation.GetMiningStationAI().GetWantedTransportShips() >
            faction.GetShipCountOfType(Ship.ShipType.Transport) +
            chapter1.shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction)) {
            return true;
        }
        return false;
    }

    public void AddTradeRouteToStation(Station station) {
        tradeRoutes.Add(station);
    }

    public override Station GetFleetCommand() {
        return playerMiningStation;
    }
}
