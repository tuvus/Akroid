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

        // Increase time to skip tutorial
        bool skipTutorial = false;
        EventChainBuilder eventChain = new EventChainBuilder();
        eventChain.AddCondition(EventCondition.WaitEvent(2f));
        eventChain.AddAction(() => { 
            if (LocalPlayer.Instance.GetLocalPlayerGameInput().AdditiveButtonPressed) {
                chapter1.GetBattleManager().SetSimulationTimeScale(10);
                skipTutorial = true;
            }
        });
        eventChain.Build(chapter1.eventManager)();

        chapter1.planetFactionAI.faction.GetFactionCommManager().SendCommunication(new CommunicationEvent(chapter1.playerFaction.GetFactionCommManager(),
            "Undocking procedure successful! \n You are now on route to the designated mining location. " +
            "As we planned, you will construct the mining station at the designated point (" +
            Mathf.RoundToInt(chapter1.playerMiningStation.GetPosition().x) + ", " + Mathf.RoundToInt(chapter1.playerMiningStation.GetPosition().y) + ") and begin operations.\nGood luck!",
            (communicationEvent) => {
                if (!skipTutorial) {
                    AddTutorial1();
                } else {
                    commManager.SendCommunication(faction, "Skipping Tutorial", (communicationEvent) => {
                        chapter1.GetBattleManager().SetSimulationTimeScale(faction.fleets.First().FleetAI.GetTimeUntilFinishedWithCommand() / 5);
                        chapter1.eventManager.AddEvent(EventCondition.PredicateEvent((_) => playerMiningStation.IsBuilt()), () => {
                            Ship shuttle = faction.ships.First(s => s.IsCivilianShip());
                            if (LocalPlayer.Instance.GetFaction() == faction) {
                                LocalPlayer.Instance.AddOwnedUnit(shuttle);
                            }
                            AddResearchQuestLine();
                        });
                    }, 20);
                }
            }), 10 * GetTimeScale());
    }

    public override void UpdateFactionAI(float deltaTime) {
        UpdateFactionState();
        ManageIdleShips();
    }

    void UpdateFactionState() { }

    private void AddTutorial1() {
        Fleet setupFleet = faction.fleets.First();
        commManager.SendCommunication(chapter1.planetFactionAI.faction, "Thanks for the goodbye! We will send you some resources soon.", 5);
        EventChainBuilder eventChain = new EventChainBuilder();
        eventChain.AddCommEvent(commManager, faction,
            "We have started heading for the new mining site. \n" +
            "If I am talking too fast for you press the \"?\" key to pause and un-pause the game. " +
            "The [<, >] keys can also change how quickly the game time passes.", 15 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Lets review the controls while we are on route to the asteroid fields.", 15 * GetTimeScale());

        // Camera movement Tutorial
        eventChain.AddCommEvent(commManager, faction,
            "Try clicking and holding your right mouse button and moving your mouse to pan the camera.", 7 * GetTimeScale());
        eventChain.AddCondition(EventCondition.PanEvent(40));
        eventChain.AddCommEvent(commManager, faction,
            "If you ever get lost, try pressing V to center your camera again.", 2 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Now scroll out to view more of the solar system.", 7 * GetTimeScale());
        eventChain.AddCondition(EventCondition.ZoomEvent(2000));
        eventChain.AddCommEvent(commManager, faction,
            "Great job! As you can see our ships appear with a green icon when zoomed out, meaning that we own them but can't control them. " +
            "Neutral units will appear grey and hostile units will appear red.");
        eventChain.AddCommEvent(commManager, faction,
            "Now zoom back in to our ships so we can see them better.", 15 * GetTimeScale());
        eventChain.AddCondition(EventCondition.ZoomEvent(300));

        // Selection Tutorial
        eventChain.AddCommEvent(commManager, faction,
            "Well done! Now lets try selecting the ships. Click on one of them to select our fleet.", 2 * GetTimeScale());
        eventChain.AddCondition(EventCondition.SelectFleetEvent(faction.fleets.First(), true));
        eventChain.AddCommEvent(commManager, faction,
            "Our ships are in a fleet, which means if you select one you will select all by default. \n" +
            "Try selecting just one ship in the fleet by holding alt while clicking the ship.", 3 * GetTimeScale());
        eventChain.AddCondition(EventCondition.SelectUnitsAmountEvent(setupFleet.ships.Cast<Unit>().ToHashSet(), 1, true));
        eventChain.AddCommEvent(commManager, faction,
            "You can see a line coming out of the selected ship, this is where they are going. \n" +
            "Try holding shift to select multiple ships. ");
        eventChain.AddCondition(EventCondition.SelectUnitsAmountEvent(setupFleet.ships.Cast<Unit>().ToHashSet(), 2, true));
        eventChain.AddCommEvent(commManager, faction,
            "Exelent. Now click in empty space or press D to deselect the ships.", 1 * GetTimeScale());
        eventChain.AddCondition(EventCondition.UnselectUnitsEvent(battleManager.units, false));
        eventChain.AddCommEvent(commManager, faction,
            "There is one more way that you can select ships, try clicking and dragging your mouse to do a box select. " +
            "Remember to hold alt while doing it.", 1 * GetTimeScale());
        eventChain.AddCondition(EventCondition.SelectUnitsAmountEvent(setupFleet.ships.Cast<Unit>().ToHashSet(), 2, true));
        eventChain.AddCommEvent(commManager, faction,
            "Great job! Now try right clicking on the biggest ship to view its stats.", 1 * GetTimeScale());
        eventChain.AddCondition(EventCondition.OpenObjectPanelEvent(setupFleet.ships.First((ship) => ship.IsConstructionShip()), true));
        eventChain.AddCommEvent(commManager, faction,
            "Here you can see its owner, state, cargo and weapons of the unit. " +
            "Right click again or press the close button to close the panel.", 1 * GetTimeScale());
        eventChain.AddCondition(EventCondition.OpenObjectPanelEvent(null, false));
        // Following Tutorial
        eventChain.AddCommEvent(commManager, faction,
            "We are currently following a ship in our fleet to keep it visible. " +
            "Press B to unfollow the ship in our fleet.", 2 * GetTimeScale());
        eventChain.AddCondition(EventCondition.FollowUnitEvent(null));
        eventChain.AddCommEvent(commManager, faction,
            "As you can see, our ships are moving without the camera now. " +
            "You can always follow a ship again by selecting it and pressing B.", 3 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Zoom all the way out to view more of the solar system.", 12 * GetTimeScale());
        eventChain.AddCondition(EventCondition.ZoomEvent(30000));
        eventChain.AddCommEvent(commManager, faction,
            "You can barely see the stations, planet, the many asteroid fields and gas clouds. " +
            "Our minning team is currently heading to a particularly dense asteroid field to mine.", 2 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Zoom in and right click on the planet to view the political state. This is our home.", 6 * GetTimeScale());
        eventChain.AddCondition(EventCondition.OpenObjectPanelEvent(chapter1.planet, true));
        eventChain.AddCommEvent(commManager, faction,
            "Here you can see the various factions on the planet, their territory and forces.", 5 * GetTimeScale());
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
        }, 10 * GetTimeScale())(); // Don't forget the last set of parenthesis to call the function.
    }

    void AddTutorial2() {
        chapter1.GetBattleManager().SetSimulationTimeScale(faction.fleets.First().FleetAI.GetTimeUntilFinishedWithCommand() / (120 + 40));
        EventChainBuilder eventChainBuilder = new EventChainBuilder();
        eventChainBuilder.AddCommEvent(commManager, faction,
            "Resources on our planet are sparce due to the slow development of resource reusing policy and climate change. " +
            "This is starting to build tension between the major nations. " +
            "Luckily our space instillations are independent of any individual nation so there shouldn't be any space wars out here.", 5 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "Overpopulation has ignited an effort to colonize other planets in the system. " +
            "That is, however a long way off, to start we have been developing the first moon colony. " +
            "We'll see how well it works out.", 35 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "Space technology is relatively new and we are starting to harvest asteroid fields to help solve our resource problems back at " + GetPlanetName() + ".", 20 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "We haven't figured out how to travel to other solar systems yet. " +
            "It might take a hundred years or so until, if it is even possible. " +
            "We have an advanced space research station far out in the solar system is working on this.", 15 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "There was a big boom in civilian space travel once a general purpose space ship came into production in our first designated shipyard.", 15 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, faction,
            "AI technology is also on the rise and may aid space exploration, but like all things, recent research has been limited by the resource crisis.", 15 * GetTimeScale());
        eventChainBuilder.Build(chapter1.eventManager, () => {
            commManager.SendCommunication(chapter1.planetFactionAI.faction, "We are about to arrive at our destination!");
            commManager.SendCommunication(faction,
                "Thats it until until we reach the mining site. \n " +
                "Remember that you can press the [<, >, ?] keys to change how quickly the game time passes. \n " +
                "In the mean time feel free to click the \"Controls help\" button in the top right and read the controls.", 15 * GetTimeScale());

            chapter1.eventManager.AddEvent(EventCondition.PredicateEvent((_) => playerMiningStation.IsBuilt()), () => {
                AddStationTutorial();
            });
        })();
    }

    void AddStationTutorial() {
        chapter1.GetBattleManager().SetSimulationTimeScale(1);
        Ship shuttle = faction.ships.First(s => s.IsCivilianShip());
        if (LocalPlayer.Instance.GetFaction() == faction) {
            LocalPlayer.Instance.AddOwnedUnit(shuttle);
        }
        EventChainBuilder movementTutorial = new EventChainBuilder();
        commManager.SendCommunication(new CommunicationEvent(chapter1.planetFactionAI.faction.GetFactionCommManager(), "We have arrived safely at the destination and are setting up our operations.",
        new CommunicationEventOption[] { new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; },
            (communicationEvent) => {
                if (!communicationEvent.isActive)
                    return false;
                AddTradeRouteToStation(chapter1.tradeStation);
                communicationEvent.DeactivateEvent();
                chapter1.shipyardFaction.GetFactionCommManager().SendCommunication(faction,
                    "Good to see that you are set up and everything is going well. " +
                    "We are setting up a trade route for you. " +
                    "We will give you resources to operate your station in return for metal.",
                    (communicationEvent) => movementTutorial.Build(chapter1.eventManager)(), 3 * GetTimeScale());
                return true;
            })
        }, true), 2 * GetTimeScale());
        movementTutorial.AddCommEvent(commManager, faction,
            "Lets learn about ship movement and investigate the nearby asteroid fields. " +
            "Open the minning station panel and click on the civilian ship button in the hanger, then close the menu.", 10 * GetTimeScale());
        movementTutorial.AddCondition(EventCondition.SelectUnitEvent(shuttle, true));
        movementTutorial.AddCondition(EventCondition.OpenObjectPanelEvent(null, true));
        movementTutorial.AddCondition(EventCondition.SelectUnitEvent(shuttle, true));
        movementTutorial.AddCommEvent(commManager, faction,
            "Now press Q and click on the asteroid field highlighted nearby to issue a move command to it.");
        List<AsteroidField> closestAsteroidFields = battleManager.asteroidFields.ToList().OrderBy((a) => Vector2.Distance(shuttle.GetPosition(), a.GetPosition())).ToList();
        movementTutorial.AddCondition(EventCondition.MoveShipToObject(shuttle, closestAsteroidFields.First(), true));
        // Make sure the ship isn't moving
        movementTutorial.AddCondition(EventCondition.WaitUntilShipsIdle(new List<Ship> { shuttle }));
        movementTutorial.AddCommEvent(commManager, faction,
            "Lets survey some more asteroid fields. " +
            "Hold shift while issuing a move command to add the command to the ships command queue. " +
            "Tell the ship to move to the asteroid fields when they are highlighted.");
        movementTutorial.AddCondition(EventCondition.CommandMoveShipToObjectSequence(shuttle, closestAsteroidFields.GetRange(1, 4).Cast<IObject>().ToList(), true));
        movementTutorial.AddAction(() => battleManager.SetSimulationTimeScale(10));
        movementTutorial.AddCommEvent(commManager, faction,
            "Issue a dock command by selecting the ship and the move command and clicking on the station. " +
            "We can also add a command to the start of the queue by holding alt, try it!", 10 * GetTimeScale());
        movementTutorial.AddCondition(EventCondition.DockShipsAtUnit(new List<Ship> { shuttle }, playerMiningStation, true));
        movementTutorial.AddCommEvent(commManager, faction,
            "Great job, this concludes the movement practice for now.", 2 * GetTimeScale());
        movementTutorial.AddAction(AddResearchQuestLine);
    }

    void AddResearchQuestLine() {
        Ship shuttle = faction.ships.First(s => s.IsCivilianShip());
        chapter1.GetBattleManager().SetSimulationTimeScale(10);
        Faction researchFaction = chapter1.researchFaction;
        Station researchStation = chapter1.researchStation;
        FactionCommManager researchCommManager = researchFaction.GetFactionCommManager();

        EventChainBuilder builder = new EventChainBuilder();
        builder.AddCommEvent(researchFaction.GetFactionCommManager(), faction,
            "Send your shuttle over to our research station station. " +
            "We have some science experiments to do.", 10);
        builder.AddAction(() => {
            chapter1.eventManager.AddEvent(EventCondition.CommandDockShipToUnit(shuttle, researchStation, false), () => {
                commManager.SendCommunication(faction,
                    "Our ship is on route! Remember that you can always use the [<, >, ?] keys to change the speed of time.", 5 * GetTimeScale());
            });
            commManager.SendCommunication(faction, 
                "The Research station is far away. You will have to scroll out and pan around to see it.", 20 * GetTimeScale());
        });
        builder.AddCondition(EventCondition.DockShipsAtUnit(new List<Ship>() { shuttle }, researchStation, true));
        builder.AddCommEvent(commManager, researchFaction,
            "Our shuttle has arrived at your research station. What would you like for us to do?", 2 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, faction,
            "Good to see that you got here safely! \n " +
            "Our station was sent up as a combined reasearch initiative. " +
            "We were initially supported by most of the factions back on the planet. " +
            "However, now that tensions are building it is too risky for them to invest into research that could benefit some factions more than others.", 2 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, faction,
            "We have found some interesting gas clouds farther from the sun and they may contain rare gases that could be usefull back on the planet. " +
            "Unfortunately we don't have the funding to construct a science ship with expensive equipment.", 30 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, faction,
            "Our research could greatly help you and the rest of space exploration. " +
            "It would be a great help if you could help us out by sending your ship to the gas field that we have marked.", 20 * GetTimeScale());
        GasCloud targetGasCloud = researchFaction.GetClosestGasCloud(researchStation.GetPosition());
        builder.AddCondition(EventCondition.MoveShipToObject(shuttle, targetGasCloud, true));
        builder.AddCommEvent(researchCommManager, faction,
            "We have recieved some preliminary data about the gas. The high density of the gas cloud is spectacular! " +
            "There are some anomalies about it that we can't figure out with the small amount of equipment on your ship. \n" +
            "Could you bring the ship back to our station with a sample to farther analyze the gas?", 5 * GetTimeScale());
        builder.AddCommEvent(commManager, researchFaction,
            "No problem! We'll bring the gas back to the station.", 1 * GetTimeScale());
        builder.AddCondition(EventCondition.DockShipsAtUnit(new List<Ship> { shuttle }, researchStation, true));
        builder.AddCommEvent(researchFaction.GetFactionCommManager(), faction, 
            "Thanks for the gas! We won't be needing your ship anytime soon so you are free to use it again." +
            "We'll need some time to analyse this gas, I'm sure it will be of use.", 3 * GetTimeScale());
        builder.AddCommEvent(commManager, researchFaction,
            "Sounds great! Let us know if you find anything interesting about the gas.", 5 * GetTimeScale());
        builder.AddCommEvent(researchFaction.GetFactionCommManager(), faction,
            "We have fully analysed the high density gas. " +
            "It seems like it could be used as a very efficient energy generation tool! " +
            "With a specialised reactor installed in our space ships we could last quite a while in deep space without much sunlight.", 500 * GetTimeScale());
        builder.AddCommEvent(commManager, researchFaction,
            "Thats interesting, is there any way we could help collect it.", 5 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, faction,
            "You could start by collecting the gas with a specialised gas collector ship. " +
            "We can then build generators in our stations to provide them with an abundance of energy.", 20 * GetTimeScale());
        builder.Build(chapter1.eventManager)();
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