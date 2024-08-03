using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlanetFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    Planet planet;
    Station tradeStation;
    Shipyard shipyard;
    List<Ship> civilianShips;
    List<Station> friendlyStations;

    float updateTime;
    float sellMetalToPlanetTime;

    public void SetupPlanetFactionAI(BattleManager battleManger, Faction faction, Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, Planet planet, Station tradeStation, Shipyard shipyard, List<Ship> civilianShips) {
        base.SetupFactionAI(battleManger, faction);
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.planet = planet;
        this.tradeStation = tradeStation;
        this.shipyard = shipyard;
        this.civilianShips = civilianShips;
        friendlyStations = new List<Station>();
        // We need to re-add the Idle ships since we are seting up after creating them
        idleShips.AddRange(faction.ships);
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        updateTime -= deltaTime;
        sellMetalToPlanetTime -= deltaTime;
        if (updateTime <= 0) {
            updateTime += 10;
            faction.AddCredits(planet.GetPopulation() / 100000000);
            if (tradeStation != null && tradeStation.IsSpawned()) {
                UpdateTradeStation();
            }
        }
    }

    void UpdateTradeStation() {
        foreach (var transportShip in tradeStation.GetHanger().GetTransportShips()) {
            if (transportShip.faction != faction && transportShip.faction != shipyardFactionAI.faction) {
                long cost = (long)(transportShip.GetAllCargoOfType(CargoBay.CargoTypes.Metal) * chapter1.GetMetalCost() * 0.2f);
                if (faction.UseCredits(cost)) {
                    transportShip.faction.AddCredits(cost);
                    transportShip.GetCargoBay().UseCargo(transportShip.GetAllCargoOfType(CargoBay.CargoTypes.Metal), CargoBay.CargoTypes.Metal);
                }
            }
        }

        if (sellMetalToPlanetTime <= 0) {
            faction.AddCredits((long)((100 - tradeStation.UseCargo(100, CargoBay.CargoTypes.Metal)) * chapter1.GetMetalCost() * .8f));
            sellMetalToPlanetTime += 10;
        }
        ManageIdleShips();
    }

    void ManageIdleShips() {
        friendlyStations.Clear();
        friendlyStations.AddRange(battleManager.stations.Where(s => !faction.IsAtWarWithFaction(s.faction)));
        foreach (var idleShip in idleShips) {
            if (idleShip.IsIdle() && idleShip.IsCivilianShip()) {
                int randomNumber = Random.Range(0, 100);
                if (friendlyStations.Count > 0 && (idleShip.dockedStation != null && randomNumber > 20) || (idleShip.dockedStation == null && randomNumber > 80)) {
                    idleShip.shipAI.AddUnitAICommand(Command.CreateDockCommand(friendlyStations[Random.Range(0, friendlyStations.Count)]));
                    idleShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(7, 30f)));
                } else {
                    if (idleShip.dockedStation != null)
                        idleShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(idleShip.GetPosition() + Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0, 360), Random.Range(6000, 12000))));
                    else
                        idleShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(idleShip.GetPosition() + Calculator.GetPositionOutOfAngleAndDistance(idleShip.GetRotation() + Random.Range(-120, 120), Random.Range(1000, 5000))));
                    idleShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(0, 3f)));
                }
            }
        }
    }
}
