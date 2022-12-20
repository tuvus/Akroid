using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherMiningFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    MiningStation otherMiningStation;
    Station tradeStation;

    public void SetupOtherMiningFactionAI(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, MiningStation otherMiningStation, Station tradeStation) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.otherMiningStation = otherMiningStation;
        this.tradeStation = tradeStation;
    }

    public override void UpdateFactionAI(float deltaTime) {
        if (faction.credits > 10000) {
            if (otherMiningStation.GetMiningStationAI().GetWantedTransportShips() > faction.GetShipsOfType(Ship.ShipType.Transport) + shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction.factionIndex)) {
                shipyardFactionAI.PlaceTransportOrder(faction);
            }
        }
        ManageIdleShips();
    }

    void ManageIdleShips() {
        for (int i = 0; i < idleShips.Count; i++) {
            if (idleShips[i].IsIdle()) {
                if (idleShips[i].IsTransportShip()) {
                    idleShips[i].shipAI.AddUnitAICommand(new Command(Command.CommandType.Transport, otherMiningStation, tradeStation), ShipAI.CommandAction.AddToEnd);
                }
            }
        }
    }
}