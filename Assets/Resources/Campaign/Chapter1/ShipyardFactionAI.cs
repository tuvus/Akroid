using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FactionCommManager;

public class ShipyardFactionAI : FactionAI {
    Chapter1 chapter1;
    PlanetFactionAI planetFactionAI;
    Shipyard shipyard;
    float timeUntilNextCommunication;
    long transportMetalCost = 4800;
    long transportCreditCost = 10000;
    long lancerMetalCost = 9800;
    long lancerCreditCost = 20000;

    public void SetupShipyardFactionAI(Chapter1 chapter1, PlanetFactionAI planetFactionAI, Shipyard shipyard) {
        this.chapter1 = chapter1;
        this.planetFactionAI = planetFactionAI;
        this.shipyard = shipyard;
        faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "Undocking procedure succesfull! \n You are now on route to the designated minning location. As we planned, you will construct the minning station at the designated point (" +
            Mathf.RoundToInt(chapter1.playerMiningStation.GetPosition().x) + ", " + Mathf.RoundToInt(chapter1.playerMiningStation.GetPosition().y) + ") and begin opperations.\nGood luck!");
    }

    public override void UpdateFactionAI(float deltaTime) {
        if (shipyard.GetHanger().GetCombatShip(0) != null) {
            if (shipyard.GetHanger().GetCombatShip(0).faction.stations.Count > 0) {
                shipyard.GetHanger().GetCombatShip(0).shipAI.AddUnitAICommand(Command.CreateDockCommand(shipyard.GetHanger().GetCombatShip(0).faction.stations[0]), Command.CommandAction.AddToEnd);
            }
        }
        UpdateFactionCommunication(deltaTime);
    }

    void UpdateFactionCommunication(float deltaTime) {
        for (int i = 0; i < faction.GetFactionCommManager().communicationLog.Count; i++) {
            if (faction.GetFactionCommManager().communicationLog[i].isActive && faction.GetFactionCommManager().communicationLog[i].options.Length > 0)
                faction.GetFactionCommManager().communicationLog[i].ChooseOption(0);
        }
        timeUntilNextCommunication -= deltaTime;
        if (timeUntilNextCommunication <= 0) {
            if (chapter1.playerFaction.credits > Mathf.Min(GetTransportTotalCost() * 1.2f, GetLancerTotalCost() * 1.2f)) {
                faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "You have enough money to order a ship from us, click our shipyard to open the build menue.");
            }

            timeUntilNextCommunication += Random.Range(1200, 5000);
        }
    }

    public void PlaceCombatOrder(Faction faction) {
        long cost = GetLancerTotalCost();
        if (faction.UseCredits(cost)) {
            this.faction.AddCredits(cost);
            if (planetFactionAI.AddMetalOrder(this.faction, 9800)) {
                shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Lancer, "Lancer", 12000,
                    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<long>() { 9800 }));
            }
        }
    }

    public void PlaceTransportOrder(Faction faction) {
        long cost = GetTransportTotalCost();
        if (faction.UseCredits(cost)) {
            this.faction.AddCredits(cost);
            if (planetFactionAI.AddMetalOrder(this.faction, transportMetalCost)) {
                shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipBlueprint(faction.factionIndex, Ship.ShipClass.Transport, "Transport", transportCreditCost,
                    new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<long>() { transportMetalCost }));
            }
        }
    }

    public override void OnShipBuiltForAnotherFaction(Ship ship, Faction faction) {
        this.faction.GetFactionCommManager().SendCommunication(faction, "We have built a " + ship.GetUnitName() + " for you, it will be sent to your station.");
    }

    long GetTransportTotalCost() {
        return (long)(transportMetalCost * chapter1.GetMetalCost()) + transportCreditCost;
    }

    long GetLancerTotalCost() {
        return (long)(lancerMetalCost * chapter1.GetMetalCost()) + lancerCreditCost;
    }

    public int GetOrderCount(Ship.ShipClass shipClass, int factionIndex) {
        return shipyard.GetConstructionBay().GetNumberOfShipsOfClassFaction(shipClass, factionIndex);
    }
}