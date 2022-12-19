using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FactionCommManager;

public class ShipyardFactionAI : FactionAI {
    Chapter1 chapter1;
    PlanetFactionAI planetFactionAI;
    Shipyard shipyard;
    CommunicationEvent lastQuestion;
    float timeUntilNextCommunication;

    public void SetupShipyardFactionAI(Chapter1 chapter1, PlanetFactionAI planetFactionAI, Shipyard shipyard) {
        this.chapter1 = chapter1;
        this.planetFactionAI = planetFactionAI;
        this.shipyard = shipyard;
    }

    public override void UpdateFactionAI(float deltaTime) {
        if (shipyard.GetHanger().GetCombatShip(0) != null) {
            if (shipyard.GetHanger().GetCombatShip(0).faction.stations.Count > 0) {
                shipyard.GetHanger().GetCombatShip(0).shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, shipyard.GetHanger().GetCombatShip(0).faction.stations[0]), ShipAI.CommandAction.AddToEnd);
            }
        }
        timeUntilNextCommunication -= deltaTime;
        if (timeUntilNextCommunication <= 0) {
            if (chapter1.playerFactionAI.WantMoreTransportShips()) {
                float cost = GetTransportCost();
                if (chapter1.playerFaction.credits > cost * 1.2f) {
                    if (lastQuestion != null)
                        lastQuestion.DeactivateEvent();
                    lastQuestion = new CommunicationEvent(
                        "We see that you have enough money to purchase a transport for your operations. It will cost you around " + cost +
                        " credits to purchase. Would you like a deal?",
                        new CommunicationEvent.CommunicationEventOption[] {
                            new CommunicationEvent.CommunicationEventOption("Buy Transport", (communicationEvent) => {
                                return chapter1.playerFaction.credits > cost; }, (communicationEvent) => {
                                if (!communicationEvent.isActive || !communicationEvent.options[0].checkStatus(communicationEvent))
                                    return false;
                                PlaceTransportOrder(chapter1.playerFaction);
                                faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "We have recieved your order and have begun construction of the transport.");
                                return true;
                                }
                            )
                        }, true);
                    faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, lastQuestion);
                }
            } else {
                float cost = GetLancerCost();
                if (chapter1.playerFaction.credits > cost * 1.2f) {
                    if (lastQuestion != null)
                        lastQuestion.DeactivateEvent();
                    lastQuestion = new CommunicationEvent(
                        "We see that you have enough money to purchase a lancer class cruiser from us. It will cost you around " + cost +
                        " credits to purchase. Would you like a deal?",
                        new CommunicationEvent.CommunicationEventOption[] {
                            new CommunicationEvent.CommunicationEventOption("Buy Lancer", (communicationEvent) => {
                                return chapter1.playerFaction.credits > cost; }, (communicationEvent) => {
                                if (!communicationEvent.isActive || !communicationEvent.options[0].checkStatus(communicationEvent))
                                    return false;
                                PlaceCombatOrder(chapter1.playerFaction);
                                faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "We have recieved your order and have begun construction of the lancer class cruiser.");
                                return true;
                                }
                            )
                        }, true);
                    faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, lastQuestion);
                }
            }
            timeUntilNextCommunication += Random.Range(1200, 5000);
        }
    }

    public void PlaceCombatOrder(Faction faction) {
        long cost = GetLancerCost();
        if (faction.UseCredits(cost)) {
            this.faction.AddCredits(cost);
            if (planetFactionAI.AddMetalOrder(this.faction, 9800)) {
                shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Lancer, "Lancer", 12000,
                    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 9800 }));
            }
        }
    }

    public void PlaceTransportOrder(Faction faction) {
        long cost = GetTransportCost();
        if (faction.UseCredits(cost)) {
            this.faction.AddCredits(cost);
            if (planetFactionAI.AddMetalOrder(this.faction, 4800)) {
                shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Transport, "Transport", 7000,
                    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 4800 }));
            }
        }
    }

    long GetTransportCost() {
        return (long)(4800 * chapter1.GetMetalCost()) + 10000;
    }

    long GetLancerCost() {
        return (long)(9800 * chapter1.GetMetalCost()) + 20000;
    }

    public int GetOrderCount(Ship.ShipClass shipClass, int factionIndex) {
        return shipyard.GetConstructionBay().GetNumberOfShipsOfClassFaction(shipClass, factionIndex);
    }
}