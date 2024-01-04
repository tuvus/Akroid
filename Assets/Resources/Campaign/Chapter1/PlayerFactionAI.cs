using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CommunicationEvent;

public class PlayerFactionAI : FactionAI {
    public enum AIState {
        Docked,
        Tutorial,
        Background,
        Deploying,
        SettingUp,
        Normal,
    }
    Chapter1 chapter1;
    FactionCommManager commManager;
    MiningStation playerMiningStation;
    List<Station> tradeRoutes;
    int nextStationToSendTo;
    public AIState state { get; private set; }
    private bool nextState;

    public void SetupPlayerFactionAI(Chapter1 chapter1, MiningStation playerMiningStation) {
        this.chapter1 = chapter1;
        this.playerMiningStation = playerMiningStation;
        tradeRoutes = new List<Station>();
        nextStationToSendTo = 0;
        SetState(AIState.Docked);
        commManager = faction.GetFactionCommManager();
    }

    public override void UpdateFactionAI(float deltaTime) {
        UpdateFactionState();
        ManageIdleShips();
    }

    void UpdateFactionState() {
        if (nextState && state == AIState.SettingUp && !playerMiningStation.IsBuilt()) return;
        if (!nextState) return;
        nextState = false;
        if (state == AIState.Deploying) {
            // TODO: Put message about time controls here so the player can read at their own pace.
            commManager.SendCommunication(chapter1.planetFactionAI.faction, "Thanks for the goodbye! We will send you some resources soon.", 5);
            commManager.SendCommunication(faction,
            "We have started heading for the new mining site. \n If I am talking too fast for you press the \"?\" key to pause and un-pause the game. The [<, >] keys can also change how quickly the game time passes.", (communicationEvent) => {
                commManager.SendCommunication(faction,
                "Right click and scroll to move the camera. Our ships will appear with a green icon, meaning that we own them but can't control them. Neutral units will appear grey and hostile units will appear red.", (communicationEvent) => {
                    commManager.SendCommunication(faction,
                    "Now left click on ships and drag the mouse to select one or multiple ships. Our ships are in a fleet, which means if you select one, by default you will select all. Hold alt to select just one ship in a fleet. You can also press B to follow and unfollow a ship.", (communicationEvent) => {
                        commManager.SendCommunication(faction,
                        "Right click on a ship or station to view its stats, cargo or construction bay. Right click again to close the panel. Try right clicking on each of your ships.", (communicationEvent) => {
                            commManager.SendCommunication(faction,
                            "This solar system has more stations than just the shipyard we just left. Zoom out and right click on all of the stations to view their unique menus.", (communicationEvent) => {
                                commManager.SendCommunication(faction,
                                "Now press G to toggle all of the unit icons while zoomed out. You can barely see the stations, planet and many asteroid fields. We are currently heading to a particularly dense asteroid field to mine.", (communicationEvent) => {
                                    commManager.SendCommunication(new CommunicationEvent(faction.GetFactionCommManager(),
                                    "What difficulty would you like to play at? Harder difficulties will have a faster intro scene."
                                    , new CommunicationEventOption[] {
                                        new CommunicationEventOption("Easy", (communicationEvent) => { return true; }, (communicationEvent) => {
                                            if (!communicationEvent.isActive)
                                                return false;
                                            communicationEvent.DeactivateEvent();
                                            chapter1.SetDifficultyLevel(Chapter1.DifficultyLevel.Easy);
                                            SetState(AIState.Background);
                                            return true; }),
                                        new CommunicationEventOption("Normal", (communicationEvent) => { return true; }, (communicationEvent) => {
                                            if (!communicationEvent.isActive)
                                                return false;
                                            communicationEvent.DeactivateEvent();
                                            chapter1.SetDifficultyLevel(Chapter1.DifficultyLevel.Normal);
                                            SetState(AIState.Background);
                                            return true; }),
                                        new CommunicationEventOption("Hard", (communicationEvent) => { return true; }, (communicationEvent) => {
                                            if (!communicationEvent.isActive)
                                                return false;
                                            communicationEvent.DeactivateEvent();
                                            chapter1.SetDifficultyLevel(Chapter1.DifficultyLevel.Hard);
                                            SetState(AIState.Background);
                                            return true; })
                                    }, true), 30 * GetTimeScale());
                                }, 30 * GetTimeScale());
                            }, 35 * GetTimeScale());
                         }, 35 * GetTimeScale());
                     }, 25 * GetTimeScale());
                }, 25 * GetTimeScale());
             }, 25 * GetTimeScale());
        } else if (state == AIState.Background) {
            chapter1.GetBattleManager().SetSimulationTimeScale(faction.ships[0].fleet.FleetAI.GetTimeUntilFinishedWithCommand() / (120 + 40));
            commManager.SendCommunication(faction,
            "See if you can locate and zoom in on the planet with the station, this is our home. \n Due to the slow development of resource reusing policy and climate change, resources are getting sparse, which is building tension between the major nations. Luckily our space instillations are independent of any individual nation so there shouldn't be any space wars out here.", (communicationEvent) => {
                commManager.SendCommunication(faction,
                "Overpopulation has ignited an effort to colonize other planets in the system. That is, however a long way off, to start we have been developing the first moon colony. We'll see how well it works out.", (communicationEvent) => {
                    commManager.SendCommunication(faction,
                    "Space technology is relatively new and we are starting to harvest asteroid fields to help solve our resource problems back at " + GetPlanetName() + ".", (communicationEvent) => {
                        commManager.SendCommunication(faction,
                        "We haven't figured out how to travel to other solar systems yet, it might take a hundred years or so until it is possible. Our advanced space research station far out in the solar system is working on this.", (communicationEvent) => {
                            commManager.SendCommunication(faction,
                            "There was a big boom in civilian space travel once a general purpose space ship came into production in our first designated shipyard.", (communicationEvent) => {
                                commManager.SendCommunication(faction,
                                "AI technology is also on the rise and may aid space exploration, but like all things, recent research has been limited by the resource crisis.", (communicationEvent) => {
                                    commManager.SendCommunication(chapter1.planetFactionAI.faction,
                                    "We are about to arrive at our destination!", (communicationEvent) => {
                                        if (!playerMiningStation.IsBuilt()) {
                                            commManager.SendCommunication(faction,
                                            "Now we have nothing to do but wait until we reach the mining site. \n Remember that you can press the [<, >, ?] keys to change how quickly the game time passes. \n In the mean time feel free to click the \"Controls help\" button and read the controls.", (communicationEvent) => {
                                                SetState(AIState.SettingUp);
                                            });
                                        } else {
                                            SetState(AIState.SettingUp);
                                        }
                                    }, 15 * GetTimeScale());
                                }, 15 * GetTimeScale());
                            }, 15 * GetTimeScale());
                        }, 15 * GetTimeScale());
                    }, 20 * GetTimeScale());
                }, 35 * GetTimeScale());
            }, 5 * GetTimeScale());
        } else if (state == AIState.SettingUp) {
            chapter1.GetBattleManager().SetSimulationTimeScale(10);
            commManager.SendCommunication(new CommunicationEvent(chapter1.planetFactionAI.faction.GetFactionCommManager(), "We have arrived safely at the destination and are setting up our operations.",
            new CommunicationEventOption[] { new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; },
                (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    SetState(AIState.Normal);
                    AddTradeRouteToStation(chapter1.tradeStation);
                    communicationEvent.DeactivateEvent();
                    chapter1.shipyardFaction.GetFactionCommManager().SendCommunication(faction, "Good to see that you are set up and everything is going well. We are setting up a trade route for you. We will give you resources to operate your station in return for metal.", 3 * GetTimeScale());
                    return true;
                }) 
            }, true), 5 * GetTimeScale());
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

    public void SetState(AIState newState) {
        state = newState;
        nextState = true;
    }

    string GetPlanetName() {
        return "Earth";
    }
}