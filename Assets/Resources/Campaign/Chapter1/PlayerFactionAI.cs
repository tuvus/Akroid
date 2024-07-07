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
        eventChain.AddEvent(EventCondition.PanEvent(40));
        eventChain.AddCommEvent(commManager, faction,
            "If you ever get lost, try pressing V to center your camera again.", 2 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Now scroll out to view more of the solar system.", 7 * GetTimeScale());
        eventChain.AddEvent(EventCondition.ZoomEvent(2000));
        eventChain.AddCommEvent(commManager, faction,
            "Great job! As you can see our ships appear with a green icon when zoomed out, meaning that we own them but can't control them. " +
            "Neutral units will appear grey and hostile units will appear red.");
        eventChain.AddCommEvent(commManager, faction,
            "Now zoom back in to our ships so we can see them better.", 15 * GetTimeScale());
        eventChain.AddEvent(EventCondition.ZoomEvent(300));

        // Selection Tutorial
        eventChain.AddCommEvent(commManager, faction,
            "Well done! Now lets try selecting the ships. Click on one of them to select our fleet.", 2 * GetTimeScale());
        eventChain.AddEvent(EventCondition.SelectFleetEvent(faction.fleets.First(), true));
        eventChain.AddCommEvent(commManager, faction,
            "Our ships are in a fleet, which means if you select one you will select all by default. \n" +
            "Try selecting just one ship in the fleet by holding alt while clicking the ship.", 3 * GetTimeScale());
        eventChain.AddEvent(EventCondition.SelectUnitsAmountEvent(setupFleet.ships.Cast<Unit>().ToHashSet(), 1, true));
        eventChain.AddCommEvent(commManager, faction,
            "You can see a line coming out of the selected ship, this is where they are going. \n" +
            "Try holding shift to select multiple ships. " +
            "You will have to hold alt as well since the ships are in a fleet.");
        eventChain.AddEvent(EventCondition.SelectUnitsAmountEvent(setupFleet.ships.Cast<Unit>().ToHashSet(), 2, true));
        eventChain.AddCommEvent(commManager, faction,
            "Exelent. Now click in empty space or press D to deselect the ships.", 1 * GetTimeScale());
        eventChain.AddEvent(EventCondition.UnselectUnitsEvent(battleManager.units, false));
        eventChain.AddCommEvent(commManager, faction,
            "There is one more way that you can select ships, try clicking and dragging your mouse to do a box select. " +
            "Remember to hold alt while doing it.", 1 * GetTimeScale());
        eventChain.AddEvent(EventCondition.SelectUnitsAmountEvent(setupFleet.ships.Cast<Unit>().ToHashSet(), 2, true));
        eventChain.AddCommEvent(commManager, faction,
            "Great job! Now try right clicking on the biggest ship to view its stats.", 1 * GetTimeScale());
        eventChain.AddEvent(EventCondition.OpenObjectPanelEvent(setupFleet.ships.First((ship) => ship.IsConstructionShip()), true));
        eventChain.AddCommEvent(commManager, faction,
            "Here you can see its owner, state, cargo and weapons of the unit. " +
            "Right click again or press the close button to close the panel.", 1 * GetTimeScale());
        eventChain.AddEvent(EventCondition.OpenObjectPanelEvent(null, false));
        // Following Tutorial
        eventChain.AddCommEvent(commManager, faction,
            "We are currently following a ship in our fleet to keep it visible. " +
            "Press B to unfollow the ship in our fleet.", 2 * GetTimeScale());
        eventChain.AddEvent(EventCondition.FollowUnitEvent(null));
        eventChain.AddCommEvent(commManager, faction,
            "As you can see, our ships are moving without the camera now. " +
            "You can always follow a ship again by selecting it and pressing B.", 3 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Zoom all the way out to view more of the solar system.", 12 * GetTimeScale());
        eventChain.AddEvent(EventCondition.ZoomEvent(30000));
        eventChain.AddCommEvent(commManager, faction,
            "You can barely see the stations, planet, the many asteroid fields and gas clouds. " +
            "Our minning team is currently heading to a particularly dense asteroid field to mine.", 2 * GetTimeScale());
        eventChain.AddCommEvent(commManager, faction,
            "Zoom in and right click on the planet to view the political state. This is our home.", 6 * GetTimeScale());
        eventChain.AddEvent(EventCondition.OpenObjectPanelEvent(chapter1.planet, true));
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
        chapter1.GetBattleManager().SetSimulationTimeScale(faction.fleets.First().FleetAI.GetTimeUntilFinishedWithCommand() / (5));
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
        movementTutorial.AddEvent(EventCondition.SelectUnitEvent(shuttle, true));
        movementTutorial.AddEvent(EventCondition.OpenObjectPanelEvent(null, true));
        movementTutorial.AddEvent(EventCondition.SelectUnitEvent(shuttle, true));
        movementTutorial.AddCommEvent(commManager, faction,
            "Now press Q and click on the asteroid field highlighted nearby to issue a move command to it.");
        List<AsteroidField> closestAsteroidFields = battleManager.asteroidFields.ToList().OrderBy((a) => Vector2.Distance(shuttle.GetPosition(), a.GetPosition())).ToList();
        movementTutorial.AddEvent(EventCondition.MoveShipToObject(shuttle, closestAsteroidFields.First(), true));
        // Make sure the ship isn't moving
        movementTutorial.AddEvent(EventCondition.WaitUntilShipsIdle(new List<Ship> { shuttle }));
        movementTutorial.AddCommEvent(commManager, faction,
            "Lets survey some more asteroid fields. " +
            "Hold shift while issuing a move command to add the command to the ships command queue. " +
            "Tell the ship to move to the asteroid fields when they are highlighted.");
        movementTutorial.AddEvent(EventCondition.CommandMoveShipToObjectSequence(shuttle, closestAsteroidFields.GetRange(1, 4).Cast<IObject>().ToList(), true));
        movementTutorial.AddAction(() => battleManager.SetSimulationTimeScale(10));
        movementTutorial.AddCommEvent(commManager, faction,
            "Issue a dock command by selecting the ship and the move command and clicking on the station. " +
            "We can also add a command to the start of the queue by holding alt, try it!", 10 * GetTimeScale());
        movementTutorial.AddEvent(EventCondition.DockShipsAtUnit(new List<Ship> { shuttle }, playerMiningStation, true));
        movementTutorial.AddCommEvent(commManager, faction,
            "Great job, this concludes the movement practice for now.", 2 * GetTimeScale());
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