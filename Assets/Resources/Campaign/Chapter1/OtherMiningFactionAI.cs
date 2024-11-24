using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OtherMiningFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    MiningStation otherMiningStation;
    Station tradeStation;

    public OtherMiningFactionAI(BattleManager battleManager, Faction faction) : base(battleManager, faction) {    }


    public void Setup(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, MiningStation otherMiningStation, Station tradeStation) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.otherMiningStation = otherMiningStation;
        this.tradeStation = tradeStation;
        // We need to re-add the Idle ships since we are seting up after creating them
        faction.ships.ToList().ForEach((s) => idleShips.Add(s));
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        if (otherMiningStation.faction == faction) {
            BuyMinningShips();
        }
        ManageIdleShips();
    }

    void BuyMinningShips() {
        if (otherMiningStation.GetMiningStationAI().GetWantedTransportShips() > shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction)) {
            Ship.ShipBlueprint shipBlueprint = battleManager.GetShipBlueprint(Ship.ShipClass.Transport);
            long metalToUse = shipBlueprint.shipScriptableObject.resourceCosts[shipBlueprint.shipScriptableObject.resourceTypes.IndexOf(CargoBay.CargoTypes.Metal)];
            long metalCost = (long)(metalToUse * chapter1.resourceCosts[CargoBay.CargoTypes.Metal] * 1.2f);
            long transportCost = shipBlueprint.shipScriptableObject.cost + metalCost;
            long transportCount = faction.ships.Count + chapter1.shipyard.GetConstructionBay().buildQueue.Count((s) => s.faction == faction);
            if (transportCount < 4 && faction.credits > 10000 * transportCount + transportCost) {
                chapter1.shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction, shipBlueprint));
                faction.TransferCredits(metalCost, chapter1.shipyardFaction);
            }
        }
    }

    void ManageIdleShips() {
        foreach (var idleShip in idleShips) {
            if (idleShip.IsTransportShip()) {
                idleShip.shipAI.AddUnitAICommand(Command.CreateTransportCommand(otherMiningStation, tradeStation, CargoBay.CargoTypes.Metal), Command.CommandAction.AddToEnd);
            }
        }
    }
}
