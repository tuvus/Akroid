using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    MiningStation playerMiningStation;
    List<Station> tradeRoutes;
    int nextStationToSendTo;

    public void SetupPlayerFactionAI(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, MiningStation playerMiningStation) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.playerMiningStation = playerMiningStation;
        tradeRoutes = new List<Station>();
        nextStationToSendTo = 0;
    }

    public override void UpdateFactionAI(float deltaTime) {
        ManageIdleShips();
    }

    void ManageIdleShips() {
        for (int i = 0; i < idleShips.Count; i++) {
            if (idleShips[i].IsIdle()) {
                if (idleShips[i].IsTransportShip()) {
                    if (tradeRoutes.Count > 0) {
                        nextStationToSendTo++;
                        if (nextStationToSendTo >= tradeRoutes.Count)
                            nextStationToSendTo = 0;
                        idleShips[i].shipAI.AddUnitAICommand(new Command(Command.CommandType.Transport, playerMiningStation, tradeRoutes[nextStationToSendTo], true), ShipAI.CommandAction.AddToEnd);
                    }
                }
            }
        }
    }

    public override void OnShipBuilt(Ship ship) {
        if (ship.IsCombatShip()) {
            LocalPlayer.Instance.AddOwnedUnit(ship);
            ship.GetUnitSelection().UpdateFactionColor();
        }
        ship.shipAI.AddUnitAICommand(new Command(Command.CommandType.Dock, playerMiningStation));
    }

    public bool WantMoreTransportShips() {
        if (playerMiningStation.GetMiningStationAI().GetWantedTransportShips() > faction.GetShipsOfType(Ship.ShipType.Transport) + shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction.factionIndex)) {
            return true;
        } else {
            return false;
        }
    }

    public void AddTradeRouteToStation(Station station) {
        tradeRoutes.Add(station);
    }
}