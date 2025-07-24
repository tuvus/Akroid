using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class PlanetFactionAI : FactionAI {
    private Chapter1 chapter1;
    private List<Ship> civilianShips;
    private List<Station> friendlyStations;
    private Planet planet;
    private float tradeWithPlanetTime;
    private Shipyard shipyard;
    private ShipyardFactionAI shipyardFactionAI;
    private Shipyard tradeStation;

    private float updateTime;

    public PlanetFactionAI(BattleManager battleManager, Faction faction) : base(battleManager, faction) { }

    public void Setup(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, Planet planet, Shipyard tradeStation,
        Shipyard shipyard,
        List<Ship> civilianShips, EventManager eventManager) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.planet = planet;
        this.tradeStation = tradeStation;
        this.shipyard = shipyard;
        this.civilianShips = civilianShips;
        friendlyStations = new List<Station>();
        // We need to re-add the Idle ships since we are setting up after creating them
        faction.ships.ToList().ForEach(s => idleShips.Add(s));

        void produceCivilianShipDelayed() {
            eventManager.AddEvent(eventManager.CreateWaitCondition(1000 + Random.Range(0, 1000)), () => {
                tradeStation.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction,
                    battleManager.GetShipBlueprint(Ship.ShipType.Civilian), "Civilian Ship"));
                produceCivilianShipDelayed();
            });
        }

        foreach (var keyValuePair in tradeStation.reservedCargo.ToList()) {
            tradeStation.UnReserveCargo(keyValuePair.Value.wanted, keyValuePair.Key);
        }

        eventManager.AddEvent(eventManager.CreateWaitCondition(40000), () => { produceCivilianShipDelayed(); });
        produceCivilianShipDelayed();
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        updateTime -= deltaTime;
        tradeWithPlanetTime -= deltaTime;
        if (updateTime <= 0) {
            updateTime += 10;
            faction.AddCredits(planet.GetPopulation() / 100000000);
            if (tradeStation != null && tradeStation.IsSpawned()) {
                UpdateTradeStation();
            }
        }
    }

    private void UpdateTradeStation() {
        if (tradeWithPlanetTime <= 0) {
            foreach (CargoBay.CargoType type in CargoBay.allCargoTypes) {
                long cargo = tradeStation.GetAllCargoOfType(type);
                if (cargo < tradeStation.freeCargo[type].minWanted + 400) {
                    // We have too little cargo, buy some at an expensive price from the planet
                    long amount = math.min(400, tradeStation.freeCargo[type].minWanted + 400 - cargo);
                    if (amount <= 0) continue;
                    tradeStation.LoadCargo(amount, type);
                    faction.UseCredits((long)(amount * battleManager.baseResourcePrice[type] * 2));
                } else if (cargo > (tradeStation.freeCargo[type].maxWanted + tradeStation.freeCargo[type].maxWanted) /
                    2) {
                    // We have too much cargo, sell some to the planet
                    long amount = math.min(800,
                        cargo - (tradeStation.freeCargo[type].maxWanted + tradeStation.freeCargo[type].maxWanted) / 2);
                    if (amount <= 0) continue;
                    tradeStation.UseCargo(amount, type);
                    faction.AddCredits((long)(amount * battleManager.baseResourcePrice[type] * 1.4f));
                }
            }

            PopulationCenter populationCenter = tradeStation.moduleSystem.Get<PopulationCenter>().First();
            if (populationCenter.population.engineers <
                populationCenter.GetCapacity() * PopulationCenter.engineerRatio) {
                long engineersToAdd = math.min(populationCenter.population.civilians, math.min(3,
                    (long)(populationCenter.GetCapacity() * PopulationCenter.engineerRatio) -
                    populationCenter.population.engineers));
                populationCenter.population.engineers += engineersToAdd;
                populationCenter.population.civilians -=
                    populationCenter.population.TotalPopulation() - populationCenter.GetCapacity();
                faction.UseCredits(engineersToAdd * 10);
            }

            tradeWithPlanetTime += 5;
        }

        ManageIdleShips();
    }

    private void ManageIdleShips() {
        friendlyStations.Clear();
        friendlyStations.AddRange(battleManager.stations.Where(s => !faction.IsAtWarWithFaction(s.faction)));
        foreach (Ship idleShip in idleShips) {
            if (idleShip.IsIdle() && idleShip.IsCivilianShip()) {
                // int randomNumber = Random.Range(0, 100);
                // if (friendlyStations.Count > 0 && idleShip.dockedStation != null && randomNumber > 20 ||
                //     idleShip.dockedStation == null && randomNumber > 80) {
                //     idleShip.shipAI.AddUnitAICommand(
                //         Command.CreateDockCommand(friendlyStations[Random.Range(0, friendlyStations.Count)]));
                //     idleShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(7, 30f)));
                // } else {
                //     if (idleShip.dockedStation != null) {
                //         idleShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(idleShip.GetPosition() +
                //             Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0, 360),
                //                 Random.Range(6000, 12000))));
                //     } else {
                //         idleShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(idleShip.GetPosition() +
                //             Calculator.GetPositionOutOfAngleAndDistance(idleShip.rotation + Random.Range(-120, 120),
                //                 Random.Range(1000, 5000))));
                //     }
                //     idleShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(1, 3f)));
                // }
                idleShip.shipAI.AddUnitAICommand(Command.CreateTradeTransportCommand());
            } else if (idleShip.IsIdle() && idleShip.IsTransportShip()) {
                idleShip.shipAI.AddUnitAICommand(Command.CreateTradeTransportCommand());
            }
        }
    }
}
