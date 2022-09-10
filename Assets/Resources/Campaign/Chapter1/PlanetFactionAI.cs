using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    Planet planet;
    Station tradeStation;
    Shipyard shipyard;

    float updateTime;
    float metalOrder;

    public void SetupPlanetFactionAI(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, Planet planet, Station tradeStation, Shipyard shipyard) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.planet = planet;
        this.tradeStation = tradeStation;
        this.shipyard = shipyard;
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        updateTime -= deltaTime;
        if (updateTime <= 0) {
            updateTime += 10;
            faction.AddCredits(planet.GetPopulation() / 50000000);
            if (tradeStation != null && tradeStation.IsSpawned()) {
                UpdateTradeStation();
            }
        }
    }

    void UpdateTradeStation() {
        int count = 0;
        while (tradeStation.GetHanger().GetTransportShip(count) != null) {
            Ship transportShip = tradeStation.GetHanger().GetTransportShip(count);
            if (transportShip.faction != faction) {
                long cost = (long)(transportShip.GetAllCargo(CargoBay.CargoTypes.Metal) * chapter1.GetMetalCost() * 0.8f);
                if (faction.UseCredits(cost)) {
                    transportShip.faction.AddCredits(cost);
                    transportShip.GetCargoBay().UseCargo(transportShip.GetAllCargo(CargoBay.CargoTypes.Metal), CargoBay.CargoTypes.Metal);
                }
            } else if (transportShip.faction == faction && metalOrder > 0) {
                //long cost = (long)(metalOrder * chapter1.GetMetalCost());
                //if (faction.UseCredits(cost)) {
                metalOrder = transportShip.GetCargoBay().LoadCargo(metalOrder, CargoBay.CargoTypes.Metal);
                //}
            }
            count++;
        }
        if (faction.credits > 200000 * (faction.GetShipsOfType(Ship.ShipType.Transport) + shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction.factionIndex)) && 7 < faction.GetShipsOfType(Ship.ShipType.Transport) + shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction.factionIndex)) {
            if (faction.GetShipsOfType(Ship.ShipType.Transport) + shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction.factionIndex) < 3) {
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
                    idleShips[i].shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Transport, tradeStation, shipyard), ShipAI.CommandAction.AddToEnd);
                }
            }
        }
    }

    public bool AddMetalOrder(Faction faction, float metal) {
        if (faction.UseCredits((long)(metal * chapter1.GetMetalCost()))) {
            metalOrder += metal;
            //this.faction.AddCredits((long)(metal * chapter1.GetMetalCost()));
            return true;
        }
        return false;
    }
}
