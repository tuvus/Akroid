using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    MiningStation playerMiningStation;
    Station tradeStation;

    public void SetupPlayerFactionAI(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, MiningStation playerMiningStation, Station tradeStation) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.playerMiningStation = playerMiningStation;
        this.tradeStation = tradeStation;
    }

    public override void UpdateFactionAI(float deltaTime) {
        if (faction.credits > 10000) {
            if (playerMiningStation.GetMiningStationAI().GetWantedTransportShips() > faction.GetShipsOfType(Ship.ShipType.Transport) + shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport,faction.factionIndex)) {
                shipyardFactionAI.PlaceTransportOrder(faction);
            } else {
                shipyardFactionAI.PlaceCombatOrder(faction);
            }
        }
        ManageIdleShips();
    }

    void ManageIdleShips() {
        for (int i = 0; i < idleShips.Count; i++) {
            if (idleShips[i].IsIdle()) {
                if (idleShips[i].IsTransportShip()) {
                    idleShips[i].shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Transport, playerMiningStation, tradeStation),ShipAI.CommandAction.AddToEnd);
                }
            }
        }
    }
}
