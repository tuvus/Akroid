using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FactionCommManager;
using static FactionCommManager.CommunicationEvent;

public class PlayerFactionAI : FactionAI {
    enum AIState {
        SettingUp,
        Normal,
    }
    Chapter1 chapter1;
    MiningStation playerMiningStation;
    List<Station> tradeRoutes;
    int nextStationToSendTo;
    AIState state;

    public void SetupPlayerFactionAI(Chapter1 chapter1, MiningStation playerMiningStation) {
        this.chapter1 = chapter1;
        this.playerMiningStation = playerMiningStation;
        tradeRoutes = new List<Station>();
        nextStationToSendTo = 0;
        state = AIState.SettingUp;
    }

    public override void UpdateFactionAI(float deltaTime) {
        UpdateFactionState();
        ManageIdleShips();
    }

    void UpdateFactionState() {
        if (state == AIState.SettingUp && playerMiningStation.IsBuilt()) {
            state = AIState.Normal;
            faction.GetFactionCommManager().SendCommunication(chapter1.shipyardFaction, new CommunicationEvent("We have arrived safetly at the destination and are setting up our opperations.",
                new CommunicationEventOption[] { new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; }, 
                (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    AddTradeRouteToStation(chapter1.shipyard); 
                    communicationEvent.DeactivateEvent();
                    chapter1.shipyardFaction.GetFactionCommManager().SendCommunication(faction, "Good to see that you are set up and everything is going well. We are setting up a trade route for you. We will give you resources to operate your station in return for metal.");
                    return true; 
                }) }, true));
        }
    }

    void ManageIdleShips() {
        for (int i = 0; i < idleShips.Count; i++) {
            if (idleShips[i].IsIdle()) {
                if (idleShips[i].IsTransportShip()) {
                    if (tradeRoutes.Count > 0) {
                        nextStationToSendTo++;
                        if (nextStationToSendTo >= tradeRoutes.Count)
                            nextStationToSendTo = 0;
                        idleShips[i].shipAI.AddUnitAICommand(Command.CreateTransportCommand(playerMiningStation, tradeRoutes[nextStationToSendTo], true), Command.CommandAction.AddToEnd);
                    }
                }
            }
        }
    }

    public override void OnShipBuilt(Ship ship) {
        if (ship.IsCombatShip()) {
            LocalPlayer.Instance.AddOwnedUnit(ship);
            ship.GetUnitSelection().UpdateFactionColor();
        }
        ship.shipAI.AddUnitAICommand(Command.CreateDockCommand(playerMiningStation));
    }

    public bool WantMoreTransportShips() {
        if (playerMiningStation.GetMiningStationAI().GetWantedTransportShips() > faction.GetShipsOfType(Ship.ShipType.Transport) + chapter1.shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction.factionIndex)) {
            return true;
        } else {
            return false;
        }
    }

    public void AddTradeRouteToStation(Station station) {
        tradeRoutes.Add(station);
    }
}