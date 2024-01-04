using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using static PlayerFactionAI;

public class PlanetFactionAI : FactionAI {
    Chapter1 chapter1;
    ShipyardFactionAI shipyardFactionAI;
    Planet planet;
    Station tradeStation;
    Shipyard shipyard;
    List<Ship> civilianShips;
    List<Station> friendlyStations;

    float updateTime;
    long metalOrder;
    float timeUntilNextCommunication;
    State planetFactionState;
    enum State {
        Beginning,
        AskedForMetal,
        RejectedMetal,
        ReceivingMetal,
        AttackingPlayer,
    }
    public void SetupPlanetFactionAI(Chapter1 chapter1, ShipyardFactionAI shipyardFactionAI, Planet planet, Station tradeStation, Shipyard shipyard, List<Ship> civilianShips) {
        this.chapter1 = chapter1;
        this.shipyardFactionAI = shipyardFactionAI;
        this.planet = planet;
        this.tradeStation = tradeStation;
        this.shipyard = shipyard;
        this.civilianShips = civilianShips;
        planetFactionState = State.Beginning;
        timeUntilNextCommunication = Random.Range(30, 60);
        friendlyStations = new List<Station>();
        faction.GetFactionCommManager().SendCommunication(new CommunicationEvent(chapter1.playerFaction,
            "Undocking procedure successful! \n You are now on route to the designated mining location. As we planned, you will construct the mining station at the designated point (" +
            Mathf.RoundToInt(chapter1.playerMiningStation.GetPosition().x) + ", " + Mathf.RoundToInt(chapter1.playerMiningStation.GetPosition().y) + ") and begin operations.\nGood luck!",
            (communicationEvent) => { chapter1.playerFactionAI.SetState(AIState.Deploying); }), 10 * GetTimeScale());
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
        //if (chapter1.playerMiningStation.IsBuilt())
        //    timeUntilNextCommunication -= deltaTime;
        //if (timeUntilNextCommunication <= 0) {
        //    if (planetFactionState == State.Beginning) {
        //        planetFactionState = State.AskedForMetal;
        //        faction.GetFactionCommManager().SendCommunication(new CommunicationEvent(chapter1.playerFaction,
        //        "War has broken out on our planet! We require your metal in order to survive the war. Prices are high because of the war so you should be eager to sell your metal.",
        //        new CommunicationEventOption[] {
        //            new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; }, (communicationEvent) => {
        //                if (!communicationEvent.isActive)
        //                    return false;
        //                communicationEvent.DeactivateEvent();
        //                chapter1.playerFactionAI.AddTradeRouteToStation(tradeStation);
        //                planetFactionState = State.ReceivingMetal;
        //                return true; }),
        //            new CommunicationEventOption("Ignore", (communicationEvent) => { return true; }, (communicationEvent) => {
        //                if (!communicationEvent.isActive)
        //                    return false;
        //                communicationEvent.DeactivateEvent();
        //                planetFactionState = State.RejectedMetal;
        //                chapter1.ChangeMetalCost(.6f);
        //                return true; })
        //        }, true), 5 * GetTimeScale());
        //        timeUntilNextCommunication = Random.Range(800, 2000);
        //    } else if (planetFactionState == State.RejectedMetal) {
        //        planetFactionState = State.AskedForMetal;
        //        faction.GetFactionCommManager().SendCommunication(new CommunicationEvent(chapter1.playerFaction,
        //            "The war has devastated the planet. We need your metal in order to rebuild. Sadly we can't pay you much.",
        //            new CommunicationEventOption[] {
        //                new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; }, (communicationEvent) => {
        //                    if (!communicationEvent.isActive)
        //                        return false;
        //                communicationEvent.DeactivateEvent();
        //                    chapter1.playerFactionAI.AddTradeRouteToStation(tradeStation);
        //                    planetFactionState = State.ReceivingMetal;
        //                    return true; }),
        //                new CommunicationEventOption("Ignore", (communicationEvent) => { return true; }, (communicationEvent) => {
        //                    if (!communicationEvent.isActive)
        //                        return false;
        //                    faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "Since you won't give us your metal, we will have to take it from you by force.", 2 * GetTimeScale());
        //                    faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "We declare war on you!.", 10 * GetTimeScale());
        //                    Ship ship1 = tradeStation.BuildShip(Ship.ShipClass.Lancer);
        //                    Ship ship2 = tradeStation.BuildShip(Ship.ShipClass.Lancer);
        //                    ship1.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(200,400)));
        //                    ship1.shipAI.AddUnitAICommand(Command.CreateAttackMoveCommand(chapter1.playerMiningStation.GetPosition()));
        //                    ship1.shipAI.AddUnitAICommand(Command.CreateDockCommand(tradeStation));
        //                    ship2.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(200,400)));
        //                    ship2.shipAI.AddUnitAICommand(Command.CreateAttackMoveCommand(chapter1.playerMiningStation.GetPosition()));
        //                    ship2.shipAI.AddUnitAICommand(Command.CreateDockCommand(tradeStation));
        //                    communicationEvent.DeactivateEvent();
        //                    faction.AddEnemyFaction(chapter1.playerFaction);
        //                    chapter1.playerFaction.AddEnemyFaction(faction);
        //                    planetFactionState = State.AttackingPlayer;
        //                    return true; })
        //            }, true), 3 * GetTimeScale());
        //        timeUntilNextCommunication = Random.Range(400, 800);
        //    } else if (planetFactionState == State.ReceivingMetal) {
        //        faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "Thank you so much for trading your metal with us.");
        //        timeUntilNextCommunication = Random.Range(4000, 8000);
        //        chapter1.ChangeMetalCost(.98f);
        //    }
        //}
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
        friendlyStations.Clear();
        for (int i = 0; i < BattleManager.Instance.stations.Count; i++) {
            if (!faction.IsAtWarWithFaction(BattleManager.Instance.stations[i].faction)) friendlyStations.Add(BattleManager.Instance.stations[i]);
        }
        for (int i = 0; i < idleShips.Count; i++) {
            Ship idleShip = idleShips[i];
            if (idleShip.IsIdle()) {
                if (idleShip.IsTransportShip()) {
                    idleShip.shipAI.AddUnitAICommand(Command.CreateTransportCommand(tradeStation, shipyard), Command.CommandAction.AddToEnd);
                } else if (idleShip.IsCivilianShip()) {
                    int randomNumber = Random.Range(0, 100);
                    if (friendlyStations.Count > 0 && (idleShip.dockedStation != null && randomNumber > 30) || (idleShip.dockedStation == null && randomNumber > 80)) {
                        idleShip.shipAI.AddUnitAICommand(Command.CreateDockCommand(friendlyStations[Random.Range(0, friendlyStations.Count)]));
                        idleShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(2, 10f)));
                    } else {
                        if (idleShip.dockedStation != null)
                            idleShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(idleShip.GetPosition() + Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0, 360), Random.Range(3000, 12000))));
                        else
                            idleShip.shipAI.AddUnitAICommand(Command.CreateMoveCommand(idleShip.GetPosition() + Calculator.GetPositionOutOfAngleAndDistance(idleShip.GetRotation() + Random.Range(-120, 120), Random.Range(1000, 5000))));
                        idleShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(0, 3f)));
                    }
                }
            }
        }
    }

    public bool AddMetalOrder(Faction faction, long metal) {
        if (faction.UseCredits((long)(metal * chapter1.GetMetalCost()))) {
            metalOrder += metal;
            faction.GetFactionCommManager().SendCommunication(this.faction, "We need " + metal + " metal. Please give it to us!");
            //this.faction.AddCredits((long)(metal * chapter1.GetMetalCost()));
            return true;
        }
        return false;
    }
}
