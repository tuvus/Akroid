using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FactionCommManager;
using static FactionCommManager.CommunicationEvent;

public class PlayerFactionAI : FactionAI {
    public enum AIState {
        Docked,
        Tutorial,
        Deploying,
        SettingUp,
        Normal,
    }
    Chapter1 chapter1;
    FactionCommManager commManager;
    MiningStation playerMiningStation;
    List<Station> tradeRoutes;
    int nextStationToSendTo;
    public AIState state;

    public void SetupPlayerFactionAI(Chapter1 chapter1, MiningStation playerMiningStation) {
        this.chapter1 = chapter1;
        this.playerMiningStation = playerMiningStation;
        tradeRoutes = new List<Station>();
        nextStationToSendTo = 0;
        state = AIState.Docked;
        commManager = faction.GetFactionCommManager();
    }

    public override void UpdateFactionAI(float deltaTime) {
        UpdateFactionState();
        ManageIdleShips();
    }

    void UpdateFactionState() {
        if (state == AIState.Deploying) {
            commManager.SendCommunication(faction,
                "We have started heading for the designated location. \n Right click and scroll to move the camera. Our ships will appear with a green icon, meaning that we own them but can't control them. Neutral units will appear white and hostile units will appear red.", (communicationEvent) => {
                    commManager.SendCommunication(faction,
                        "Now left click on ships and drag the mouse to select one or multiple ships. Our ships are in a fleet, which means if you select one, by default you will select all. Hold alt to select just one ship in a fleet. You can also press B to follow and unfollow a ship.", (communicationEvent) => {
                            commManager.SendCommunication(faction,
                                "Right click on a ship or station to view its stats, cargo or construction bay. Right click again to close the panel. Try right clicking on each of your ships.", (communicationEvent) => {
                                    commManager.SendCommunication(faction,
                                    "This solar system has more stations than just the shipyard we just left. Zoom out and right click on all of the stations to ", (communicationEvent) => {
                                        commManager.SendCommunication(faction,
                                        "Now press G to toggle all of the unit icons while zoomed out. You can barely see the stations and a planet and many asteroid fields to mine from. We are currently heading to a particularly dense cluster.", (communicationEvent) => {
                                            commManager.SendCommunication(faction,
                                            "See if you can locate and zoom in on the planet with the station, that is where we come from. Resources are getting spars and tension is building between the major nations. Luckily our space instillations are independent of any individual nation.", (communicationEvent) => { 
                                            }, 30 * GetTimeScale());
                                        }, 30 * GetTimeScale());
                                    }, 30 * GetTimeScale());
                                }, 30 * GetTimeScale());
                        }, 30 * GetTimeScale());
                }, 15 * GetTimeScale());
            state = AIState.Tutorial;
        } else if (state == AIState.SettingUp && playerMiningStation.IsBuilt()) {
            state = AIState.Normal;
            commManager.SendCommunication(new CommunicationEvent(chapter1.shipyardFaction, "We have arrived safely at the destination and are setting up our operations.",
                new CommunicationEventOption[] { new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; },
                (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    AddTradeRouteToStation(chapter1.shipyard);
                    communicationEvent.DeactivateEvent();
                    chapter1.shipyardFaction.GetFactionCommManager().SendCommunication(faction, "Good to see that you are set up and everything is going well. We are setting up a trade route for you. We will give you resources to operate your station in return for metal.", 3 * GetTimeScale());
                    return true;
                }) }, true), 2 * GetTimeScale());
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