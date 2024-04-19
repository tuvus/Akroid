using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public void SetupShipyardFactionAI(BattleManager battleManager, Faction faction, Chapter1 chapter1, PlanetFactionAI planetFactionAI, Shipyard shipyard) {
        base.SetupFactionAI(battleManager, faction);
        this.chapter1 = chapter1;
        this.planetFactionAI = planetFactionAI;
        this.shipyard = shipyard;
    }

    public override void UpdateFactionAI(float deltaTime) {
        base.UpdateFactionAI(deltaTime);
        if (shipyard.GetHanger().GetCombatShip(0) != null) {
            if (shipyard.GetHanger().GetCombatShip(0).faction.stations.Count > 0) {
                shipyard.GetHanger().GetCombatShip(0).shipAI.AddUnitAICommand(Command.CreateDockCommand(shipyard.GetHanger().GetCombatShip(0).faction.stations.First()), Command.CommandAction.AddToEnd);
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
                faction.GetFactionCommManager().SendCommunication(chapter1.playerFaction, "You have enough money to order a ship from us, click our shipyard to open the build menu.");
            }

            timeUntilNextCommunication += Random.Range(1200, 5000);
        }
    }

    public void PlaceCombatOrder(Faction faction) {
        long cost = GetLancerTotalCost();
        if (faction.UseCredits(cost)) {
            this.faction.AddCredits(cost);
            if (planetFactionAI.AddMetalOrder(this.faction, 9800)) {
                shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction, battleManager.GetShipBlueprint(Ship.ShipClass.Aria)));
            }
        }
    }

    public void PlaceTransportOrder(Faction faction) {
        long cost = GetTransportTotalCost();
        if (faction.UseCredits(cost)) {
            this.faction.AddCredits(cost);
            if (planetFactionAI.AddMetalOrder(this.faction, transportMetalCost)) {
                shipyard.GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(faction, battleManager.GetShipBlueprint(Ship.ShipClass.Transport)));
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

    public int GetOrderCount(Ship.ShipClass shipClass, Faction faction) {
        return shipyard.GetConstructionBay().GetNumberOfShipsOfClassFaction(shipClass, faction);
    }
}