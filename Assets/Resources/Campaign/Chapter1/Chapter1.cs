using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static BattleManager;

public class Chapter1 : CampaingController {
    public Faction playerFaction { get; private set; }
    public PlayerFactionAI playerFactionAI { get; private set; }
    public MiningStation playerMiningStation { get; private set; }
    Faction otherMiningFaction;
    OtherMiningFactionAI otherMiningFactionAI;
    MiningStation otherMiningStation;
    Faction planetFaction;
    public PlanetFactionAI planetFactionAI { get; private set; }
    Planet planet;
    public Station tradeStation { get; private set; }
    public Faction shipyardFaction { get; private set; }
    public ShipyardFactionAI shipyardFactionAI { get; private set; }
    public Shipyard shipyard { get; private set; }
    Faction researchFaction;
    Station researchStation;
    float metalCost;

    public DifficultyLevel difficultyLevel { get; private set; }

    public enum DifficultyLevel {
        Easy,
        Normal,
        Hard,
    }

    public override void SetupBattle(BattleManager battleManager) {
        base.SetupBattle(battleManager);
        battleManager.SetSimulationTimeScale(1);
        metalCost = 12.6f;
        int starCount = Random.Range(1, 4);
        for (int i = 0; i < starCount; i++) {
            battleManager.CreateNewStar("Star" + (i + 1));
        }
        playerFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(PlayerFactionAI), "Free Space Miners", "FSM", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        playerFactionAI = (PlayerFactionAI)playerFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(playerFaction.GetPosition(), 0, 5000, 100, 1000, 2), Random.Range(5, 10), 10);
        }
        playerMiningStation = (MiningStation)battleManager.CreateNewStation(new Station.StationData(playerFaction, battleManager.GetStationBlueprint(Station.StationType.MiningStation).stationScriptableObject, "MiningStation", playerFaction.GetPosition(), Random.Range(0, 360), false));

        otherMiningFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(OtherMiningFactionAI), "Off-World Metal Industries", "OWM", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        otherMiningFactionAI = (OtherMiningFactionAI)otherMiningFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(otherMiningFaction.GetPosition(), 0, 5000, 100, 1000, 2), Random.Range(5, 10), 10);
        }
        otherMiningStation = (MiningStation)battleManager.CreateNewStation(new Station.StationData(otherMiningFaction, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/MiningStation"), "MiningStation", otherMiningFaction.GetPosition(), Random.Range(0, 360), true));
        otherMiningStation.BuildShip(Ship.ShipClass.Transport);
        otherMiningStation.LoadCargo(2400 * 3, CargoBay.CargoTypes.Metal);


        planetFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(PlanetFactionAI), "World Space Union", "WSU", 100000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        planet = battleManager.CreateNewPlanet(new BattleManager.PositionGiver(planetFaction.GetPosition()), new Planet.PlanetData(planetFaction, "Home", Random.Range(0,360), (long)Random.Range(500, 600) * 100000000, 0.01, Random.Range(0.12f, 0.25f), Random.Range(0.18f, 0.25f), Random.Range(0.1f, 0.2f)));
        planet.SetPopulationTarget((long)(planet.GetPopulation() * 1.1));
        tradeStation = battleManager.CreateNewStation(new Station.StationData(planetFaction, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/TradeStation"), "TradeStation", planet.GetPosition(), Random.Range(0, 360)), new PositionGiver(Vector2.MoveTowards(planet.GetPosition(), Vector2.zero, planet.GetSize() + 180), 0, 1000, 50, 200, 5));
        tradeStation.LoadCargo(2400 * 5, CargoBay.CargoTypes.Metal);
        ((Shipyard)tradeStation).GetConstructionBay().AddConstructionToBeginningQueue(new Ship.ShipConstructionBlueprint(planetFaction, battleManager.GetShipBlueprint(Ship.ShipType.Civilian), "Civilian Ship"));
        planetFactionAI = (PlanetFactionAI)planetFaction.GetFactionAI();
        tradeStation.BuildShip(Ship.ShipClass.HeavyTransport);
        tradeStation.BuildShip(Ship.ShipClass.HeavyTransport);

        shipyardFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(ShipyardFactionAI), "Solar Shipyards", "SSH", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        shipyard = (Shipyard)battleManager.CreateNewStation(new Station.StationData(shipyardFaction, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/Shipyard"), "Shipyard", shipyardFaction.GetPosition(), Random.Range(0, 360)));
        shipyardFactionAI = (ShipyardFactionAI)shipyardFaction.GetFactionAI();

        researchFaction = battleManager.CreateNewFaction(new Faction.FactionData("Frontier Research", "FRO", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 5000, 2), 100);
        researchStation = battleManager.CreateNewStation(new Station.StationData(researchFaction, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/ResearchStation"), "ResearchStation", researchFaction.GetPosition(), Random.Range(0, 360)));

        playerMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        Ship setupFleetShip1 = tradeStation.BuildShip(playerFaction, Ship.ShipClass.Transport);
        ConstructionShip setupFleetShip2 = (ConstructionShip)tradeStation.BuildShip(playerFaction, Ship.ShipClass.StationBuilder);
        setupFleetShip2.targetStationBlueprint = playerMiningStation;
        Ship setupFleetShip3 = tradeStation.BuildShip(playerFaction, Ship.ShipClass.Transport);
        Fleet miningStationSetupFleet = playerFaction.CreateNewFleet("StationSetupFleet", playerFaction.ships);
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateWaitCommand(4 * battleManager.timeScale), Command.CommandAction.Replace);
        miningStationSetupFleet.FleetAI.AddFormationTowardsPositionCommand(playerMiningStation.GetPosition(), shipyard.GetSize() * 4, Command.CommandAction.AddToEnd);
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateWaitCommand(3 * battleManager.timeScale));
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateMoveOffsetCommand(miningStationSetupFleet.GetPosition(), playerMiningStation.GetPosition(), playerMiningStation.GetSize() * 3));
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateDockCommand(playerMiningStation));
        miningStationSetupFleet.FleetAI.AddFleetAICommand(Command.CreateDisbandFleetCommand());

        otherMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        otherMiningFaction.GetTransportShip(0).shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(10, 20)), Command.CommandAction.AddToBegining);

        planetFaction.GetTransportShip(0).shipAI.AddUnitAICommand(Command.CreateTransportDelayCommand(tradeStation, shipyard, 1000), Command.CommandAction.AddToEnd);
        planetFaction.GetTransportShip(1).shipAI.AddUnitAICommand(Command.CreateTransportDelayCommand(tradeStation, shipyard, 1000), Command.CommandAction.AddToEnd);

        List<Ship> civilianShips = new List<Ship>();
        for (int i = 0; i < Random.Range(1, 3); i++) {
            civilianShips.Add(tradeStation.BuildShip(new Ship.ShipData(planetFaction, battleManager.GetShipBlueprint(Ship.ShipType.Civilian).shipScriptableObject, "Civilian", new Vector2(Random.Range(-50000, 50000), Random.Range(-50000, 50000)), Random.Range(0, 360)), 0, null));
        }
        List<Station> randStations = battleManager.stations.ToList();
        for (int i = 0; i < Random.Range(3, 5); i++) {
            Ship newShip = randStations[Random.Range(0, randStations.Count)].BuildShip(planetFaction, Ship.ShipType.Civilian);
            civilianShips.Add(newShip);
            newShip.shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(0, 70)));
        }
        playerFactionAI.SetupPlayerFactionAI(battleManager, playerFaction, this, playerMiningStation);
        otherMiningFactionAI.SetupOtherMiningFactionAI(battleManager, otherMiningFaction, this, shipyardFactionAI, otherMiningStation, tradeStation);
        planetFactionAI.SetupPlanetFactionAI(battleManager, planetFaction, this, shipyardFactionAI, planet, tradeStation, shipyard, civilianShips);
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
        Faction planetEmpire = battleManager.CreateNewFaction(new Faction.FactionData("Empire", "EMP", 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(planetEmpire, Random.Range(0.20f, 0.35f), Random.Range(8.0f, 10.0f), "Increases unit production");
        Faction planetDemocracy = battleManager.CreateNewFaction(new Faction.FactionData("Democracy", "DEM", 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(planetDemocracy, Random.Range(0.30f, 0.40f), Random.Range(6f, 11f), "Increases mining speed");
        Faction planetOligarchy = battleManager.CreateNewFaction(new Faction.FactionData("Oligarchy", "OLG", 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(planetOligarchy, Random.Range(0.70f, 0.80f), Random.Range(5f, 8f), "Increases research rate");
        Faction minorFactions = battleManager.CreateNewFaction(new Faction.FactionData("Minor Factions", "MIN", 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFaction(minorFactions, Random.Range(0.70f, 0.80f), Random.Range(2f, 3f), "All base stats improved a little");
        //planetEmpire.StartWar(planetDemocracy);
        //planetDemocracy.StartWar(planetOligarchy);
        //planetOligarchy.StartWar(planetEmpire);

        LocalPlayer.Instance.lockedOwnedUnits = true;
        LocalPlayer.Instance.ownedUnits.Add(playerMiningStation);
        LocalPlayer.Instance.SetupFaction(playerFaction);
        LocalPlayer.Instance.GetLocalPlayerInput().SetZoom(400);
        LocalPlayer.Instance.GetLocalPlayerInput().StartFollowingUnit(setupFleetShip2);
    }


    public override void UpdateController() {
        base.UpdateController();
    }

    public float GetMetalCost() {
        return metalCost;
    }

    /// <summary>
    /// Multiplies the metal cost by the factor given
    /// </summary>
    /// <param name="factor"> float value close to 1</param>
    public void ChangeMetalCost(float factor) {
        metalCost = metalCost * factor;
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
}
