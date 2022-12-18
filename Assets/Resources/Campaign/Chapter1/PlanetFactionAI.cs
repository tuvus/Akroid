using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using static FactionCommManager;
using static FactionCommManager.CommunicationEvent;

public class PlanetFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    Planet planet;
    Station tradeStation;
    Shipyard shipyard;

    float updateTime;
    float metalOrder;
    float timeUntilNextCommunication;
    State planetFactionState;
    enum State {
        Begining,
        AskedForMetal,
        RejectedMetal,
        RecievingMetal,
        AttackingPlayer,
    }
    public void SetupPlanetFactionAI(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, Planet planet, Station tradeStation, Shipyard shipyard) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.planet = planet;
        this.tradeStation = tradeStation;
        this.shipyard = shipyard;
        planetFactionState = State.Begining;
        timeUntilNextCommunication = Random.Range(30, 60);
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
        timeUntilNextCommunication -= deltaTime;
        if (timeUntilNextCommunication <= 0) {
            if (planetFactionState == State.Begining) {
                planetFactionState = State.AskedForMetal;
                faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, new CommunicationEvent(
                "War has broken out on our planet! We require your metal in order to survive the war. Prices are high because of the war so you should be eagar to sell your metal.",
                new CommunicationEventOption[] {
                    new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; }, (communicationEvent) => {
                        if (!communicationEvent.isActive)
                            return false;
                        communicationEvent.isActive = false;
                        print("1");
                        planetFactionState = State.RecievingMetal;
                        return true; }),
                    new CommunicationEventOption("Ignore", (communicationEvent) => { return true; }, (communicationEvent) => {
                        if (!communicationEvent.isActive)
                            return false;
                        communicationEvent.isActive = false;
                        planetFactionState = State.RejectedMetal;
                        return true; })
                }, true));
                timeUntilNextCommunication = Random.Range(800, 2000);
            } else if (planetFactionState == State.RejectedMetal) {
                planetFactionState = State.AskedForMetal;
                faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, new CommunicationEvent(
                "The war has devestated the planet. We need your metal in order to rebuild. Sadly we can't pay you much.",
                new CommunicationEventOption[] {
                    new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; }, (communicationEvent) => {
                        if (!communicationEvent.isActive)
                            return false;
                        communicationEvent.isActive = false;
                        planetFactionState = State.RecievingMetal;
                        return true; }),
                    new CommunicationEventOption("Ignore", (communicationEvent) => { return true; }, (communicationEvent) => {
                        if (!communicationEvent.isActive)
                            return false;
                        faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "Since you won't give us your metal, we will have to take it from you by force.");
                        Ship ship1 = tradeStation.BuildShip(Ship.ShipClass.Lancer,8000,false);
                        Ship ship2 = tradeStation.BuildShip(Ship.ShipClass.Lancer,8000,false);
                        ship1.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Wait,Random.Range(100,200)));
                        ship1.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.AttackMove,chapter1.playerMiningStation.GetPosition()));
                        ship1.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock,tradeStation));
                        ship2.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Wait,Random.Range(100,200)));
                        ship2.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.AttackMove,chapter1.playerMiningStation.GetPosition()));
                        ship2.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock,tradeStation));
                        communicationEvent.isActive = false;
                        faction.AddEnemyFaction(chapter1.playerFaction);
                        planetFactionState = State.AttackingPlayer;
                        return true; })
                }, true));
                timeUntilNextCommunication = Random.Range(400, 800);
            } else if (planetFactionState == State.RecievingMetal) {
                faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "Thank you so much for trading your metal with us.");
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
            faction.GetFactionCommManager().SendCommunication(faction, "We need " + metal + " metal. Please give it to us!");
            //this.faction.AddCredits((long)(metal * chapter1.GetMetalCost()));
            return true;
        }
        return false;
    }
}
