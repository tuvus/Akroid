using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OtherMiningFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    MiningStation otherMiningStation;
    Station tradeStation;

    public void SetupOtherMiningFactionAI(BattleManager battleManger, Faction faction, Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, MiningStation otherMiningStation, Station tradeStation) {
        base.SetupFactionAI(battleManager, faction);
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.otherMiningStation = otherMiningStation;
        this.tradeStation = tradeStation;
        // We need to re-add the Idle ships since we are seting up after creating them
        idleShips.AddRange(faction.ships);
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        if (otherMiningStation.GetMiningStationAI().GetWantedTransportShips() > shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction)) {
            Ship.ShipBlueprint shipBlueprint = battleManager.GetShipBlueprint(Ship.ShipClass.Transport);
            long metalToUse = shipBlueprint.shipScriptableObject.resourceCosts[shipBlueprint.shipScriptableObject.resourceTypes.IndexOf(CargoBay.CargoTypes.Metal)];
            long metalCost = (long)(metalToUse * chapter1.GetMetalCost() * 1.2f);
            long transportCost = shipBlueprint.shipScriptableObject.cost + metalCost;
            if (faction.credits > 10000 + transportCost) {
                chapter1.shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction, shipBlueprint));
                faction.TransferCredits(metalCost, chapter1.shipyardFaction);
            }
        }
        ManageIdleShips();
    }

    void ManageIdleShips() {
        for (int i = 0; i < idleShips.Count; i++) {
            if (idleShips[i].IsIdle()) {
                if (idleShips[i].IsTransportShip()) {
                    idleShips[i].shipAI.AddUnitAICommand(Command.CreateTransportCommand(otherMiningStation, tradeStation), Command.CommandAction.AddToEnd);
                }
            }
        }
    }
}