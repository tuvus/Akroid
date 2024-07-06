using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static CommunicationEvent;

public class PlayerFactionAI : FactionAI {

    Chapter1 chapter1;
    FactionCommManager commManager;
    MiningStation playerMiningStation;
    List<Station> tradeRoutes;
    int nextStationToSendTo;
    private bool nextState;

    public void SetupPlayerFactionAI(BattleManager battleManager, Faction faction, Chapter1 chapter1, MiningStation playerMiningStation) {
        base.SetupFactionAI(battleManager, faction);
        this.chapter1 = chapter1;
        this.playerMiningStation = playerMiningStation;
        tradeRoutes = new List<Station>();
        nextStationToSendTo = 0;
        commManager = faction.GetFactionCommManager();
        chapter1.planetFactionAI.faction.GetFactionCommManager().SendCommunication(new CommunicationEvent(chapter1.playerFaction.GetFactionCommManager(),
            "Undocking procedure successful! \n You are now on route to the designated mining location. " +
            "As we planned, you will construct the mining station at the designated point (" +
            Mathf.RoundToInt(chapter1.playerMiningStation.GetPosition().x) + ", " + Mathf.RoundToInt(chapter1.playerMiningStation.GetPosition().y) + ") and begin operations.\nGood luck!",
            (communicationEvent) => { AddTutorial1(); }), 10 * GetTimeScale());
    }

    public override void UpdateFactionAI(float deltaTime) {
        UpdateFactionState();
        ManageIdleShips();
    }

    void UpdateFactionState() { }

    private void AddTutorial1() {
        commManager.SendCommunication(chapter1.planetFactionAI.faction, "Thanks for the goodbye! We will send you some resources soon.", 5);
        EventChainBuilder eventChain = new EventChainBuilder();
        eventChain.AddCommEvent(commManager, faction,
            "We have started heading for the new mining site. \n" +
            "If I am talking too fast for you press the \"?\" key to pause and un-pause the game. " +
            "The [<, >] keys can also change how quickly the game time passes.", 25 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Right click and move the mouse to pan the camera. Scroll out to see more of the solar system. \n" +
            "Our ships will appear with a green icon, meaning that we own them but can't control them. " +
            "Neutral units will appear grey and hostile units will appear red.", 35 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Now scroll back in to our ships and left click on a ship or drag the mouse to select multiple ships. \n" +
            "Our ships are in a fleet, which means if you select one you will select all by default. \n" +
            "You can see a line coming out of the selected ship, this is where they are going. \n" +
            "Hold alt to select just one ship in a fleet.", 35 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Try right clicking on a ship or station to view its stats, cargo or construction bay." +
            "Right click again to close the panel. Try right clicking on each of your ships.", 40 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Press B to unfollow the ship the ship in our fleet. \n" +
            "This solar system has more stations than just the trade station we just left. " +
            "Zoom out and right click on all of the stations to view their unique menus.", 35 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Now press G to toggle all of the unit icons while zoomed out. " +
            "You can barely see the stations, planet, the many asteroid fields and gas clouds. " +
            "Our minning team is currently heading to a particularly dense asteroid field to mine.", 30 * GetTimeScale());
        eventChain.Build(chapter1.eventManager, commManager, faction,
            "What difficulty would you like to play at? Harder difficulties will have a faster intro scene.",
            new CommunicationEventOption[] {
                new CommunicationEventOption("Easy", (communicationEvent) => { return true; }, (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    chapter1.SetDifficultyLevel(Chapter1.DifficultyLevel.Easy);
                    AddTutorial2();
                    return true; }),
                new CommunicationEventOption("Normal", (communicationEvent) => { return true; }, (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    chapter1.SetDifficultyLevel(Chapter1.DifficultyLevel.Normal);
                    AddTutorial2();
                    return true; }),
                new CommunicationEventOption("Hard", (communicationEvent) => { return true; }, (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    chapter1.SetDifficultyLevel(Chapter1.DifficultyLevel.Hard);
                    AddTutorial2();
                    return true; })
        }, 30 * GetTimeScale())(); // Don't forget the last set of parenthesis to call the function.
    }

    void AddTutorial2() {
        chapter1.GetBattleManager().SetSimulationTimeScale(faction.fleets.First().FleetAI.GetTimeUntilFinishedWithCommand() / (120 + 40));
        EventChainBuilder eventChainBuilder = new EventChainBuilder();
        eventChainBuilder.AddCommEvent(commManager, faction,
        "See if you can locate and zoom in on the planet with the station, this is our home. \n " +
        "Due to the slow development of resource reusing policy and climate change, resources are getting sparse, which is building tension between the major nations. " +
        "Luckily our space instillations are independent of any individual nation so there shouldn't be any space wars out here.", 5 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "Overpopulation has ignited an effort to colonize other planets in the system. " +
            "That is, however a long way off, to start we have been developing the first moon colony. " +
            "We'll see how well it works out.", 35 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "Space technology is relatively new and we are starting to harvest asteroid fields to help solve our resource problems back at " + GetPlanetName() + ".", 20 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "We haven't figured out how to travel to other solar systems yet, it might take a hundred years or so until it is possible. " +
            "Our advanced space research station far out in the solar system is working on this.", 15 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "There was a big boom in civilian space travel once a general purpose space ship came into production in our first designated shipyard.", 15 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "AI technology is also on the rise and may aid space exploration, but like all things, recent research has been limited by the resource crisis.", 15 * GetTimeScale());
        eventChainBuilder.Build(chapter1.eventManager, () => {
            commManager.SendCommunication(chapter1.planetFactionAI.faction, "We are about to arrive at our destination!");
            commManager.SendCommunication(faction,
                "Now we have nothing to do but wait until we reach the mining site. \n " +
                "Remember that you can press the [<, >, ?] keys to change how quickly the game time passes. \n " +
                "In the mean time feel free to click the \"Controls help\" button in the top right and read the controls.", 15 * GetTimeScale());

            chapter1.eventManager.AddEvent(EventCondition.PredicateEvent((_) => playerMiningStation.IsBuilt()), () => {
                AddStationTutorial();
            });
        })();
    }

    void AddStationTutorial() {
        chapter1.GetBattleManager().SetSimulationTimeScale(10);
        if (LocalPlayer.Instance.GetFaction() == faction) {
            LocalPlayer.Instance.AddOwnedUnit(faction.ships.First(s => s.IsCivilianShip()));
        }
        commManager.SendCommunication(new CommunicationEvent(chapter1.planetFactionAI.faction.GetFactionCommManager(), "We have arrived safely at the destination and are setting up our operations.",
        new CommunicationEventOption[] { new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; },
                (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    AddTradeRouteToStation(chapter1.tradeStation);
                    communicationEvent.DeactivateEvent();
                    chapter1.shipyardFaction.GetFactionCommManager().SendCommunication(faction, "Good to see that you are set up and everything is going well. We are setting up a trade route for you. We will give you resources to operate your station in return for metal.", 3 * GetTimeScale());
                    return true;
                })
        }, true), 5 * GetTimeScale());
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
        if (playerMiningStation.GetMiningStationAI().GetWantedTransportShips() > faction.GetShipsOfType(Ship.ShipType.Transport) + chapter1.shipyardFactionAI.GetOrderCount(Ship.ShipClass.Transport, faction)) {
            return true;
        } else {
            return false;
        }
    }

    public void AddTradeRouteToStation(Station station) {
        tradeRoutes.Add(station);
    }

    string GetPlanetName() {
        return "Earth";
    }
}