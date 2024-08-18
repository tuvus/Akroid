using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static BattleManager;
using static CommunicationEvent;

public class Chapter1 : CampaingController {
    public Faction playerFaction { get; private set; }
    public PlayerFactionAI playerFactionAI { get; private set; }
    public MiningStation playerMiningStation { get; private set; }
    Faction otherMiningFaction;
    OtherMiningFactionAI otherMiningFactionAI;
    MiningStation otherMiningStation;
    Faction planetFaction;
    public PlanetFactionAI planetFactionAI { get; private set; }
    public Planet planet { get; private set; }
    public Shipyard tradeStation { get; private set; }
    public Faction shipyardFaction { get; private set; }
    public ShipyardFactionAI shipyardFactionAI { get; private set; }
    public Shipyard shipyard { get; private set; }
    public Faction researchFaction { get; private set; }
    public Station researchStation { get; private set; }
    public Dictionary<CargoBay.CargoTypes, double> resourceCosts;

    public DifficultyLevel difficultyLevel { get; private set; }

    public enum DifficultyLevel {
        Easy,
        Normal,
        Hard,
    }

    /// <summary>
    /// Sets up and generates the solar system
    /// </summary>
    public override void SetupBattle(BattleManager battleManager) {
        base.SetupBattle(battleManager);
        battleManager.SetSimulationTimeScale(1);
        resourceCosts = new Dictionary<CargoBay.CargoTypes, double>(10) {
            { CargoBay.CargoTypes.Metal, 4.6f },
            { CargoBay.CargoTypes.Gas, 13.9f }
        };
        battleManager.CreateNewStar("Sun");
        ColorPicker colorPicker = new ColorPicker();
        playerFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(PlayerFactionAI), "Free Space Miners", "FSM", colorPicker.PickColor(), Random.Range(1, 2) * 5400, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        playerFactionAI = (PlayerFactionAI)playerFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(playerFaction.GetPosition(), 0, 5000, 100, 1000, 2), Random.Range(5, 10), 10);
        }
        playerMiningStation = (MiningStation)battleManager.CreateNewStation(new Station.StationData(playerFaction, battleManager.GetStationBlueprint(Station.StationType.MiningStation).stationScriptableObject, "Mining Station", playerFaction.GetPosition(), Random.Range(0, 360), false));

        otherMiningFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(OtherMiningFactionAI), "Off-World Metal Industries", "OWM", colorPicker.PickColor(), 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        otherMiningFactionAI = (OtherMiningFactionAI)otherMiningFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(otherMiningFaction.GetPosition(), 0, 5000, 100, 1000, 2), Random.Range(5, 10), 10);
        }
        otherMiningStation = (MiningStation)battleManager.CreateNewStation(new Station.StationData(otherMiningFaction, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/MiningStation"), "Mining Station", otherMiningFaction.GetPosition(), Random.Range(0, 360), true));
        otherMiningStation.BuildShip(Ship.ShipClass.Transport);
        otherMiningStation.LoadCargo(2400 * 3, CargoBay.CargoTypes.Metal);


        planetFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(PlanetFactionAI), "World Space Union", "WSU", colorPicker.PickColor(), 100000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        planet = battleManager.CreateNewPlanet(new BattleManager.PositionGiver(planetFaction.GetPosition()), new Planet.PlanetData(planetFaction, "Home", Random.Range(0,360), (long)Random.Range(500, 600) * 100000000, 0.01, Random.Range(0.12f, 0.25f), Random.Range(0.18f, 0.25f), Random.Range(0.1f, 0.2f)));
        planet.SetPopulationTarget((long)(planet.GetPopulation() * 1.1));
        tradeStation = (Shipyard)battleManager.CreateNewStation(new Station.StationData(planetFaction, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/TradeStation"), "Trade Station", planet.GetPosition(), Random.Range(0, 360)), new PositionGiver(Vector2.MoveTowards(planet.GetPosition(), Vector2.zero, planet.GetSize() + 180), 0, 1000, 50, 200, 5));
        tradeStation.LoadCargo(2400 * 5, CargoBay.CargoTypes.Metal);
        ((ShipyardAI)tradeStation.stationAI).autoCollectCargo = false;
        tradeStation.GetConstructionBay().AddConstructionToBeginningQueue(new Ship.ShipConstructionBlueprint(planetFaction, battleManager.GetShipBlueprint(Ship.ShipType.Civilian), "Civilian Ship"));
        planetFactionAI = (PlanetFactionAI)planetFaction.GetFactionAI();
        tradeStation.stationAI.onBuildShip += (ship) => { if (ship.faction == LocalPlayer.Instance.GetFaction()) LocalPlayer.Instance.AddOwnedUnit(ship); };

        shipyardFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(ShipyardFactionAI), "Solar Shipyards", "SSH", colorPicker.PickColor(), (long)(2400 * resourceCosts[CargoBay.CargoTypes.Metal] * 1.4f), 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        shipyard = (Shipyard)battleManager.CreateNewStation(new Station.StationData(shipyardFaction, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/Shipyard"), "Solar Shipyard", shipyardFaction.GetPosition(), Random.Range(0, 360)));
        Ship shipyardTransport = shipyard.BuildShip(Ship.ShipClass.Transport);
        shipyardTransport.LoadCargo(2400, CargoBay.CargoTypes.Metal);
        shipyardFactionAI = (ShipyardFactionAI)shipyardFaction.GetFactionAI();
        shipyard.stationAI.onBuildShip += (ship) => { if (ship.faction == LocalPlayer.Instance.GetFaction()) LocalPlayer.Instance.AddOwnedUnit(ship); };
        

        researchFaction = battleManager.CreateNewFaction(new Faction.FactionData("Frontier Research", "FRO", colorPicker.PickColor(), 3000, 36, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 5000, 2), 100);
        researchStation = battleManager.CreateNewStation(new Station.StationData(researchFaction, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/ResearchStation"), "Frontier Station", researchFaction.GetPosition(), Random.Range(0, 360)));

        playerMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        Ship setupFleetShip1 = tradeStation.BuildShip(playerFaction, Ship.ShipClass.Transport);
        ConstructionShip setupFleetShip2 = (ConstructionShip)tradeStation.BuildShip(playerFaction, Ship.ShipClass.StationBuilder);
        setupFleetShip2.targetStationBlueprint = playerMiningStation;
        Ship setupFleetShip3 = tradeStation.BuildShip(playerFaction, Ship.ShipClass.Transport);
        Ship setupFleetShip4 = tradeStation.BuildShip(playerFaction, Ship.ShipType.Civilian, "Shuttle");
        Fleet miningStationSetupFleet = playerFaction.CreateNewFleet("Station Setup Fleet", playerFaction.ships);
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateWaitCommand(4 * battleManager.timeScale), Command.CommandAction.Replace);
        miningStationSetupFleet.FleetAI.AddFormationTowardsPositionCommand(playerMiningStation.GetPosition(), shipyard.GetSize() * 4, Command.CommandAction.AddToEnd);
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateWaitCommand(3 * battleManager.timeScale));
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateMoveOffsetCommand(miningStationSetupFleet.GetPosition(), playerMiningStation.GetPosition(), playerMiningStation.GetSize() * 3));
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateDockCommand(playerMiningStation));
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateDisbandFleetCommand());

        otherMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        otherMiningFaction.GetTransportShip(0).shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(10, 20)), Command.CommandAction.AddToBegining);

        List<Ship> civilianShips = new List<Ship>();
        for (int i = 0; i < Random.Range(0, 2); i++) {
            civilianShips.Add(tradeStation.BuildShip(planetFaction, battleManager.GetShipBlueprint(Ship.ShipType.Civilian).shipScriptableObject, "Civilian"));
        }

        playerFactionAI.SetupPlayerFactionAI(battleManager, playerFaction, this, playerMiningStation);
        otherMiningFactionAI.SetupOtherMiningFactionAI(battleManager, otherMiningFaction, this, shipyardFactionAI, otherMiningStation, tradeStation);
        planetFactionAI.SetupPlanetFactionAI(battleManager, planetFaction, this, shipyardFactionAI, planet, tradeStation, shipyard, civilianShips, eventManager);
        shipyardFactionAI.SetupShipyardFactionAI(battleManager, shipyardFaction, this, planetFactionAI, shipyard);


        int asteroidFieldCount = Random.Range(50, 80);
        for (int i = 0; i < asteroidFieldCount; i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(Vector2.zero, 1500, 100000, 20000, 300, 1), Random.Range(8, 10), 10);
        }

        int gasFieldCount = Random.Range(8, 14);
        for (int i = 0; i < gasFieldCount; i++) {
            battleManager.CreateNewGasCloud(new PositionGiver(Vector2.zero, 10000, 100000, 20000, 1000, 3));
        }


        planet.AddFaction(planetFaction, Random.Range(0.05f, 0.1f), 0f, 0f, Random.Range(5.0f, 10.0f), "Increases space production");
        Faction planetEmpire = battleManager.CreateNewFaction(new Faction.FactionData("Empire", "EMP", colorPicker.PickColor(), 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(planetEmpire, Random.Range(0.20f, 0.35f), Random.Range(8.0f, 10.0f), "Increases unit production");
        Faction planetDemocracy = battleManager.CreateNewFaction(new Faction.FactionData("Democracy", "DEM", colorPicker.PickColor(), 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(planetDemocracy, Random.Range(0.30f, 0.40f), Random.Range(6f, 11f), "Increases mining speed");
        Faction planetOligarchy = battleManager.CreateNewFaction(new Faction.FactionData("Oligarchy", "OLG", colorPicker.PickColor(), 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(planetOligarchy, Random.Range(0.70f, 0.80f), Random.Range(5f, 8f), "Increases research rate");
        Faction minorFactions = battleManager.CreateNewFaction(new Faction.FactionData("Minor Factions", "MIN", colorPicker.PickColor(), 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(minorFactions, Random.Range(0.70f, 0.80f), Random.Range(2f, 3f), "All base stats improved a little");
        //planetEmpire.StartWar(planetDemocracy);
        //planetDemocracy.StartWar(planetOligarchy);
        //planetOligarchy.StartWar(planetEmpire);

        LocalPlayer.Instance.lockedOwnedUnits = true;
        LocalPlayer.Instance.ownedUnits.Add(playerMiningStation);
        LocalPlayer.Instance.SetupFaction(playerFaction);
        LocalPlayer.Instance.GetLocalPlayerInput().SetZoom(400);
        LocalPlayer.Instance.GetLocalPlayerInput().StartFollowingUnit(setupFleetShip2);

        StartTutorial();
    }

    /// <summary>
    /// Handles the first part of the tutorial where the fleet is on the way to set up the station.
    /// This can be skipped by holding left shift
    /// </summary>
    private void StartTutorial() {
        // Increase time to skip tutorial
        bool skipTutorial = false;
        EventChainBuilder eventChain = new EventChainBuilder();
        eventChain.AddCondition(EventCondition.WaitEvent(1f));
        eventChain.AddAction(() => {
            if (LocalPlayer.Instance.GetLocalPlayerGameInput().AdditiveButtonPressed) {
                GetBattleManager().SetSimulationTimeScale(10);
                skipTutorial = true;
            }
        });
        eventChain.Build(eventManager)();
        EventChainBuilder eventChain2 = new EventChainBuilder();
        eventChain2.AddCondition(EventCondition.WaitEvent(2f));
        eventChain2.AddAction(() => {
            if (!skipTutorial && LocalPlayer.Instance.GetLocalPlayerGameInput().AdditiveButtonPressed) {
                GetBattleManager().SetSimulationTimeScale(10);
                skipTutorial = true;
            }
        });
        eventChain2.Build(eventManager)();
        EventChainBuilder eventChain3 = new EventChainBuilder();
        eventChain3.AddCondition(EventCondition.WaitEvent(5f));
        eventChain3.AddAction(() => {
            if (!skipTutorial && LocalPlayer.Instance.GetLocalPlayerGameInput().AdditiveButtonPressed) {
                GetBattleManager().SetSimulationTimeScale(10);
                skipTutorial = true;
            }
        });
        eventChain3.Build(eventManager)();
        EventChainBuilder eventChain4 = new EventChainBuilder();
        eventChain4.AddCondition(EventCondition.WaitEvent(10f));
        eventChain4.AddAction(() => {
            if (!skipTutorial && LocalPlayer.Instance.GetLocalPlayerGameInput().AdditiveButtonPressed) {
                GetBattleManager().SetSimulationTimeScale(10);
                skipTutorial = true;
            }
        });
        eventChain4.Build(eventManager)();


        planetFactionAI.faction.GetFactionCommManager().SendCommunication(new CommunicationEvent(playerFaction.GetFactionCommManager(),
            "Undocking procedure successful! \n You are now on route to the designated mining location. " +
            "As we planned, you will construct the mining station at the designated point (" +
            Mathf.RoundToInt(playerMiningStation.GetPosition().x) + ", " + Mathf.RoundToInt(playerMiningStation.GetPosition().y) + ") and begin operations.\nGood luck!",
            (communicationEvent) => {
                if (!skipTutorial) {
                    AddTutorial1();
                } else {
                    playerFaction.GetFactionCommManager().SendCommunication(playerFaction, "Skipping Tutorial", (communicationEvent) => {
                        GetBattleManager().SetSimulationTimeScale(playerFaction.fleets.First().FleetAI.GetTimeUntilFinishedWithCommand() / 5);
                        eventManager.AddEvent(EventCondition.PredicateEvent((_) => playerMiningStation.IsBuilt()), () => {
                            Ship shuttle = playerFaction.ships.First(s => s.IsCivilianShip());
                            if (LocalPlayer.Instance.GetFaction() == playerFaction) {
                                LocalPlayer.Instance.AddOwnedUnit(shuttle);
                            }
                            playerFactionAI.AddTradeRouteToStation(tradeStation);
                            AddResearchQuestLine();
                        });
                    }, 20);
                }
            }), 10 * GetTimeScale());
    }


    private void AddTutorial1() {
        Fleet setupFleet = playerFaction.fleets.First();
        FactionCommManager commManager = playerFaction.GetFactionCommManager();
        commManager.SendCommunication(planetFactionAI.faction, "Thanks for the goodbye! We will send you some resources soon.", 5);
        EventChainBuilder eventChain = new EventChainBuilder();
        eventChain.AddCommEvent(commManager, playerFaction,
            "We have started heading for the new mining site. \n" +
            "If I am talking too fast for you press the \"?\" key to pause and un-pause the game. " +
            "The [<, >] keys can also change how quickly the game time passes.", 15 * GetTimeScale());
        eventChain.AddCommEvent(commManager, playerFaction,
            "Lets review the controls while we are on route to the asteroid fields.", 15 * GetTimeScale());

        // Camera movement Tutorial
        eventChain.AddCommEvent(commManager, playerFaction,
            "Try clicking and holding your right mouse button and moving your mouse to pan the camera.", 7 * GetTimeScale());
        eventChain.AddCondition(EventCondition.PanEvent(40));
        eventChain.AddCommEvent(commManager, playerFaction,
            "If you ever get lost, try pressing V to center your camera again.", 2 * GetTimeScale());
        eventChain.AddCommEvent(commManager, playerFaction,
            "Now scroll out to view more of the solar system.", 7 * GetTimeScale());
        eventChain.AddCondition(EventCondition.ZoomEvent(2000));
        eventChain.AddCommEvent(commManager, playerFaction,
            "Great job! As you can see our ships appear with a green icon when zoomed out, meaning that we own them but can't control them. " +
            "Neutral units will appear grey and hostile units will appear red.");
        eventChain.AddCommEvent(commManager, playerFaction,
            "Now zoom back in to our ships so we can see them better.", 15 * GetTimeScale());
        eventChain.AddCondition(EventCondition.ZoomEvent(300));

        // Selection Tutorial
        eventChain.AddCommEvent(commManager, playerFaction,
            "Well done! Now lets try selecting the ships. Click on one of them to select our fleet.", 2 * GetTimeScale());
        eventChain.AddCondition(EventCondition.SelectFleetEvent(playerFaction.fleets.First(), true));
        eventChain.AddCommEvent(commManager, playerFaction,
            "Our ships are in a fleet, which means if you select one you will select all by default. \n" +
            "Try selecting just one ship in the fleet by holding alt while clicking the ship.", 3 * GetTimeScale());
        eventChain.AddCondition(EventCondition.SelectUnitsAmountEvent(setupFleet.ships.Cast<Unit>().ToHashSet(), 1, true));
        eventChain.AddCommEvent(commManager, playerFaction,
            "You can see a line coming out of the selected ship, this is where they are going. \n" +
            "Try holding shift to select multiple ships. ");
        eventChain.AddCondition(EventCondition.SelectUnitsAmountEvent(setupFleet.ships.Cast<Unit>().ToHashSet(), 2, true));
        eventChain.AddCommEvent(commManager, playerFaction,
            "Exelent. Now click in empty space or press D to deselect the ships.", 1 * GetTimeScale());
        eventChain.AddCondition(EventCondition.UnselectUnitsEvent(battleManager.units, false));
        eventChain.AddCommEvent(commManager, playerFaction,
            "There is one more way that you can select ships, try clicking and dragging your mouse to do a box select. " +
            "Remember to hold alt while doing it.", 1 * GetTimeScale());
        eventChain.AddCondition(EventCondition.SelectUnitsAmountEvent(setupFleet.ships.Cast<Unit>().ToHashSet(), 2, true));
        eventChain.AddCommEvent(commManager, playerFaction,
            "Great job! Now try right clicking on the biggest ship to view its stats.", 1 * GetTimeScale());
        eventChain.AddCondition(EventCondition.OpenObjectPanelEvent(setupFleet.ships.First((ship) => ship.IsConstructionShip()), true));
        eventChain.AddCommEvent(commManager, playerFaction,
            "Here you can see its owner, state, cargo and weapons of the unit. " +
            "Right click again or press the close button to close the panel.", 1 * GetTimeScale());
        eventChain.AddCondition(EventCondition.OpenObjectPanelEvent(null, false));
        // Following Tutorial
        eventChain.AddCommEvent(commManager, playerFaction,
            "We are currently following a ship in our fleet to keep it visible. " +
            "Press B to unfollow the ship in our fleet.", 2 * GetTimeScale());
        eventChain.AddCondition(EventCondition.FollowUnitEvent(null));
        eventChain.AddCommEvent(commManager, playerFaction,
            "As you can see, our ships are moving without the camera now. " +
            "You can always follow a ship again by selecting it and pressing B.", 3 * GetTimeScale());
        eventChain.AddCommEvent(commManager, playerFaction,
            "Zoom all the way out to view more of the solar system.", 12 * GetTimeScale());
        eventChain.AddCondition(EventCondition.ZoomEvent(30000));
        eventChain.AddCommEvent(commManager, playerFaction,
            "You can barely see the stations, planet, the many asteroid fields and gas clouds. " +
            "Our minning team is currently heading to a particularly dense asteroid field to mine.", 2 * GetTimeScale());
        eventChain.AddCommEvent(commManager, playerFaction,
            "Zoom in and right click on the planet to view the political state. This is our home.", 6 * GetTimeScale());
        eventChain.AddCondition(EventCondition.OpenObjectPanelEvent(planet, true));
        eventChain.AddCommEvent(commManager, playerFaction,
            "Here you can see the various factions on the planet, their territory and forces.", 5 * GetTimeScale());
        eventChain.Build(eventManager, commManager, playerFaction,
            "What difficulty would you like to play at? Harder difficulties will have a faster intro scene.",
            new CommunicationEventOption[] {
                new CommunicationEventOption("Easy", (communicationEvent) => { return true; }, (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    SetDifficultyLevel(DifficultyLevel.Easy);
                    AddTutorial2();
                    return true; }),
                new CommunicationEventOption("Normal", (communicationEvent) => { return true; }, (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    SetDifficultyLevel(DifficultyLevel.Normal);
                    AddTutorial2();
                    return true; }),
                new CommunicationEventOption("Hard", (communicationEvent) => { return true; }, (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    SetDifficultyLevel(DifficultyLevel.Hard);
                    AddTutorial2();
                    return true; })
        }, 10 * GetTimeScale())(); // Don't forget the last set of parenthesis to call the function.
    }


    void AddTutorial2() {
        FactionCommManager commManager = playerFaction.GetFactionCommManager();
        GetBattleManager().SetSimulationTimeScale(playerFaction.fleets.First().FleetAI.GetTimeUntilFinishedWithCommand() / (120 + 40));
        EventChainBuilder eventChainBuilder = new EventChainBuilder();
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "Resources on our planet are sparce due to the slow development of resource reusing policy and climate change. " +
            "This is starting to build tension between the major nations. " +
            "Luckily our space instillations are independent of any individual nation so there shouldn't be any space wars out here.", 5 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "Overpopulation has ignited an effort to colonize other planets in the system. " +
            "That is, however a long way off, to start we have been developing the first moon colony. " +
            "We'll see how well it works out.", 35 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "Space technology is relatively new and we are starting to harvest asteroid fields to help solve our resource problems back at " + planet.objectName + ".", 20 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "We haven't figured out how to travel to other solar systems yet. " +
            "It might take a hundred years or so until, if it is even possible. " +
            "We have an advanced space research station far out in the solar system is working on this.", 15 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "There was a big boom in civilian space travel once a general purpose space ship came into production in our first designated shipyard.", 15 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "AI technology is also on the rise and may aid space exploration, but like all things, recent research has been limited by the resource crisis.", 15 * GetTimeScale());
        eventChainBuilder.Build(eventManager, () => {
            commManager.SendCommunication(planetFactionAI.faction, "We are about to arrive at our destination!");
            commManager.SendCommunication(playerFaction,
                "Thats it until until we reach the mining site. \n " +
                "Remember that you can press the [<, >, ?] keys to change how quickly the game time passes. \n " +
                "In the mean time feel free to click the \"Controls help\" button in the top right and read the controls.", 15 * GetTimeScale());

            eventManager.AddEvent(EventCondition.PredicateEvent((_) => playerMiningStation.IsBuilt()), () => {
                AddStationTutorial();
            });
        })();
    }

    void AddStationTutorial() {
        FactionCommManager commManager = playerFaction.GetFactionCommManager();
        GetBattleManager().SetSimulationTimeScale(1);
        Ship shuttle = playerFaction.ships.First(s => s.IsCivilianShip());
        if (LocalPlayer.Instance.GetFaction() == playerFaction) {
            LocalPlayer.Instance.AddOwnedUnit(shuttle);
        }
        EventChainBuilder movementTutorial = new EventChainBuilder();
        commManager.SendCommunication(new CommunicationEvent(planetFactionAI.faction.GetFactionCommManager(), "We have arrived safely at the destination and are setting up our operations.",
        new CommunicationEventOption[] { new CommunicationEventOption("Trade Metal", (communicationEvent) => { return true; },
            (communicationEvent) => {
                if (!communicationEvent.isActive)
                    return false;
                playerFactionAI.AddTradeRouteToStation(tradeStation);
                communicationEvent.DeactivateEvent();
                shipyardFaction.GetFactionCommManager().SendCommunication(playerFaction,
                    "Good to see that you are set up and everything is going well. " +
                    "We are setting up a trade route for you. " +
                    "We will give you resources to operate your station in return for metal.",
                    (communicationEvent) => movementTutorial.Build(eventManager)(), 3 * GetTimeScale());
                return true;
            })
        }, true), 2 * GetTimeScale());
        movementTutorial.AddCommEvent(commManager, playerFaction,
            "Lets learn about ship movement and investigate the nearby asteroid fields. " +
            "Open the minning station panel and click on the civilian ship button in the hangar, then close the menu.", 10 * GetTimeScale());
        movementTutorial.AddCondition(EventCondition.SelectUnitEvent(shuttle, true));
        movementTutorial.AddCondition(EventCondition.OpenObjectPanelEvent(null, true));
        movementTutorial.AddCondition(EventCondition.SelectUnitEvent(shuttle, true));
        movementTutorial.AddCommEvent(commManager, playerFaction,
            "Now press Q and click on the asteroid field highlighted nearby to issue a move command to it.");
        List<AsteroidField> closestAsteroidFields = battleManager.asteroidFields.ToList().OrderBy((a) => Vector2.Distance(shuttle.GetPosition(), a.GetPosition())).ToList();
        movementTutorial.AddCondition(EventCondition.MoveShipToObject(shuttle, closestAsteroidFields.First(), true));
        // Make sure the ship isn't moving
        movementTutorial.AddCondition(EventCondition.WaitUntilShipsIdle(new List<Ship> { shuttle }));
        movementTutorial.AddCommEvent(commManager, playerFaction,
            "Lets survey some more asteroid fields. " +
            "Hold shift while issuing a move command to add the command to the ships command queue. " +
            "Tell the ship to move to the asteroid fields when they are highlighted.");
        movementTutorial.AddCondition(EventCondition.CommandMoveShipToObjectSequence(shuttle, closestAsteroidFields.GetRange(1, 4).Cast<IObject>().ToList(), true));
        movementTutorial.AddAction(() => battleManager.SetSimulationTimeScale(10));
        movementTutorial.AddCommEvent(commManager, playerFaction,
            "Issue a dock command by selecting the ship and the move command and clicking on the station. " +
            "We can also add a command to the start of the queue by holding alt, try it!", 10 * GetTimeScale());
        movementTutorial.AddCondition(EventCondition.DockShipsAtUnit(new List<Ship> { shuttle }, playerMiningStation, true));
        movementTutorial.AddCommEvent(commManager, playerFaction,
            "Great job, this concludes the movement practice for now.", 2 * GetTimeScale());
        movementTutorial.AddAction(AddResearchQuestLine);
    }

    void AddResearchQuestLine() {
        FactionCommManager commManager = playerFaction.GetFactionCommManager();
        Ship shuttle = playerFaction.ships.First(s => s.IsCivilianShip());
        GetBattleManager().SetSimulationTimeScale(10);
        FactionCommManager researchCommManager = researchFaction.GetFactionCommManager();

        EventChainBuilder builder = new EventChainBuilder();
        builder.AddCommEvent(researchFaction.GetFactionCommManager(), playerFaction,
            "Send your shuttle over to our research station station. " +
            "We have some science experiments to do.", 20);
        builder.AddAction(() => {
            eventManager.AddEvent(EventCondition.CommandDockShipToUnit(shuttle, researchStation, false), () => {
                commManager.SendCommunication(playerFaction,
                    "Our ship is on route! Remember that you can always use the [<, >, ?] keys to change the speed of time.", 5 * GetTimeScale());
            });
            commManager.SendCommunication(playerFaction,
                "The Research station is far away. You will have to scroll out and pan around to see it.", 20 * GetTimeScale());
        });
        builder.AddCondition(EventCondition.DockShipsAtUnit(new List<Ship>() { shuttle }, researchStation, true));
        builder.AddCommEvent(commManager, researchFaction,
            "Our shuttle has arrived at your research station. What would you like for us to do?", 2 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, playerFaction,
            "Good to see that you got here safely! \n " +
            "Our station was sent up as a combined reasearch initiative. " +
            "We were initially supported by most of the factions back on the planet. " +
            "However, now that tensions are building it is too risky for them to invest into research that could benefit some factions more than others.", 2 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, playerFaction,
            "We have found some interesting gas clouds farther from the sun and they may contain rare gases that could be usefull back on the planet. " +
            "Unfortunately we don't have the funding to construct a science ship with expensive equipment.", 30 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, playerFaction,
            "Our research could greatly help you and the rest of space exploration. \n" +
            "It would be a great help if you could help us out by sending your ship to the gas field that we have marked.", 20 * GetTimeScale());
        GasCloud targetGasCloud = researchFaction.GetClosestGasCloud(researchStation.GetPosition());
        builder.AddCondition(EventCondition.MoveShipToObject(shuttle, targetGasCloud, true));
        builder.AddAction(() => playerFaction.AddScience(100));
        builder.AddCommEvent(researchCommManager, playerFaction,
            "We have recieved some preliminary data about the gas. The high density of the gas cloud is spectacular! " +
            "There are some anomalies about it that we can't figure out with the small amount of equipment on your ship. \n" +
            "Were sending you our descoveries as well.", 5 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, playerFaction,
            "Could you bring the ship back to our station with a sample to farther analyze the gas?", 15 * GetTimeScale());
        builder.AddCondition(EventCondition.CommandDockShipToUnit(shuttle, researchStation, true));
        builder.AddAction(() => battleManager.SetSimulationTimeScale(1));
        builder.AddCommEvent(commManager, researchFaction,
            "No problem! We'll bring the gas back to the station.", 1 * GetTimeScale());
        builder.AddAction(() => {
            EventChainBuilder spendResearchChain = new EventChainBuilder();
            spendResearchChain.AddCommEvent(commManager, playerFaction,
                "We have converted the data that we recieved from " + researchFaction.name + ". \n" +
                "Lets spend this science, click the top left button to open the faction panel.", 3 * GetTimeScale());
            spendResearchChain.AddCondition(EventCondition.OpenFactionPanelEvent(playerFaction, true));
            spendResearchChain.AddCommEvent(commManager, playerFaction,
                "There are three research fields: Engineering, Electricity and Chemicals. " +
                "Each time science is put into a field it improves one of the areas assosiated with that field. \n" +
                "Try putting your science into one of the fields.", 2 * GetTimeScale());
            spendResearchChain.AddCondition(EventCondition.PredicateEvent((_) => playerFaction.Discoveries > 0));
            spendResearchChain.AddCommEvent(commManager, playerFaction,
                "Great Job! You can see which area was improved by scrolling through the improvements list. \n" +
                "The cost to research goes up each time. " +
                "Remember to check back when we get more sciecne!", 1 * GetTimeScale());
            spendResearchChain.Build(eventManager, () => battleManager.SetSimulationTimeScale(10))();
        });
        builder.AddCondition(EventCondition.DockShipsAtUnit(new List<Ship> { shuttle }, researchStation, true));
        builder.AddCommEvent(researchCommManager, playerFaction,
            "Thanks for the gas! We won't be needing your ship anytime soon so you are free to use it again." +
            "We'll need some time to analyse this gas, I'm sure it will be of use.", 3 * GetTimeScale());
        builder.AddCommEvent(commManager, researchFaction,
            "Sounds great! Let us know if you find anything interesting about the gas.", 5 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, playerFaction,
            "We have fully analysed the high density gas. " +
            "It seems like it could be used as a very efficient energy generation tool! " +
            "With a specialised reactor installed in our space ships we could last quite a while in deep space without much sunlight.", 500 * GetTimeScale());
        builder.AddAction(() => playerFaction.AddScience(200));
        builder.AddCommEvent(commManager, researchFaction,
            "Thats interesting, is there any way we could help collect it.", 5 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, playerFaction,
            "You could start by collecting the gas with a specialised gas collector ship. " +
            "We can then build generators in our stations to provide them with an abundance of energy.", 30 * GetTimeScale());
        builder.AddCommEvent(commManager, playerFaction,
            "Once we have enough energy credits open the menu of the " + shipyard.objectName + " to view the ship construction options.", 20 * GetTimeScale());
        builder.AddCondition(EventCondition.OpenObjectPanelEvent(shipyard, true));
        builder.AddCommEvent(commManager, playerFaction,
            "Now click on the ship blueprint named GasCollector to order the contract to build the ship.", 1 * GetTimeScale());
        builder.AddCondition(EventCondition.BuildShipAtStation(battleManager.shipBlueprints.First((b) => b.shipScriptableObject.shipType == Ship.ShipType.GasCollector), shipyard, true));
        Ship gasCollector = null;
        builder.AddAction(() => gasCollector = playerFaction.ships.First((s) => s.IsGasCollectorShip()));
        builder.AddCommEvent(researchCommManager, playerFaction,
            "I see that the " + shipyard.objectName + " has completed your request to construct a gas collection ship! " +
            "I'm sure the gas would be very helpfull for the people back on " + planet.objectName + ".", 10 * GetTimeScale());
        builder.AddCommEvent(commManager, playerFaction,
            "Select our gas colector ship in the shipyard, close the station menu and press E to select the gas collection command. " +
            "Then click on a gas cloud near our minning station to issue a command.", 25 * GetTimeScale());
        builder.AddCondition(EventCondition.LateConditionEvent(() => EventCondition.CommandShipToCollectGas(gasCollector, true)));
        builder.AddCommEvent(commManager, playerFaction,
            "Great job! Our transport ships will automatically take the gas collected from our mining station to sell at the trade station.", 3 * GetTimeScale());
        builder.AddCondition(EventCondition.WaitEvent(300 * GetTimeScale()));
        builder.AddCommEvent(researchCommManager, playerFaction,
            "We have designed a new ship blueprint that holds some specialized research equipment. " +
            "It would be a great help if you could build one for our research missions.");
        Ship researchShip = null;
        builder.AddCondition(EventCondition.LateConditionEvent(() => EventCondition.BuildShipAtStation(
            battleManager.shipBlueprints.First((b) => b.shipScriptableObject.shipType == Ship.ShipType.Research), shipyard, true)));
        builder.AddAction(() => researchShip = playerFaction.ships.First((s) => s.IsScienceShip()));
        builder.AddCommEvent(researchCommManager, playerFaction,
            "We heard that " + shipyardFaction.name + " finished your contract to build the research ship we requested! " +
            "Could you send the ship to investigate this wierd asteroid field?", 5 * GetTimeScale());
        builder.AddCondition(EventCondition.LateConditionEvent(() => EventCondition.MoveShipToObject(researchShip, battleManager.asteroidFields.Last(), true)));
        builder.AddCommEvent(researchCommManager, playerFaction,
            "...What is this?", 5 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, playerFaction,
            "...It looks like a ship???", 4 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, playerFaction,
            "An Alien ship!!!", 4 * GetTimeScale());
        builder.AddCommEvent(researchCommManager, playerFaction,
            "It looks like a very damaged long range exploration ship!", 4 * GetTimeScale());
        builder.Build(eventManager, researchCommManager, playerFaction,
            "We'd like to take a closer look, could you give us control of the research ship?", 
            new CommunicationEventOption[] {
                new CommunicationEventOption("Investigate", (communicationEvent) => { return true; }, (communicationEvent) => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    LocalPlayer.Instance.RemoveOwnedUnit(researchShip);
                    playerFaction.AddScience(300);
                    return true; 
                }) 
            }, 4 * GetTimeScale()
        )();
    }


    public override void UpdateController(float deltaTime) {
        base.UpdateController(deltaTime);
    }

    public override string GetPathToChapterFolder() {
        return "Campaign/Chapter1";
    }

    public BattleManager GetBattleManager() {
        return battleManager;
    }

    public void SetDifficultyLevel(DifficultyLevel level) {
        difficultyLevel = level;
    }
    protected float GetTimeScale() {
        return BattleManager.Instance.timeScale;
    }
}
