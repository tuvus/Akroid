using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlayerFactionAI : FactionAI {
    Chapter1 chapter1;
    FactionCommManager commManager;
    MiningStation playerMiningStation;
    List<Station> tradeRoutes;
    int nextStationToSendTo;
    private bool nextState;

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
        foreach (var ship in playerMiningStation.GetAllDockedShips()) {
            if (ship.IsScienceShip() && !ship.IsDamaged()) {
                ship.moduleSystem.Get<ResearchEquipment>().ForEach(r => faction.AddScience(r.DownloadData()));
            }
        }
    }

    void ManageIdleShips() {
        foreach (var ship in idleShips.Where((s) => s.IsIdle() && s.IsTransportShip() && s.fleet == null)) {
            if (tradeRoutes.Count == 0) break;
            nextStationToSendTo++;
            if (nextStationToSendTo >= tradeRoutes.Count)
                nextStationToSendTo = 0;
            ship.shipAI.AddUnitAICommand(
                Command.CreateTransportCommand(playerMiningStation, tradeRoutes[nextStationToSendTo], CargoBay.CargoTypes.All, true),
                Command.CommandAction.AddToEnd);
        }
    }

    public override void OnShipBuilt(Ship ship) {
        if (ship.IsCombatShip()) {
            battleManager.GetLocalPlayer().AddOwnedUnit(ship);
        }

        ship.shipAI.AddUnitAICommand(Command.CreateDockCommand(playerMiningStation));
    }

    public bool WantMoreTransportShips() {
        if (playerMiningStation.GetMiningStationAI().GetWantedTransportShips() > faction.GetShipCountOfType(Ship.ShipType.Transport) +
            chapter1.shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction)) {
            return true;
        } else {
            return false;
        }
    }

    public void AddTradeRouteToStation(Station station) {
        tradeRoutes.Add(station);
    }

    public override Station GetFleetCommand() {
        return playerMiningStation;
    }
}
