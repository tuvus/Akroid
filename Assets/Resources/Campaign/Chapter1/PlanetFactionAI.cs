using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class PlanetFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    Planet planet;
    Shipyard tradeStation;
    Shipyard shipyard;
    List<Ship> civilianShips;
    List<Station> friendlyStations;

    float updateTime;
    float sellResourcesToPlanetTime;

    public PlanetFactionAI(BattleManager battleManager, Faction faction) : base(battleManager, faction) { }

    public void Setup(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, Planet planet, Shipyard tradeStation, Shipyard shipyard,
        List<Ship> civilianShips, EventManager eventManager) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.planet = planet;
        this.tradeStation = tradeStation;
        this.shipyard = shipyard;
        this.civilianShips = civilianShips;
        friendlyStations = new List<Station>();
        // We need to re-add the Idle ships since we are seting up after creating them
        faction.ships.ToList().ForEach((s) => idleShips.Add(s));

        void produceCivilianShipDelayed() {
            eventManager.AddEvent(eventManager.CreateWaitEvent(1000 + Random.Range(0, 1000)), () => {
                tradeStation.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction,
                    battleManager.GetShipBlueprint(Ship.ShipType.Civilian), "Civilian Ship"));
                produceCivilianShipDelayed();
            });
        }

        eventManager.AddEvent(eventManager.CreateWaitEvent(40000), () => { produceCivilianShipDelayed(); });
        produceCivilianShipDelayed();
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        updateTime -= deltaTime;
        sellResourcesToPlanetTime -= deltaTime;
        if (updateTime <= 0) {
            updateTime += 10;
            faction.AddCredits(planet.GetPopulation() / 100000000);
            if (tradeStation != null && tradeStation.IsSpawned()) {
                UpdateTradeStation();
            }
        }
    }

    void UpdateTradeStation() {
        foreach (var transportShip in tradeStation.GetAllDockedShips().Where(s => s.IsTransportShip())) {
            if (transportShip.faction != faction && transportShip.faction != shipyardFactionAI.faction) {
                long amountToTransfer = tradeStation.stationAI.cargoAmount;
                foreach (var type in CargoBay.allCargoTypes) {
                    if (amountToTransfer <= 0) break;
                    long amountOfResource = math.min(amountToTransfer, transportShip.GetAllCargoOfType(type));
                    if (faction.TransferCredits((long)(amountOfResource * chapter1.resourceCosts[type]), transportShip.faction)) {
                        tradeStation.LoadCargoFromUnit(amountOfResource, type, transportShip);
                    }
                }
            }
        }

        if (sellResourcesToPlanetTime <= 0) {
            foreach (var type in CargoBay.allCargoTypes) {
                long amount = math.min(100, tradeStation.GetAllCargoOfType(type, true) - 4800);
                if (amount <= 0) continue;
                tradeStation.UseCargo(amount, type);
                faction.AddCredits((long)(amount * chapter1.resourceCosts[type] * .5f));
            }

            sellResourcesToPlanetTime += 5;
        }

        ManageIdleShips();
    }

    void ManageIdleShips() {
        friendlyStations.Clear();
        friendlyStations.AddRange(battleManager.stations.Where(s => !faction.IsAtWarWithFaction(s.faction)));
        foreach (var idleShip in idleShips) {
            if (idleShip.IsIdle() && idleShip.IsCivilianShip()) {
                int randomNumber = Random.Range(0, 100);
                if (friendlyStations.Count > 0 && (idleShip.dockedStation != null && randomNumber > 20) ||
                    (idleShip.dockedStation == null && randomNumber > 80)) {
                    idleShip.shipAI.AddUnitAICommand(Command.CreateDockCommand(friendlyStations[Random.Range(0, friendlyStations.Count)]));
                    idleShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(7, 30f)));
                } else {
                    if (idleShip.dockedStation != null)
                        idleShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(idleShip.GetPosition() +
                                                                                   Calculator.GetPositionOutOfAngleAndDistance(
                                                                                       Random.Range(0, 360), Random.Range(6000, 12000))));
                    else
                        idleShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(idleShip.GetPosition() +
                                                                                   Calculator.GetPositionOutOfAngleAndDistance(
                                                                                       idleShip.rotation + Random.Range(-120, 120),
                                                                                       Random.Range(1000, 5000))));
                    idleShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(1, 3f)));
                }
            }
        }
    }
}
