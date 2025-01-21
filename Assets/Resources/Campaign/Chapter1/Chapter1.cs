using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BattleManager;
using static CommunicationEvent;
using static Faction;
using Random = UnityEngine.Random;

public class Chapter1 : CampaingController {
    public Faction playerFaction { get; private set; }
    public PlayerFactionAI playerFactionAI { get; private set; }
    public MiningStation playerMiningStation { get; private set; }
    Faction otherMiningFaction;
    OtherMiningFactionAI otherMiningFactionAI;
    MiningStation otherMiningStation;
    Faction planetFaction;
    Faction planetEmpire;
    Faction planetDemocracy;
    Faction planetOligarchy;
    Faction pirateFaction;

    public PlanetFactionAI planetFactionAI { get; private set; }
    public Planet planet { get; private set; }
    public Planet moon { get; private set; }
    public Shipyard tradeStation { get; private set; }
    public Faction shipyardFaction { get; private set; }
    public ShipyardFactionAI shipyardFactionAI { get; private set; }
    public Shipyard shipyard { get; private set; }
    public Faction researchFaction { get; private set; }
    public Station researchStation { get; private set; }
    public Faction robotFaction { get; private set; }
    public RobotFactionAI robotFactionAI { get; private set; }
    Faction minorFactions;
    public Dictionary<CargoBay.CargoTypes, double> resourceCosts;
    ColorPicker colorPicker;

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
        colorPicker = new ColorPicker();
        playerFaction =
            battleManager.CreateNewFaction(
                new FactionData(typeof(PlayerFactionAI), "Free Space Miners", "FSM", colorPicker.PickColor(),
                    Random.Range(1, 2) * 5400, 0, 0, 0), new PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        playerFactionAI = (PlayerFactionAI)playerFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(playerFaction.GetPosition(), 100, 10000, 100, 300, 2),
                Random.Range(5, 10),
                10);
        }

        playerMiningStation = (MiningStation)battleManager.CreateNewStation(
            new BattleObject.BattleObjectData("Mining Station", playerFaction.GetPosition(), Random.Range(0, 360), playerFaction),
            Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/MiningStation"), false);


        otherMiningFaction =
            battleManager.CreateNewFaction(
                new FactionData(typeof(OtherMiningFactionAI), "Off-World Metal Industries", "OWM", colorPicker.PickColor(), 1000, 0, 0, 0),
                new PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        otherMiningFactionAI = (OtherMiningFactionAI)otherMiningFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(otherMiningFaction.GetPosition(), 0, 5000, 100, 1000, 2),
                Random.Range(5, 10), 10);
        }

        otherMiningStation = (MiningStation)battleManager.CreateNewStation(
            new BattleObject.BattleObjectData("Mining Station", otherMiningFaction.GetPosition(), Random.Range(0, 360), otherMiningFaction),
            Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/MiningStation"), true);
        otherMiningStation.BuildShip(Ship.ShipClass.Transport);
        otherMiningStation.LoadCargo(2400 * 3, CargoBay.CargoTypes.Metal);


        planetFaction =
            battleManager.CreateNewFaction(
                new FactionData(typeof(PlanetFactionAI), "World Space Union", "WSU", colorPicker.PickColor(), 100000, 0, 0, 0),
                new PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        planet = battleManager.CreateNewPlanet(new Planet.PlanetData(
                new BattleObject.BattleObjectData("Home", planetFaction.GetPosition(), Random.Range(0, 360), new Vector2(14, 14),
                    planetFaction),
                Random.Range(0.12f, 0.25f), Random.Range(0.18f, 0.25f), Random.Range(0.1f, 0.2f)),
            Resources.Load<PlanetScriptableObject>(GetPathToChapterFolder() + "/EarthPlanet"));
        moon = battleManager.CreateNewMoon(new Planet.PlanetData(
                new BattleObject.BattleObjectData("Moon", new PositionGiver(planetFaction.GetPosition(), 500, 50000, 300, 5000, 5),
                    Random.Range(0, 360), new Vector2(8, 8), planetFaction), 0, 0.02f, 0.98f),
            Resources.Load<PlanetScriptableObject>(GetPathToChapterFolder() + "/Moon"));
        tradeStation = (Shipyard)battleManager.CreateNewStation(
            new BattleObject.BattleObjectData("Trade Station",
                new PositionGiver(Vector2.MoveTowards(planet.GetPosition(), Vector2.zero, planet.GetSize() + 180), 0, 1000, 50, 200, 5),
                Random.Range(0, 360), planetFaction), Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/TradeStation"),
            true);
        tradeStation.LoadCargo(2400 * 5, CargoBay.CargoTypes.Metal);
        tradeStation.LoadCargo(2400, CargoBay.CargoTypes.Gas);
        ((ShipyardAI)tradeStation.stationAI).autoCollectCargo = false;
        tradeStation.GetConstructionBay().AddConstructionToBeginningQueue(new Ship.ShipConstructionBlueprint(planetFaction,
            battleManager.GetShipBlueprint(Ship.ShipType.Civilian), "Civilian Ship"));
        planetFactionAI = (PlanetFactionAI)planetFaction.GetFactionAI();
        tradeStation.stationAI.OnBuildShip += ship => {
            if (ship.faction == battleManager.GetLocalPlayer().faction) battleManager.GetLocalPlayer().AddOwnedUnit(ship);
        };

        shipyardFaction = battleManager.CreateNewFaction(
            new FactionData(typeof(ShipyardFactionAI), "Solar Shipyards", "SSH", colorPicker.PickColor(),
                (long)(2400 * resourceCosts[CargoBay.CargoTypes.Metal] * 1.4f), 0, 0, 0),
            new PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        shipyard = (Shipyard)battleManager.CreateNewStation(
            new BattleObject.BattleObjectData("Solar Shipyard", new PositionGiver(shipyardFaction.GetPosition()), Random.Range(0, 360),
                shipyardFaction), Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/Shipyard"), true);
        shipyard.LoadCargo(2400 * 4, CargoBay.CargoTypes.Gas);
        Ship shipyardTransport = shipyard.BuildShip(Ship.ShipClass.Transport);
        shipyardTransport.LoadCargo(2400, CargoBay.CargoTypes.Metal);
        shipyardFactionAI = (ShipyardFactionAI)shipyardFaction.GetFactionAI();
        shipyard.stationAI.OnBuildShip += ship => {
            if (ship.faction == battleManager.GetLocalPlayer().faction) battleManager.GetLocalPlayer().AddOwnedUnit(ship);
        };


        researchFaction =
            battleManager.CreateNewFaction(new FactionData("Frontier Research", "FRO", colorPicker.PickColor(), 3000, 36, 0, 0),
                new PositionGiver(Vector2.zero, 10000, 50000, 500, 5000, 2), 100);
        researchStation = battleManager.CreateNewStation(
            new BattleObject.BattleObjectData("Frontier Station", new PositionGiver(researchFaction.GetPosition()), Random.Range(0, 360),
                researchFaction), Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/ResearchStation"), true);

        playerMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        Fleet miningStationSetupFleet = playerFaction.CreateNewFleet("Station Setup Fleet",
            new HashSet<Ship>() {
                tradeStation.BuildShip(playerFaction, Ship.ShipClass.Transport),
                tradeStation.BuildShip(playerFaction, Ship.ShipClass.StationBuilder),
                tradeStation.BuildShip(playerFaction, Ship.ShipClass.Transport),
                tradeStation.BuildShip(playerFaction, Ship.ShipType.Civilian, "Shuttle")
            });
        miningStationSetupFleet.fleetAI.AddFleetAICommand(Command.CreateWaitCommand(4 * battleManager.timeScale),
            Command.CommandAction.Replace);
        miningStationSetupFleet.fleetAI.AddFormationTowardsPositionCommand(playerMiningStation.GetPosition(), shipyard.GetSize() * 4,
            Command.CommandAction.AddToEnd);
        miningStationSetupFleet.fleetAI.AddFleetAICommand(Command.CreateWaitCommand(3 * battleManager.timeScale));
        miningStationSetupFleet.fleetAI.AddFleetAICommand(Command.CreateMoveOffsetCommand(miningStationSetupFleet.GetPosition(),
            playerMiningStation.GetPosition(), playerMiningStation.GetSize() * 3));
        miningStationSetupFleet.fleetAI.AddFleetAICommand(Command.CreateBuildStationCommand(playerMiningStation));
        miningStationSetupFleet.fleetAI.AddFleetAICommand(Command.CreateDisbandFleetCommand());

        otherMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        otherMiningFaction.GetTransportShip(0).shipAI
            .AddUnitAICommand(Command.CreateWaitCommand(Random.Range(10, 20)), Command.CommandAction.AddToBegining);

        List<Ship> civilianShips = new List<Ship>();
        for (int i = 0; i < Random.Range(0, 2); i++) {
            civilianShips.Add(tradeStation.BuildShip(planetFaction,
                battleManager.GetShipBlueprint(Ship.ShipType.Civilian).shipScriptableObject, "Civilian"));
        }

        playerFactionAI.Setup(this, playerMiningStation);
        otherMiningFactionAI.Setup(this, shipyardFactionAI, otherMiningStation, tradeStation);
        planetFactionAI.Setup(this, shipyardFactionAI, planet, tradeStation, shipyard, civilianShips, eventManager);
        shipyardFactionAI.Setup(this, planetFactionAI, shipyard);


        int asteroidFieldCount = Random.Range(50, 80);
        for (int i = 0; i < asteroidFieldCount; i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(Vector2.zero, 1500, 100000, 20000, 300, 1), Random.Range(8, 10), 10);
        }

        int gasFieldCount = Random.Range(8, 14);
        for (int i = 0; i < gasFieldCount; i++) {
            battleManager.CreateNewGasCloud(new PositionGiver(Vector2.zero, 10000, 100000, 20000, 1000, 3));
        }

        pirateFaction =
            battleManager.CreateNewFaction(
                new FactionData(typeof(FactionAI), "Space Pirates", "SPR", colorPicker.PickColor(), 1000, 0, 0, 0),
                new PositionGiver(planet.position), 100);

        planet.AddFaction(planetFaction, Random.Range(0.05f, 0.1f), Random.Range(12, 35) * 1000000L, Random.Range(0.01f, 0.02f),
            "Increases space production");
        planetEmpire =
            battleManager.CreateNewFaction(new FactionData("Empire", "EMP", colorPicker.PickColor(), 1000000, 1000, 0, 0),
                new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(planetEmpire, Random.Range(0.20f, 0.35f), Random.Range(18, 24) * 100000000L, Random.Range(0.002f, 0.004f),
            "Increases unit production");
        planetDemocracy =
            battleManager.CreateNewFaction(new FactionData("Democracy", "DEM", colorPicker.PickColor(), 1000000, 1000, 0, 0),
                new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(planetDemocracy, Random.Range(0.30f, 0.40f), Random.Range(22, 36) * 100000000L, Random.Range(0.0014f, 0.003f),
            "Increases research rate");
        planetOligarchy =
            battleManager.CreateNewFaction(new FactionData("Oligarchy", "OLG", colorPicker.PickColor(), 1000000, 1000, 0, 0),
                new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(planetOligarchy, Random.Range(0.65f, 0.75f), Random.Range(12, 20) * 100000000L, Random.Range(0.0025f, 0.0035f),
            "Increases mining speed");
        minorFactions =
            battleManager.CreateNewFaction(new FactionData("Minor Factions", "MIN", colorPicker.PickColor(), 1000000, 1000, 0, 0),
                new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(minorFactions, Random.Range(0.90f, 0.99f), Random.Range(8, 14) * 100000000L, Random.Range(0.001f, 0.003f),
            "All base stats improved");

        battleManager.GetLocalPlayer().SetLockedUnits(true);
        battleManager.GetLocalPlayer().ownedUnits.Add(playerMiningStation);
        battleManager.GetLocalPlayer().SetFaction(playerFaction);
        eventManager.SetPlayerZoom(400);
        eventManager.StartFollowingUnit(miningStationSetupFleet.ships.First(s => s.IsConstructionShip()));

        battleManager.shipBlueprints.Where(b => b.shipScriptableObject.shipType == Ship.ShipType.Fighter ||
                b.shipScriptableObject.shipType == Ship.ShipType.Cruiser ||
                b.shipScriptableObject.shipType == Ship.ShipType.Dreadnaught).ToList()
            .ForEach(b => battleManager.shipBlueprints.Remove(b));

        StartTutorial();
        AddMoonQuestLine();
        AddPiratesEventLine();
        AddEasterEggs();
    }

    /// <summary>
    /// Handles the first part of the tutorial where the fleet is on the way to set up the station.
    /// This can be skipped by holding left shift
    /// </summary>
    private void StartTutorial() {
        // Increase time to skip tutorial
        bool skipTutorial = false;
        EventChainBuilder eventChain = new EventChainBuilder();
        eventChain.AddCondition(eventManager.CreateWaitCondition(1f));
        eventChain.AddAction(() => {
            // if (battleManager.GetLocalPlayer().GetLocalPlayerGameInput().AdditiveButtonPressed) {
            // GetBattleManager().SetSimulationTimeScale(10);
            // skipTutorial = true;
            // }
        });
        eventChain.Build(eventManager)();
        EventChainBuilder eventChain2 = new EventChainBuilder();
        eventChain2.AddCondition(eventManager.CreateWaitCondition(2f));
        eventChain2.AddAction(() => {
            // if (!skipTutorial && battleManager.GetLocalPlayer().GetLocalPlayerGameInput().AdditiveButtonPressed) {
            // GetBattleManager().SetSimulationTimeScale(10);
            // skipTutorial = true;
            // }
        });
        eventChain2.Build(eventManager)();
        EventChainBuilder eventChain3 = new EventChainBuilder();
        eventChain3.AddCondition(eventManager.CreateWaitCondition(5f));
        eventChain3.AddAction(() => {
            // if (!skipTutorial && battleManager.GetLocalPlayer().GetLocalPlayerGameInput().AdditiveButtonPressed) {
            // GetBattleManager().SetSimulationTimeScale(10);
            // skipTutorial = true;
            // }
        });
        eventChain3.Build(eventManager)();
        EventChainBuilder eventChain4 = new EventChainBuilder();
        eventChain4.AddCondition(eventManager.CreateWaitCondition(10f));
        eventChain4.AddAction(() => {
            // if (!skipTutorial && battleManager.GetLocalPlayer().GetLocalPlayerGameInput().AdditiveButtonPressed) {
            // GetBattleManager().SetSimulationTimeScale(10);
            // skipTutorial = true;
            // }
        });
        eventChain4.Build(eventManager)();


        planetFactionAI.faction.GetFactionCommManager().SendCommunication(new CommunicationEvent(playerFaction.GetFactionCommManager(),
            "Undocking procedure successful! \n You are now on route to the designated mining location. " +
            "As we planned, you will construct the mining station at the designated point (" +
            Mathf.RoundToInt(playerMiningStation.GetPosition().x) + ", " + Mathf.RoundToInt(playerMiningStation.GetPosition().y) +
            ") and begin operations.\nGood luck!",
            _ => {
                if (!skipTutorial) {
                    AddTutorial1();
                    return;
                }

                playerFaction.GetFactionCommManager().SendCommunication(playerFaction, "Skipping Tutorial", _ => {
                    GetBattleManager().SetSimulationTimeScale(playerFaction.fleets.First().fleetAI.GetTimeUntilFinishedWithCommand() / 5);
                    eventManager.AddEvent(eventManager.CreatePredicateCondition(_ => playerMiningStation.IsBuilt()), () => {
                        Ship shuttle = playerFaction.ships.First(s => s.IsCivilianShip());
                        if (battleManager.GetLocalPlayer().faction == playerFaction) {
                            battleManager.GetLocalPlayer().AddOwnedUnit(shuttle);
                        }

                        playerFactionAI.AddTradeRouteToStation(tradeStation);
                        playerFaction.AddCredits(10000000);
                        GetBattleManager().SetSimulationTimeScale(10);
                        AddResearchQuestLine();
                        AddWarEscalationEventLine();
                        playerMiningStation.moduleSystem.Get<CargoBay>().First().AddReservedCargoBays(CargoBay.CargoTypes.Metal, 2);
                        playerMiningStation.moduleSystem.Get<CargoBay>().First().AddReservedCargoBays(CargoBay.CargoTypes.Gas, 2);
                    });
                }, 20);
            }), 10 * GetTimeScale());
    }

    private void AddTutorial1() {
        Fleet setupFleet = playerFaction.fleets.First();
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();
        playerComm.SendCommunication(planetFactionAI.faction, "Thanks for the goodbye! We will send you some resources soon.", 5);
        EventChainBuilder eventChain = new EventChainBuilder();
        eventChain.AddCommEvent(playerComm, playerFaction,
            "We have started heading for the new mining site. \n" +
            "If I am talking too fast for you press the \"?\" key to pause and un-pause the game. " +
            "The [<, >] keys can also change how quickly the game time passes.", 15 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Lets review the controls while we are on route to the asteroid fields.", 15 * GetTimeScale());

        // Camera movement Tutorial
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Try clicking and holding your right mouse button and moving your mouse to pan the camera.", 7 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreatePanCondition(40));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Try pressing V to center your camera again, this can be helful if you get lost.", 2 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Now scroll out to view more of the solar system.", 7 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateZoomCondition(2000));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Great job! As you can see our ships appear with a green icon when zoomed out, meaning that we own them but can't control them. " +
            "Neutral units will appear grey and hostile units will appear red.");
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Now zoom back in to our ships so we can see them better.", 15 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateZoomCondition(300));

        // Selection Tutorial
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Well done! Now lets try selecting the ships.", 2 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Our ships are in a fleet, which means when you click a ship you will select the fleet by default.", 5 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Try clicking on of the ships to select our fleet.", 8 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateSelectFleetCondition(playerFaction.fleets.First(), true));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Now try selecting just one ship in the fleet.", 1 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Hold alt while clicking a ship to select it.", 3 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateSelectUnitsAmountCondition(setupFleet.ships.Cast<Unit>().ToList(), 1, true));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "You can see a line coming out of the selected ship, this shows where their current command is going.", 1 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "If you want to select more units you can hold shift while clicking it to add it to our current selection.",
            7 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Try adding another ship to our current selection.", 9 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateSelectUnitsAmountCondition(setupFleet.ships.Cast<Unit>().ToList(), 2, true));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Exelent! Now deselect the ships by clicking on empty space or by pressing D.", 1 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateUnselectUnitsCondition(battleManager.units.ToList(), false));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "There is one more way that you can select ships. " +
            "Try holding alt then click and dragging your mouse to do a box selection until it contains a few of the ships.",
            1 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateSelectUnitsAmountCondition(setupFleet.ships.Cast<Unit>().ToList(), 2, true));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Great job! Now try right clicking on the biggest ship to view its stats.", 1 * GetTimeScale());
        eventChain.AddCondition(
            eventManager.CreateOpenObjectPanelCondition(setupFleet.ships.First(ship => ship.IsConstructionShip()), true));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Here you can see its owner, state, cargo and weapons of the unit. ", 1 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Right click again or press the close button to close the panel.", 4 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateOpenObjectPanelCondition(null, false));

        // Following Tutorial
        eventChain.AddCommEvent(playerComm, playerFaction,
            "We are currently following a ship in our fleet to keep it visible. " +
            "Press B to unfollow the ship in our fleet.", 2 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateFollowUnitCondition(null));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "As you can see, our ships are moving without the camera now. " +
            "You can always follow a ship again by selecting it and pressing B.", 3 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Zoom all the way out to view more of the solar system.", 12 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateZoomCondition(30000));
        eventChain.AddCommEvent(playerComm, playerFaction,
            "You can barely see the stations, planet, the many asteroid fields and gas clouds. " +
            "Our mining team is currently heading to a particularly dense asteroid field to mine.", 2 * GetTimeScale());
        eventChain.AddCommEvent(playerComm, playerFaction,
            "Zoom in and right click on the planet to view the political state. This is our home.", 6 * GetTimeScale());
        eventChain.AddCondition(eventManager.CreateOpenObjectPanelCondition(planet, true));
        eventChain.AddCommEvent(playerComm, playerFaction,
        "Here you can see the various factions on the planet, their territory and forces.", 5 * GetTimeScale());
        eventChain.Build(eventManager, playerComm, playerFaction,
            "What difficulty would you like to play at? Harder difficulties will have a faster intro scene.",
            new CommunicationEventOption[] {
                new CommunicationEventOption("Easy", _ => { return true; }, communicationEvent => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    SetDifficultyLevel(DifficultyLevel.Easy);
                    AddTutorial2();
                    return true;
                }),
                new CommunicationEventOption("Normal", _ => { return true; }, communicationEvent => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    SetDifficultyLevel(DifficultyLevel.Normal);
                    AddTutorial2();
                    return true;
                }),
                new CommunicationEventOption("Hard", _ => { return true; }, communicationEvent => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    SetDifficultyLevel(DifficultyLevel.Hard);
                    AddTutorial2();
                    return true;
                })
            }, 10 * GetTimeScale())(); // Don't forget the last set of parenthesis to call the function.
    }

    void AddTutorial2() {
        FactionCommManager commManager = playerFaction.GetFactionCommManager();
        GetBattleManager().SetSimulationTimeScale(playerFaction.fleets.First().fleetAI.GetTimeUntilFinishedWithCommand() / (120 + 40));
        EventChainBuilder eventChainBuilder = new EventChainBuilder();
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "As you might already know, resources on our planet are sparse due to the slow development of resource reusing policy and climate change. " +
            "The impact of the resource crisis is starting to build tension between the major nations. " +
            "Luckily our space instillations are independent of any nation so there shouldn't be any space wars out here.",
            5 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "Overpopulation has ignited an effort to colonize other planets in the system. " +
            "It is a long way off however, to start we have been developing the first moon colony. " +
            "We will see how well it works out.", 35 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "Space technology is relatively new and we are starting to harvest asteroid fields to help solve our resource problems back at " +
            planet.objectName + ".\n" +
            "However there is some worry that not even all the metal from this solar system will support our consumption.",
            20 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "We haven't figured out how to travel to other solar systems yet, " +
            "it might take a hundred years or so until, if it is even possible. " +
            "We have an advanced space research station far out in the solar system working on this.", 15 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "Now that we have some general-purpouse space ship designs in production at our shipyard we expect a boom in civilian space travel.",
            15 * GetTimeScale());
        eventChainBuilder.AddCommEvent(commManager, playerFaction,
            "AI technology is also on the rise and may aid us in space exploration." +
            "But like all things, recent research has been limited by the resource crisis.",
            15 * GetTimeScale());
        eventChainBuilder.Build(eventManager, () => {
            commManager.SendCommunication(planetFactionAI.faction, "We are about to arrive at our destination!");
            commManager.SendCommunication(playerFaction,
                "Thats it until until we reach the mining site. \n " +
                "Remember that you can press the [<, >, ?] keys to change how quickly the game time passes. \n ", 15 * GetTimeScale());
            commManager.SendCommunication(playerFaction,
                "In the mean time feel free to click the \"Controls help\" button in the top right and read the controls.",
                15 * GetTimeScale());
            eventManager.AddEvent(eventManager.CreatePredicateCondition(_ => playerMiningStation.IsBuilt()),
                () => {
                    AddStationTutorial();
                    playerMiningStation.moduleSystem.Get<CargoBay>().First().AddReservedCargoBays(CargoBay.CargoTypes.Metal, 2);
                    playerMiningStation.moduleSystem.Get<CargoBay>().First().AddReservedCargoBays(CargoBay.CargoTypes.Gas, 2);
                });
        })();
    }

    void AddStationTutorial() {
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();
        GetBattleManager().SetSimulationTimeScale(1);
        Ship shuttle = playerFaction.ships.First(s => s.IsCivilianShip());
        if (battleManager.GetLocalPlayer().faction == playerFaction) battleManager.GetLocalPlayer().AddOwnedUnit(shuttle);

        EventChainBuilder movementTutorial = new EventChainBuilder();
        playerComm.SendCommunication(new CommunicationEvent(planetFactionAI.faction.GetFactionCommManager(),
            "We have arrived safely at the destination and are setting up our operations.",
            new CommunicationEventOption[] {
                new CommunicationEventOption("Trade Metal", _ => { return true; },
                    communicationEvent => {
                        if (!communicationEvent.isActive)
                            return false;
                        playerFactionAI.AddTradeRouteToStation(tradeStation);
                        communicationEvent.DeactivateEvent();
                        shipyardFaction.GetFactionCommManager().SendCommunication(playerFaction,
                            "Good to see that you are set up and everything is going well. " +
                            "We are setting up a trade route for you. " +
                            "We will give you resources to operate your station in return for metal.",
                            _ => movementTutorial.Build(eventManager)(), 3 * GetTimeScale());
                        return true;
                    })
            }, true), 2 * GetTimeScale());
        movementTutorial.AddCommEvent(playerComm, playerFaction,
            "Lets learn about ship movement and investigate the nearby asteroid fields. ", 5 * GetTimeScale());
        movementTutorial.AddCommEvent(playerComm, playerFaction,
            "Open the mining station menue by right clicking on it.", 4 * GetTimeScale());
        movementTutorial.AddCondition(eventManager.CreateOpenObjectPanelCondition(playerMiningStation, true));
        movementTutorial.AddCommEvent(playerComm, playerFaction,
            "Now click on the button named \"Shuttle\" in the hanger to select it and close the station menu.", 1 * GetTimeScale());
        movementTutorial.AddCondition(eventManager.CreateSelectUnitCondition(shuttle, true));
        movementTutorial.AddCondition(eventManager.CreateOpenObjectPanelCondition(null, true));
        movementTutorial.AddCondition(eventManager.CreateSelectUnitCondition(shuttle, true));
        movementTutorial.AddCommEvent(playerComm, playerFaction,
            "Now press Q and click on the asteroid field highlighted nearby to issue a move command to it.");
        List<AsteroidField> closestAsteroidFields = battleManager.asteroidFields.ToList()
            .OrderBy(a => Vector2.Distance(shuttle.GetPosition(), a.GetPosition())).ToList();
        movementTutorial.AddCondition(eventManager.CreateCommandMoveShipToObject(shuttle, closestAsteroidFields.First(), true));
        movementTutorial.AddCommEvent(playerComm, playerFaction,
            "I'll speed up the time for you.", 1 * GetTimeScale());
        movementTutorial.AddAction(() => battleManager.SetSimulationTimeScale(10));
        movementTutorial.AddCondition(eventManager.CreateMoveShipToObject(shuttle, closestAsteroidFields.First(), 30, true));
        // Make sure the ship isn't moving
        movementTutorial.AddCondition(eventManager.CreateWaitUntilShipsIdle(new List<Ship> { shuttle }));
        movementTutorial.AddAction(() => battleManager.SetSimulationTimeScale(1));
        movementTutorial.AddCommEvent(playerComm, playerFaction,
            "Lets survey some more asteroid fields. " +
            "Hold shift while issuing a move command to add the command to the ships command queue. " +
            "Try queueing some more movement commands to the asteroid fields highlighted.");
        movementTutorial.AddCondition(eventManager.CreateCommandMoveShipToObjects(shuttle,
            closestAsteroidFields.GetRange(1, 4).Cast<IObject>().ToList(), true));
        movementTutorial.AddAction(() => battleManager.SetSimulationTimeScale(10));
        movementTutorial.AddCommEvent(playerComm, playerFaction,
            "Now try docking to the station by pressing Q issuing a move command and clicking on the mining station. " +
            "We can also add a command to the start of the queue by holding alt while issuing the command. " +
            "Try it!", 10 * GetTimeScale());
        movementTutorial.AddCondition(eventManager.CreateDockShipsAtUnit(new List<Ship> { shuttle }, playerMiningStation, true));
        movementTutorial.AddCommEvent(playerComm, playerFaction,
            "Great job, this concludes the movement practice.", 2 * GetTimeScale());
        movementTutorial.AddAction(AddResearchQuestLine);
        movementTutorial.AddAction(AddWarEscalationEventLine);
    }

    void AddResearchQuestLine() {
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();
        Ship shuttle = playerFaction.ships.First(s => s.IsCivilianShip());
        GetBattleManager().SetSimulationTimeScale(10);
        FactionCommManager researchCommManager = researchFaction.GetFactionCommManager();

        EventChainBuilder researchChain = new EventChainBuilder();
        researchChain.AddCommEvent(researchFaction.GetFactionCommManager(), playerFaction,
            "Hello there, this is " + researchCommManager.GetSenderName() + ". " +
            "I'm the lead reasearcher of the " + researchStation.objectName + ".", 5 * GetTimeScale());
        researchChain.AddCommEvent(researchFaction.GetFactionCommManager(), playerFaction,
            "I see that your mining operations are all set up. ", 7 * GetTimeScale());
        researchChain.AddCommEvent(researchFaction.GetFactionCommManager(), playerFaction,
            "We could use your help investigating some resources that might play a role in interstellar travel. " +
            "Could you send your shuttle over to our research station station to help us out with some?", 5 * GetTimeScale());
        researchChain.AddAction(() => {
            eventManager.AddEvent(eventManager.CreateCommandDockShipToUnit(shuttle, researchStation, false), () => {
                playerComm.SendCommunication(playerFaction,
                    "Our ship is on route to the " + researchStation.objectName +
                    "! Remember that you can always use the [<, >, ?] keys to change the speed of time.",
                    5 * GetTimeScale());
            });
            playerComm.SendCommunication(playerFaction,
                "The Research station was built far away to experiment with minimal gravity from the sun. " +
                "You will have to scroll out and pan around to see it.", 20 * GetTimeScale());
        });
        researchChain.AddCondition(eventManager.CreateDockShipAtUnit(shuttle, researchStation, true));
        researchChain.AddCommEvent(playerComm, researchFaction,
            "Our shuttle has arrived at your research station. What would you like for us to do?", 2 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "Good to see that you got here safely! \n " +
            "Our station was sent up as a combined reasearch initiative to investigate possibilities for interstellar travel. ",
            2 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "We were initially supported by most of the factions back on the planet. " +
            "However, now that tensions are building it is too risky for them to invest into research that could benefit some factions more than others.",
            16 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "We have found some interesting gas clouds farther from the sun and they may contain low-density gases that could be usefull back on the planet. " +
            "Unfortunately we don't have the funding to construct a science ship with expensive equipment.", 16 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "It would be a great help if you could send your ship to the gas field that we have marked.",
            18 * GetTimeScale());
        GasCloud targetGasCloud = researchFaction.GetClosestGasCloud(researchStation.GetPosition());
        researchChain.AddCondition(eventManager.CreateMoveShipToObject(shuttle, targetGasCloud, 10, true));
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "We have recieved some preliminary data about the gas. The low-density of the gas cloud is spectacular! " +
            "There are some anomalies about it that we can't figure out with the small amount of equipment on your ship.",
            3 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "Could you bring the ship back to our station with a sample to farther analyze the gas?", 15 * GetTimeScale());
        researchChain.AddCondition(eventManager.CreateCommandDockShipToUnit(shuttle, researchStation, true));
        researchChain.AddAction(() => battleManager.SetSimulationTimeScale(1));
        researchChain.AddCommEvent(playerComm, researchFaction,
            "No problem! We'll bring the gas back to the station.", 1 * GetTimeScale());
        researchChain.AddAction(() => {
            EventChainBuilder spendResearchChain = new EventChainBuilder();
            spendResearchChain.AddCommEvent(playerComm, playerFaction,
                "Here's some of the data that you collected.", 3 * GetTimeScale());
            spendResearchChain.AddAction(() => playerFaction.AddScience(100));
            spendResearchChain.AddCommEvent(playerComm, playerFaction,
                "We have processed the data that we recieved from " + researchFaction.name + " as science. \n" +
                "Lets put it to use. Click the top left button to open the faction panel.", 5 * GetTimeScale());
            spendResearchChain.AddCondition(eventManager.CreateOpenFactionPanelCondition(playerFaction, true));
            spendResearchChain.AddCommEvent(playerComm, playerFaction,
                "There are three research fields: Engineering, Electricity and Chemicals. " +
                "Each time science is put into a field it improves one of the areas assosiated with that field. \n" +
                "Try putting your science into one of the fields.", 2 * GetTimeScale());
            spendResearchChain.AddCondition(eventManager.CreatePredicateCondition(_ => playerFaction.Discoveries > 0));
            spendResearchChain.AddCommEvent(playerComm, playerFaction,
                "Great Job! You can see which area was improved by scrolling through the improvements list. \n" +
                "The cost to research goes up each time. " +
                "Remember to check back when we get more sciecne!", 1 * GetTimeScale());
            spendResearchChain.Build(eventManager, () => battleManager.SetSimulationTimeScale(10))();
        });
        researchChain.AddCondition(eventManager.CreateDockShipsAtUnit(new List<Ship> { shuttle }, researchStation, true));
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "Thanks for the gas! We won't be needing your ship anytime soon so you are free to use it again. " +
            "We'll need some time to analyse this gas, I'm sure it will be of use.", 3 * GetTimeScale());
        researchChain.AddCommEvent(playerComm, researchFaction,
            "Sounds great! Let us know if you find anything interesting about the gas.", 5 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "We have fully analysed the high density gas. " +
            "It seems like it could be used as a very efficient energy generation tool! " +
            "With a specialised reactor installed in our space ships we could last quite a while in deep space without much sunlight.",
            500 * GetTimeScale());
        researchChain.AddAction(() => playerFaction.AddScience(200));
        researchChain.AddCommEvent(playerComm, researchFaction,
            "Thats interesting, is there any way we could help collect it.", 5 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "You could start by collecting the gas with a specialised gas collector ship. " +
            "We can then build generators in our stations to provide them with an abundance of energy.", 30 * GetTimeScale());
        researchChain.AddCommEvent(playerComm, playerFaction,
            "Once we have enough energy credits open the menu of the " + shipyard.objectName + " to view the ship construction options.",
            20 * GetTimeScale());
        researchChain.AddCondition(eventManager.CreateOpenObjectPanelCondition(shipyard, true));
        researchChain.AddCommEvent(playerComm, playerFaction,
            "Now click on the ship blueprint named GasCollector to order the contract to build the ship.", 1 * GetTimeScale());
        researchChain.AddCondition(eventManager.CreateBuildShipAtStation(
            battleManager.shipBlueprints.First(b => b.shipScriptableObject.shipType == Ship.ShipType.GasCollector), playerFaction,
            shipyard, true));
        Ship gasCollector = null;
        researchChain.AddAction(() => gasCollector = playerFaction.ships.First(s => s.IsGasCollectorShip()));
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "I see that the " + shipyard.objectName + " has completed your request to construct a gas collection ship! " +
            "I'm sure the gas would be very helpfull for the people back on " + planet.objectName + ".", 10 * GetTimeScale());
        researchChain.AddCommEvent(playerComm, playerFaction,
            "Select our gas colector ship in the shipyard, close the station menu and press E to select the gas collection command. " +
            "Then click on a gas cloud near our mining station to issue a command.", 25 * GetTimeScale());
        researchChain.AddCondition(eventManager.CreateLateCondition(() =>
            eventManager.CreateCommandShipToCollectGas(gasCollector, null, null, true)));
        researchChain.AddCommEvent(playerComm, playerFaction,
            "Great job! Our transport ships will automatically take the gas collected from our mining station to sell at the trade station.",
            3 * GetTimeScale());
        researchChain.AddCondition(eventManager.CreateWaitCondition(300 * GetTimeScale()));
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "We have designed a new ship blueprint that holds some specialized research equipment. " +
            "It would be a great help if you could build one for our research missions.");
        Ship researchShip = null;
        researchChain.AddCondition(eventManager.CreateLateCondition(() => eventManager.CreateBuildShipAtStation(
            battleManager.shipBlueprints.First(b => b.shipScriptableObject.shipType == Ship.ShipType.Research), playerFaction, shipyard,
            true)));
        researchChain.AddAction(() => researchShip = playerFaction.ships.First(s => s.IsScienceShip()));
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "We heard that " + shipyardFaction.name + " finished your contract to build the research ship we requested! " +
            "Could you send the ship to investigate this wierd asteroid field?", 5 * GetTimeScale());
        researchChain.AddCondition(eventManager.CreateLateCondition(() =>
            eventManager.CreateMoveShipToObject(researchShip, battleManager.asteroidFields.Last(), 30, true)));
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "...What is this?", 5 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "...It looks like a ship???", 4 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "An Alien ship!!!", 4 * GetTimeScale());
        researchChain.AddCommEvent(researchCommManager, playerFaction,
            "It looks like a very damaged long range exploration ship!", 4 * GetTimeScale());
        researchChain.Build(eventManager, researchCommManager, playerFaction,
            "We'd like to take a closer look, could you give us control of the research ship?",
            new CommunicationEventOption[] {
                new CommunicationEventOption("Investigate", _ => { return true; }, communicationEvent => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    playerFaction.TransferShipTo(researchShip, researchFaction);
                    playerFaction.AddScience(300);
                    AddResearchInvestigationQuestLine(researchShip)();
                    return true;
                })
            }, 4 * GetTimeScale()
        )();
    }

    private Action AddResearchInvestigationQuestLine(Ship researchShip) {
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();
        FactionCommManager researchCommManager = researchFaction.GetFactionCommManager();

        EventChainBuilder investigateChain = new EventChainBuilder();
        investigateChain.AddCommEvent(researchCommManager, playerFaction,
            "This ship is quite advanced. How did it get here?", GetTimeScale() * 5);
        investigateChain.AddCommEvent(researchCommManager, playerFaction,
            "Could this be it?", GetTimeScale() * 5);
        investigateChain.AddCommEvent(researchCommManager, playerFaction,
            "The ship has a component that looks an awfull lot like a hyperdrive.\n" +
            "We'll need some time to investigate it further", GetTimeScale() * 5);
        investigateChain.AddCondition(eventManager.CreateWaitCondition(GetTimeScale() * 100));
        investigateChain.AddAction(() => { playerFaction.AddScience(200); });
        investigateChain.AddCondition(eventManager.CreateWaitCondition(GetTimeScale() * 100));
        investigateChain.AddAction(() => { playerFaction.AddScience(200); });
        investigateChain.AddCommEvent(researchCommManager, playerFaction,
            "We were able to analyze the hyperdrive! " +
            "With a few repairs it is likely to be functional again.", GetTimeScale() * 5);
        return investigateChain.Build(eventManager, researchCommManager, playerFaction,
            "Do we have your permission to install the old hyperdrive onto this ship for an experiment? " +
            "I can't guarantee that it will be able to come back.",
            new CommunicationEventOption[] {
                new CommunicationEventOption("Donate Ship", _ => { return true; }, communicationEvent => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    playerComm.SendCommunication(researchFaction,
                        "Sure! I hope the experiment goes well.");
                    AddResearchDonateQuestLine(researchShip)();
                    return true;
                }),
                new CommunicationEventOption("Keep Ship", _ => { return true; }, communicationEvent => {
                    if (!communicationEvent.isActive)
                        return false;
                    communicationEvent.DeactivateEvent();
                    playerComm.SendCommunication(researchFaction,
                        "We would like to keep the ship if thats fine with you.");
                    AddResearchKeepQuestLine(researchShip)();
                    return true;
                })
            }, 4 * GetTimeScale()
        );
    }

    private Action AddResearchDonateQuestLine(Ship researchShip) {
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();
        FactionCommManager researchCommManager = researchFaction.GetFactionCommManager();

        EventChainBuilder donateChain = new EventChainBuilder();
        donateChain.AddCommEvent(researchCommManager, playerFaction,
            "Thank you! We are installing the hyperdrive now, it will take a bit.", GetTimeScale() * 3);
        donateChain.AddButtonCommEvent(researchCommManager, playerFaction,
            "We are almost ready to test the huperdrive. On your mark.", "Activate", GetTimeScale() * 10);
        donateChain.AddCommEvent(playerComm, researchFaction,
            "Activate the hyperdrive!");
        donateChain.AddAction(() => {
            researchShip.Explode();
            playerFaction.AddScience(500);
        });
        donateChain.AddCommEvent(researchCommManager, playerFaction,
            "It worked!", GetTimeScale() * 6);
        donateChain.AddCommEvent(researchCommManager, playerFaction,
            "I guess we have to wait for them to come back, if they can.", GetTimeScale() * 5);
        return donateChain.Build(eventManager, () => EscapeShipQuestLine(true));
    }

    private Action AddResearchKeepQuestLine(Ship researchShip) {
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();
        FactionCommManager researchCommManager = researchFaction.GetFactionCommManager();

        EventChainBuilder keepChain = new EventChainBuilder();
        keepChain.AddCommEvent(researchCommManager, playerFaction,
            "Okay, we will do as much research as we can with the information we obtained.", GetTimeScale() * 3);
        keepChain.AddCommEvent(researchCommManager, playerFaction,
            "We will give you control of the ship again once we take the device back to " + researchStation.objectName + ".",
            GetTimeScale() * 14);
        keepChain.AddAction(() => {
            researchShip.shipAI.AddUnitAICommand(Command.CreateDockCommand(researchStation), Command.CommandAction.Replace);
        });
        keepChain.AddCondition(eventManager.CreateDockShipsAtUnit(new List<Ship>() { researchShip }, researchStation));
        keepChain.AddCommEvent(researchCommManager, playerFaction,
            "We have recovered the hyperdrive and are giving you back control of your research ship.", GetTimeScale() * 3);
        keepChain.AddCommEvent(playerComm, researchFaction,
            "Thanks! Hopefully you can make good use of it!", GetTimeScale() * 3);
        keepChain.AddAction(() => {
            researchFaction.TransferShipTo(researchShip, playerFaction);
            battleManager.GetLocalPlayer().AddOwnedUnit(researchShip);
        });
        return keepChain.Build(eventManager, () => EscapeShipQuestLine(false));
    }

    private void EscapeShipQuestLine(bool donatedShip) {
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();
        FactionCommManager researchCommManager = researchFaction.GetFactionCommManager();
        FactionCommManager shipyardCommManager = shipyardFaction.GetFactionCommManager();
        FactionCommManager planetCommManager = planetFaction.GetFactionCommManager();

        EventChainBuilder buildEscapeShipChain = new EventChainBuilder();
        if (donatedShip) {
            buildEscapeShipChain.AddCondition(eventManager.CreateWaitCondition(GetTimeScale() * 100));
        } else {
            buildEscapeShipChain.AddCondition(eventManager.CreateWaitCondition(GetTimeScale() * 200));
        }

        buildEscapeShipChain.AddCommEvent(planetCommManager, shipyardFaction,
            $"I have a message for {shipyardFaction.name}, {shipyardFaction.name}, {researchFaction.name}, {playerFaction.name}.");
        buildEscapeShipChain.AddCommEvent(planetCommManager, researchFaction,
            $"I have a message for {shipyardFaction.name}, {shipyardFaction.name}, {researchFaction.name}, {playerFaction.name}.");
        buildEscapeShipChain.AddCommEvent(planetCommManager, playerFaction,
            $"I have a message for {shipyardFaction.name}, {shipyardFaction.name}, {researchFaction.name}, {playerFaction.name}.");

        buildEscapeShipChain.AddCommEvent(planetCommManager, shipyardFaction,
            "I think it is about time to develop a ship capable of colonizing plants in other solar systems.", GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(planetCommManager, researchFaction,
            "I think it is about time to develop a ship capable of colonizing plants in other solar systems.");
        buildEscapeShipChain.AddCommEvent(planetCommManager, playerFaction,
            "I think it is about time to develop a ship capable of colonizing plants in other solar systems.");

        buildEscapeShipChain.AddCommEvent(planetCommManager, shipyardFaction,
            "We need a way to progress foward in these hard times, hopefully not as a last resort.", GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(planetCommManager, researchFaction,
            "We need a way to progress foward in these hard times, hopefully not as a last resort.");
        buildEscapeShipChain.AddCommEvent(planetCommManager, playerFaction,
            "We need a way to progress foward in these hard times, hopefully not as a last resort.");

        buildEscapeShipChain.AddCommEvent(planetCommManager, shipyardFaction,
            "This is a project that can only be done with all of us working together.", GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(planetCommManager, researchFaction,
            "This is a project that can only be done with all of us working together.");
        buildEscapeShipChain.AddCommEvent(planetCommManager, playerFaction,
            "This is a project that can only be done with all of us working together.");

        buildEscapeShipChain.AddCommEvent(planetCommManager, shipyardFaction,
            $"{researchFaction.name} has discovered an interstellar technology that will can take us to the stars.", GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(planetCommManager, researchFaction,
            $"{researchFaction.name} has discovered an interstellar technology that will can take us to the stars.");
        buildEscapeShipChain.AddCommEvent(planetCommManager, playerFaction,
            $"{researchFaction.name} has discovered an interstellar technology that will can take us to the stars.");

        buildEscapeShipChain.AddCommEvent(planetCommManager, shipyardFaction,
            $"{shipyardFaction.name} has the capability to construct a ship that can withstand the travel and the uknowns that lie before us.",
            GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(planetCommManager, researchFaction,
            $"{shipyardFaction.name} has the capability to construct a ship that can withstand the travel and the uknowns that lie before us.");
        buildEscapeShipChain.AddCommEvent(planetCommManager, playerFaction,
            $"{shipyardFaction.name} has the capability to construct a ship that can withstand the travel and the uknowns that lie before us.");

        buildEscapeShipChain.AddCommEvent(planetCommManager, shipyardFaction,
            $"And {playerFaction.name} has the great amount of resources needed to produce such a ship.", GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(planetCommManager, researchFaction,
            $"And {playerFaction.name} has the great amount of resources needed to produce such a ship.");
        buildEscapeShipChain.AddCommEvent(planetCommManager, playerFaction,
            $"And {playerFaction.name} has the great amount of resources needed to produce such a ship.");

        if (donatedShip) {
            buildEscapeShipChain.AddCommEvent(researchCommManager, planetFaction,
                "From the data we collected of the hyperDrive test we believe that replicating the hyperdrive is possible given the time.",
                GetTimeScale() * 12);
            buildEscapeShipChain.AddCommEvent(researchCommManager, shipyardFaction,
                "From the data we collected of the hyperDrive test we believe that replicating the hyperdrive is possible given the time.");
            buildEscapeShipChain.AddCommEvent(researchCommManager, playerFaction,
                "From the data we collected of the hyperDrive test we believe that replicating the hyperdrive is possible given the time.");
        } else {
            buildEscapeShipChain.AddCommEvent(researchCommManager, planetFaction,
                "We can try installing the hyperdrive into the colonizer ship, but we can't guarantee that it will function reliably.",
                GetTimeScale() * 12);
            buildEscapeShipChain.AddCommEvent(researchCommManager, shipyardFaction,
                "We can try installing the hyperdrive into the colonizer ship, but we can't guarantee that it will function reliably.");
            buildEscapeShipChain.AddCommEvent(researchCommManager, playerFaction,
                "We can try installing the hyperdrive into the colonizer ship, but we can't guarantee that it will function reliably.");
        }

        buildEscapeShipChain.AddCommEvent(researchCommManager, planetFaction,
            $"The hyperdrive will take a lot of energy to make and produce. So we will need {playerFaction.name}'s help to keep us powered.",
            GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(researchCommManager, shipyardFaction,
            $"The hyperdrive will take a lot of energy to make and produce. So we will need {playerFaction.name}'s help to keep us powered.");
        buildEscapeShipChain.AddCommEvent(researchCommManager, playerFaction,
            $"The hyperdrive will take a lot of energy to make and produce. So we will need {playerFaction.name}'s help to keep us powered.");


        buildEscapeShipChain.AddCommEvent(shipyardCommManager, planetFaction,
            "Our shipyard can be converted to produce this large colonization ship, however we will need to modify our construction bays for it's size.",
            GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, researchFaction,
            "Our shipyard can be converted to produce this large colonization ship, however we will need to modify our construction bays for it's size.");
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, playerFaction,
            "Our shipyard can be converted to produce this large colonization ship, however we will need to modify our construction bays for it's size.");


        buildEscapeShipChain.AddCommEvent(shipyardCommManager, planetFaction,
            "We need a lot of metal and a some gas to produce the colonizer, the small shipments from the planet won't suffice.",
            GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, researchFaction,
            "We need a lot of metal and a some gas to produce the colonizer, the small shipments from the planet won't suffice.");
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, playerFaction,
            "We need a lot of metal and a some gas to produce the colonizer, the small shipments from the planet won't suffice.");

        buildEscapeShipChain.AddCommEvent(playerComm, planetFaction,
            "Seems like the resources that we collect will be quite usefull. We'll try to send you what we can.", GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(playerComm, shipyardFaction,
            "Seems like the resources that we collect will be quite usefull. We'll try to send you what we can.");
        buildEscapeShipChain.AddCommEvent(playerComm, researchFaction,
            "Seems like the resources that we collect will be quite usefull. We'll try to send you what we can.");

        buildEscapeShipChain.AddCommEvent(planetCommManager, shipyardFaction,
            $"Sounds great! Lets try and build it quickly, the siutation on {planet.objectName} might change.", GetTimeScale() * 12);
        buildEscapeShipChain.AddCommEvent(planetCommManager, researchFaction,
            $"Sounds great! Lets try and build it quickly, the siutation on {planet.objectName} might change");
        buildEscapeShipChain.AddCommEvent(planetCommManager, playerFaction,
            $"Sounds great! Lets try and build it quickly, the siutation on {planet.objectName} might change");

        buildEscapeShipChain.AddCommEvent(playerComm, playerFaction,
            "Build the Colonizer ship at the shipyard.", GetTimeScale() * 16);

        buildEscapeShipChain.AddCondition(eventManager.CreateBuildShipAtStation(
            battleManager.shipBlueprints.First(s => s.shipScriptableObject.shipType == Ship.ShipType.Colonizer), playerFaction, shipyard,
            true));
        Ship colonizer = null;
        buildEscapeShipChain.AddAction(() => colonizer = playerFaction.ships.First(s => s.GetShipType() == Ship.ShipType.Colonizer));
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, planetFaction,
            "We have finished building the colonizer ship. We'll need to bring it over to the research station to install the hyperdrive.",
            GetTimeScale() * 3);
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, researchFaction,
            "We have finished building the colonizer ship. We'll need to bring it over to the research station to install the hyperdrive.");
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, playerFaction,
            "We have finished building the colonizer ship. We'll need to bring it over to the research station to install the hyperdrive.");

        buildEscapeShipChain.AddCondition(eventManager.CreateLateCondition(() =>
            eventManager.CreateDockShipsAtUnit(new List<Ship> { colonizer }, researchStation, true)));
        buildEscapeShipChain.AddAction(() => {
            colonizer.shipAI.ClearCommands();
            battleManager.GetLocalPlayer().RemoveOwnedUnit(colonizer);
        });
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, planetFaction,
            "The colonizer has docked at our station and we are beggining to instal the hyperdrive.", GetTimeScale() * 3);
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, shipyardFaction,
            "The colonizer has docked at our station and we are beggining to instal the hyperdrive.");
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, playerFaction,
            "The colonizer has docked at our station and we are beggining to instal the hyperdrive.");
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, planetFaction,
            "Installation complete. The colonizer is ready to leave.", GetTimeScale() * 50);
        buildEscapeShipChain.AddCommEvent(shipyardCommManager, shipyardFaction,
            "Installation complete. The colonizer is ready to leave.");
        buildEscapeShipChain.AddButtonCommEvent(shipyardCommManager, playerFaction,
            "Installation complete. The colonizer is ready to leave. Activating the hyperdrive on your mark.", "Leave");

        buildEscapeShipChain.AddAction(() => {
            colonizer.shipAI.AddUnitAICommand(
                Command.CreateMoveCommand(colonizer.position + Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0, 360), 500)));
        });
        buildEscapeShipChain.AddCondition(
            eventManager.CreateLateCondition(() => eventManager.CreateWaitUntilShipsIdle(new List<Ship> { colonizer })));
        buildEscapeShipChain.AddCondition(eventManager.CreateWaitCondition(GetTimeScale() * 3));
        buildEscapeShipChain.AddAction(() => {
            colonizer.Explode();
            battleManager.EndBattle(playerFaction);
        });
        buildEscapeShipChain.Build(eventManager)();
    }

    void AddPiratesEventLine() {
        FactionCommManager planetCommManager = planetFaction.GetFactionCommManager();
        FactionCommManager shipyardCommManager = shipyardFaction.GetFactionCommManager();
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();
        List<Ship> pirateShips = new List<Ship>();

        EventChainBuilder pirateChain = new EventChainBuilder();
        pirateChain.AddCondition(eventManager.CreatePredicateCondition(_ => playerMiningStation.IsBuilt()));
        pirateChain.AddCondition(eventManager.CreateWaitCondition(600));
        pirateChain.AddCondition(eventManager.CreatePredicateCondition(_ =>
            tradeStation.GetAllDockedShips().Any(s => s.faction == planetFaction && s.IsCivilianShip())));
        pirateChain.AddAction(() => {
            Ship newPirateShip = tradeStation.GetAllDockedShips().First(s => s.faction == planetFaction && s.IsCivilianShip());
            planetFaction.TransferShipTo(newPirateShip, pirateFaction);
            pirateShips.Add(newPirateShip);
            newPirateShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(20), Command.CommandAction.Replace);
            newPirateShip.shipAI.AddUnitAICommand(Command.CreateDockCommand(otherMiningStation), Command.CommandAction.AddToEnd);
        });
        pirateChain.AddCondition(
            eventManager.CreateLateCondition(() => eventManager.CreateDockShipsAtUnit(pirateShips, otherMiningStation)));
        pirateChain.AddCondition(eventManager.CreateWaitCondition(15));
        pirateChain.AddAction(() => {
            otherMiningFaction.TransferStationTo(otherMiningStation, pirateFaction);
            otherMiningStation.GetAllDockedShips().ForEach(s => {
                if (s.faction != pirateFaction) {
                    s.faction.TransferShipTo(s, pirateFaction);
                    pirateShips.Add(s);
                }
            });
            pirateFaction.StartWar(planetFaction);
            pirateFaction.StartWar(researchFaction);
            pirateFaction.StartWar(playerFaction);
            pirateFaction.StartWar(shipyardFaction);
            resourceCosts[CargoBay.CargoTypes.Metal] *= 1.25;
        });
        pirateChain.AddCommEvent(planetCommManager, playerFaction,
            $"Pirates have siezed the {otherMiningFaction.name}'s mining station!\n" +
            "We will have a harder time securing metal for the planet at this rate!", 3 * GetTimeScale());
        pirateChain.Build(eventManager)();
    }

    void AddMoonQuestLine() {
        FactionCommManager planetCommManager = planetFaction.GetFactionCommManager();
        FactionCommManager shipyardCommManager = shipyardFaction.GetFactionCommManager();
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();

        EventChainBuilder moonColonyChain = new EventChainBuilder();
        moonColonyChain.AddCondition(eventManager.CreatePredicateCondition(_ => playerMiningStation.IsBuilt()));
        moonColonyChain.AddCondition(eventManager.CreateWaitCondition(200));
        moonColonyChain.AddCommEvent(planetCommManager, shipyardFaction,
            "We would like to order a colony ship to the moon.");
        moonColonyChain.AddCommEvent(shipyardCommManager, planetFaction,
            "Understood, it will take some time to build.");
        moonColonyChain.AddAction(() => shipyard.GetConstructionBay().AddConstructionToBeginningQueue(
            new Ship.ShipConstructionBlueprint(planetFaction,
                battleManager.shipBlueprints.First(b => b.shipScriptableObject.shipType == Ship.ShipType.Colonizer))));
        moonColonyChain.AddCondition(eventManager.CreateBuildShipAtStation(battleManager.shipBlueprints
            .First(b => b.shipScriptableObject.shipType == Ship.ShipType.Colonizer), planetFaction, shipyard));
        Ship colonizer = null;
        moonColonyChain.AddAction(() => colonizer = planetFaction.ships.First(s => s.IsColonizerShip()));
        moonColonyChain.AddAction(() =>
            colonizer.shipAI.AddUnitAICommand(Command.CreateDockCommand(tradeStation), Command.CommandAction.Replace));
        moonColonyChain.AddCommEvent(shipyardCommManager, planetFaction,
            "The colony ship has been built and is heading to the trade station.");
        moonColonyChain.AddCondition(eventManager.CreateLateCondition(() =>
            eventManager.CreateDockShipsAtUnit(new List<Ship> { colonizer }, tradeStation)));
        moonColonyChain.AddCondition(eventManager.CreateWaitCondition(160));
        moonColonyChain.AddCommEvent(planetCommManager, shipyardFaction,
            "Our colony ship is loaded and is ready to head to the moon!");
        moonColonyChain.AddAction(() =>
            colonizer.shipAI.AddUnitAICommand(Command.CreateColonizeCommand(moon), Command.CommandAction.Replace));
        moonColonyChain.AddCondition(eventManager.CreatePredicateCondition(_ => moon.planetFactions.ContainsKey(planetFaction)));
        moonColonyChain.AddCommEvent(planetCommManager, shipyardFaction,
            "We have started a colony on the moon! This is great progress for space travel!", 8);
        moonColonyChain.AddCommEvent(planetCommManager, playerFaction,
            "We have started a colony on the moon! This is great progress for space travel!");
        moonColonyChain.Build(eventManager)();
    }

    void AddWarEscalationEventLine() {
        FactionCommManager planetCommManager = planetFaction.GetFactionCommManager();
        FactionCommManager playerComm = playerFaction.GetFactionCommManager();
        FactionCommManager researchCommManager = researchFaction.GetFactionCommManager();
        FactionCommManager shipyardCommManager = shipyardFaction.GetFactionCommManager();

        EventChainBuilder planetEscalationChain = new EventChainBuilder();
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(1200));
        planetEscalationChain.AddAction(() => {
            planetOligarchy.GetFactionAI().attackSpeed = 6f;
            planetOligarchy.GetFactionAI().attackStrength = .04f;
            planetDemocracy.GetFactionAI().attackSpeed = 9f;
            planetDemocracy.GetFactionAI().attackStrength = .03f;
        });
        planetEscalationChain.AddAction(() => planetOligarchy.StartWar(planetDemocracy));
        planetEscalationChain.AddCommEvent(planetCommManager, shipyardFaction,
            $"Warning: The {planetOligarchy.name} has declared war on {planetDemocracy.name}");
        planetEscalationChain.AddCommEvent(planetCommManager, researchFaction,
            $"Warning: The {planetOligarchy.name} has declared war on {planetDemocracy.name}");
        planetEscalationChain.AddCommEvent(planetCommManager, playerFaction,
            $"Warning: The {planetOligarchy.name} has declared war on {planetDemocracy.name}");
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(300));
        planetEscalationChain.AddAction(() => {
            planetEmpire.StartWar(planetDemocracy);
            planetEmpire.StartWar(planetOligarchy);
            planetEmpire.GetFactionAI().attackSpeed = 5f;
            planetEmpire.GetFactionAI().attackStrength = .04f;
            planetOligarchy.GetFactionAI().attackSpeed = 6f;
            planetOligarchy.GetFactionAI().attackStrength = .05f;
            planetDemocracy.GetFactionAI().attackSpeed = 7f;
            planetDemocracy.GetFactionAI().attackStrength = .04f;
        });
        planetEscalationChain.AddCommEvent(planetCommManager, shipyardFaction,
            $"Warning: The {planetEmpire.name} has declared war on {planetOligarchy.name} and {planetDemocracy.name}");
        planetEscalationChain.AddCommEvent(planetCommManager, researchFaction,
            $"Warning: The {planetEmpire.name} has declared war on {planetOligarchy.name} and {planetDemocracy.name}");
        planetEscalationChain.AddCommEvent(planetCommManager, playerFaction,
            $"Warning: The {planetEmpire.name} has declared war on {planetOligarchy.name} and {planetDemocracy.name}");
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(500));

        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(500));
        planetEscalationChain.AddCommEvent(planetCommManager, playerFaction,
            $"The {planetOligarchy.name} has developed a new war robot technology, it will probably tip the war in their favor.");
        planetEscalationChain.AddAction(() => planet.planetFactions[planetOligarchy].AddForce(100000));
        planetEscalationChain.AddAction(() => {
            robotFaction = battleManager.CreateNewFaction(
                new Faction.FactionData(typeof(RobotFactionAI), "Robot", "RBT", colorPicker.PickColor(), Random.Range(1, 2) * 5400, 2000, 0,
                    0), new BattleManager.PositionGiver(planet.GetPosition()), 100);
            robotFactionAI = (RobotFactionAI)robotFaction.GetFactionAI();
            for (int i = 0; i < 200; i++) {
                robotFaction.DiscoverResearchArea((ResearchAreas)Random.Range(0, 3), true);
            }

            planet.AddFaction(robotFaction, 0, 0, 0, 70000000L, 20000, "Robot Uprising");
            Planet.PlanetTerritory oligarchyTerritory = planet.planetFactions[planetOligarchy].territory;
            planet.planetFactions[robotFaction].territory.AddFrom(new Planet.PlanetTerritory(oligarchyTerritory.highQualityArea / 5,
                oligarchyTerritory.mediumQualityArea / 5, oligarchyTerritory.lowQualityArea / 5));
            oligarchyTerritory.SubtractFrom(planet.planetFactions[robotFaction].territory);
        });
        for (int i = 0; i < 20; i++) {
            planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(20));
            planetEscalationChain.AddAction(() => {
                planet.planetFactions[planetOligarchy].AddForce(70000);
                Planet.PlanetTerritory oligarchyTerritory = planet.planetFactions[planetOligarchy].territory;
                Planet.PlanetTerritory territoryInfiltrated = new Planet.PlanetTerritory(oligarchyTerritory.highQualityArea / 5,
                    oligarchyTerritory.mediumQualityArea / 5, oligarchyTerritory.lowQualityArea / 5);
            });
        }

        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(30));
        // Uprising Occurs
        planetEscalationChain.AddCommEvent(planetCommManager, playerFaction,
            $"A robot uprising has begun within the {planetOligarchy.name}");
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(100));
        planetEscalationChain.AddAction(() =>
            planet.planetFactions[planetOligarchy].AddForce(planet.planetFactions[planetOligarchy].RemoveForce(70000)));
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(60));
        planetEscalationChain.AddAction(() =>
            planet.planetFactions[planetOligarchy].AddForce(planet.planetFactions[planetOligarchy].RemoveForce(70000)));
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(60));
        planetEscalationChain.AddAction(() =>
            planet.planetFactions[planetOligarchy].AddForce(planet.planetFactions[planetOligarchy].RemoveForce(70000)));
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(60));
        planetEscalationChain.AddAction(() =>
            planet.planetFactions[planetOligarchy].AddForce(planet.planetFactions[planetOligarchy].RemoveForce(70000)));
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(60));
        planetEscalationChain.AddAction(() => {
            planet.planetFactions[robotFaction].AddForce(30000000L);
            robotFaction.StartWar(planetOligarchy);
            robotFaction.GetFactionAI().attackSpeed = 4f;
            robotFaction.GetFactionAI().attackStrength = .05f;
            eventManager.AddEvent(eventManager.CreatePredicateCondition(_ => planet.planetFactions[planetOligarchy].force < 1000000),
                () => {
                    planet.planetFactions[planetFaction].territory.AddFrom(planet.planetFactions[planetOligarchy].territory);
                    planet.planetFactions[planetFaction].AddForce(planet.planetFactions[planetOligarchy].force);
                    planet.planetFactions[planetFaction].AddPopulation(planet.planetFactions[planetOligarchy].population);
                    planet.planetFactions[planetOligarchy].territory.SubtractFrom(planet.planetFactions[planetOligarchy].territory);
                    planet.RemoveFaction(planetOligarchy);
                });
        });
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(180));
        planetEscalationChain.AddAction(() => {
            planet.planetFactions[robotFaction].AddForce(40000000L);
            robotFaction.StartWar(planetDemocracy);
            robotFaction.GetFactionAI().attackStrength = .07f;
            eventManager.AddEvent(eventManager.CreatePredicateCondition(_ => planet.planetFactions[planetDemocracy].force < 1000000),
                () => {
                    planet.planetFactions[planetFaction].territory.AddFrom(planet.planetFactions[planetDemocracy].territory);
                    planet.planetFactions[planetFaction].AddForce(planet.planetFactions[planetDemocracy].force);
                    planet.planetFactions[planetFaction].AddPopulation(planet.planetFactions[planetDemocracy].population);
                    planet.planetFactions[planetDemocracy].territory.SubtractFrom(planet.planetFactions[planetDemocracy].territory);
                    planet.RemoveFaction(planetDemocracy);
                });
        });
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(180));
        planetEscalationChain.AddAction(() => {
            planet.planetFactions[robotFaction].AddForce(30000000L);
            robotFaction.StartWar(planetEmpire);
            robotFaction.GetFactionAI().attackStrength = .09f;
            eventManager.AddEvent(eventManager.CreatePredicateCondition(_ => planet.planetFactions[planetEmpire].force < 1000000), () => {
                planet.planetFactions[planetFaction].territory.AddFrom(planet.planetFactions[planetEmpire].territory);
                planet.planetFactions[planetFaction].AddForce(planet.planetFactions[planetEmpire].force);
                planet.planetFactions[planetFaction].AddPopulation(planet.planetFactions[planetEmpire].population);
                planet.planetFactions[planetEmpire].territory.SubtractFrom(planet.planetFactions[planetEmpire].territory);
                planet.RemoveFaction(planetEmpire);
            });
        });
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(40));
        planetEscalationChain.AddAction(() => {
            planet.planetFactions[robotFaction].AddForce(40000000L);
            robotFaction.StartWar(minorFactions);
            robotFaction.GetFactionAI().attackSpeed = 3f;
            eventManager.AddEvent(eventManager.CreatePredicateCondition(_ => planet.planetFactions[minorFactions].force < 1000000), () => {
                planet.planetFactions[planetFaction].territory.AddFrom(planet.planetFactions[minorFactions].territory);
                planet.planetFactions[planetFaction].AddForce(planet.planetFactions[minorFactions].force);
                planet.planetFactions[planetFaction].AddPopulation(planet.planetFactions[minorFactions].population);
                planet.planetFactions[minorFactions].territory.SubtractFrom(planet.planetFactions[minorFactions].territory);
                planet.RemoveFaction(minorFactions);
            });
        });
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(40));
        planetEscalationChain.AddAction(() => planet.planetFactions[robotFaction].AddForce(80000000L));
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(40));
        planetEscalationChain.AddAction(() => planet.planetFactions[robotFaction].AddForce(80000000L));
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(40));
        planetEscalationChain.AddAction(() => planet.planetFactions[robotFaction].AddForce(80000000L));
        planetEscalationChain.AddCondition(eventManager.CreateWaitCondition(40));
        planetEscalationChain.AddAction(() => planet.planetFactions[robotFaction].AddForce(80000000L));
        planetEscalationChain.AddCondition(eventManager.CreatePredicateCondition(_ => {
            long alliedForce = 0;
            if (planet.planetFactions.ContainsKey(planetDemocracy)) alliedForce += planet.planetFactions[planetDemocracy].force;
            if (planet.planetFactions.ContainsKey(planetOligarchy)) alliedForce += planet.planetFactions[planetOligarchy].force;
            if (planet.planetFactions.ContainsKey(planetEmpire)) alliedForce += planet.planetFactions[planetEmpire].force;
            if (planet.planetFactions.ContainsKey(minorFactions)) alliedForce += planet.planetFactions[minorFactions].force;
            return alliedForce < 6000000000L;
        }));
        planetEscalationChain.AddAction(() => {
            planet.planetFactions[robotFaction].AddForce(200000000L);
            robotFaction.StartWar(planetFaction);
            planetEmpire.EndWar(planetDemocracy);
            planetEmpire.EndWar(planetOligarchy);
            planetDemocracy.EndWar(planetOligarchy);
            robotFaction.GetFactionAI().attackSpeed = 4f;
            robotFaction.GetFactionAI().attackStrength = .1f;
        });
        planetEscalationChain.AddCommEvent(planetCommManager, playerFaction,
            $"The {planetFaction.name}, {planetDemocracy.name} and {planetOligarchy.name} have reached a peace agreement due to the robot threat.");
        planetEscalationChain.Build(eventManager)();
    }

    void AddEasterEggs() {
        eventManager.AddEvent(eventManager.CreatePredicateCondition(_ => moon.planetFactions.ContainsKey(playerFaction)),
            () => {
                planetFaction.GetFactionCommManager().SendCommunication(playerFaction,
                    "Uhhh, you weren't supposed to be the one to colonize the moon. We already did that!");
                playerFaction.AddScience(1000);
                moon.planetFactions[playerFaction].AddPopulation(300);
                moon.planetFactions[playerFaction].AddForce(100);
            });
        eventManager.AddEvent(eventManager.CreatePredicateCondition(_ => planet.planetFactions.ContainsKey(playerFaction)),
            () => {
                planetFaction.GetFactionCommManager().SendCommunication(playerFaction,
                    planet.objectName + " is already colonized. That was a waste of a colonizer.");
                planet.planetFactions[playerFaction].AddPopulation(10000000000);
                planet.planetFactions[playerFaction].AddForce(1000000000);
                var worldSpaceUnionTerritory = planet.planetFactions[planetFaction].territory;
                planet.planetFactions[playerFaction].territory.AddFrom(new Planet.PlanetTerritory(worldSpaceUnionTerritory.highQualityArea / 2,
                    worldSpaceUnionTerritory.mediumQualityArea / 2, worldSpaceUnionTerritory.lowQualityArea / 2));
                worldSpaceUnionTerritory.SubtractFrom(planet.planetFactions[playerFaction].territory);
                eventManager.AddEvent(eventManager.CreatePredicateCondition(_ => robotFaction != null),
                    () => {
                        robotFaction.StartWar(playerFaction);
                        playerFaction.GetFactionAI().attackSpeed = 4;
                        playerFaction.GetFactionAI().attackStrength = .05f;
                        playerFaction.GetFactionCommManager().SendCommunication(playerFaction,
                            "The " + robotFaction.name + "s have started fighting us!");
                    });
                playerFaction.AddScience(1000);
            });
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
