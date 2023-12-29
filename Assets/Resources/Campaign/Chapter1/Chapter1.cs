using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static BattleManager;

public class Chapter1 : CampaingController {
    BattleManager battleManager;
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

    public override void SetupBattle() {
        base.SetupBattle();
        battleManager = BattleManager.Instance;
        battleManager.SetSimulationTimeScale(1);
        metalCost = 2.4f;
        int starCount = Random.Range(1, 4);
        for (int i = 0; i < starCount; i++) {
            battleManager.CreateNewStar("Star" + (i + 1));
        }
        playerFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(PlayerFactionAI), "Free Space Miners", "FSM", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        playerFactionAI = (PlayerFactionAI)playerFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(playerFaction.GetPosition(), 0, 5000, 100, 1000, 2), Random.Range(5, 10), 10);
        }
        playerMiningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(playerFaction.factionIndex, BattleManager.Instance.GetStationBlueprint(Station.StationType.MiningStation).stationScriptableObject, "MiningStation", playerFaction.GetPosition(), Random.Range(0, 360), false));

        otherMiningFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(OtherMiningFactionAI), "Off-World Metal Industries", "OWM", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        otherMiningFactionAI = (OtherMiningFactionAI)otherMiningFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(otherMiningFaction.GetPosition(), 0, 5000, 100, 1000, 2), Random.Range(5, 10), 10);
        }
        otherMiningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(otherMiningFaction.factionIndex, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/MiningStation"), "MiningStation", otherMiningFaction.GetPosition(), Random.Range(0, 360), true));
        otherMiningStation.BuildShip(Ship.ShipClass.Transport);


        planetFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(PlanetFactionAI), "World Space Union", "WSU", 100000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        planet = battleManager.CreateNewPlanet("Home", planetFaction, new BattleManager.PositionGiver(planetFaction.GetPosition()), (long)Random.Range(500, 600) * 100000000, Random.Range(0.4f,0.6f));
        planet.SetPopulationTarget((long)(planet.GetPopulation() * 1.1));
        tradeStation = battleManager.CreateNewStation(new Station.StationData(planetFaction.factionIndex, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/TradeStation"), "TradeStation", planet.GetPosition(), Random.Range(0, 360)), new PositionGiver(Vector2.MoveTowards(planet.GetPosition(), Vector2.zero, planet.GetSize() + 180), 0, 1000, 50, 200, 5));
        planetFactionAI = (PlanetFactionAI)planetFaction.GetFactionAI();
        tradeStation.BuildShip(Ship.ShipClass.HeavyTransport);
        tradeStation.BuildShip(Ship.ShipClass.HeavyTransport);

        shipyardFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(ShipyardFactionAI), "Solar Shipyards", "SSH", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        shipyard = (Shipyard)battleManager.CreateNewStation(new Station.StationData(shipyardFaction.factionIndex, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/Shipyard"), "Shipyard", shipyardFaction.GetPosition(), Random.Range(0, 360)));
        shipyardFactionAI = (ShipyardFactionAI)shipyardFaction.GetFactionAI();

        researchFaction = battleManager.CreateNewFaction(new Faction.FactionData("Frontier Research", "FRO", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 5000, 2), 100);
        researchStation = battleManager.CreateNewStation(new Station.StationData(researchFaction.factionIndex, Resources.Load<StationScriptableObject>(GetPathToChapterFolder() + "/ResearchStation"), "ResearchStation", researchFaction.GetPosition(), Random.Range(0, 360)));

        playerMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        tradeStation.BuildShip(playerFaction.factionIndex, Ship.ShipClass.Transport);
        ((ConstructionShip)tradeStation.BuildShip(playerFaction.factionIndex, Ship.ShipClass.StationBuilder)).targetStationBlueprint = playerMiningStation;
        tradeStation.BuildShip(playerFaction.factionIndex, Ship.ShipClass.Transport);
        Fleet miningStationSetupFleet = playerFaction.CreateNewFleet("StationSetupFleet", playerFaction.ships);
        miningStationSetupFleet.FleetAI.AddUnitAICommand(Command.CreateWaitCommand(4 * BattleManager.Instance.timeScale), Command.CommandAction.Replace);
        miningStationSetupFleet.FleetAI.AddFormationTowardsPositionCommand(playerMiningStation.GetPosition(), shipyard.GetSize() * 4, Command.CommandAction.AddToEnd);
        miningStationSetupFleet.FleetAI.AddUnitAICommand(Command.CreateWaitCommand(3 * BattleManager.Instance.timeScale));
        miningStationSetupFleet.FleetAI.AddUnitAICommand(Command.CreateMoveOffsetCommand(miningStationSetupFleet.GetPosition(), playerMiningStation.GetPosition(), playerMiningStation.GetSize() * 3));
        miningStationSetupFleet.FleetAI.AddUnitAICommand(Command.CreateDockCommand(playerMiningStation));
        miningStationSetupFleet.FleetAI.AddUnitAICommand(Command.CreateDisbandFleetCommand());

        otherMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        otherMiningFaction.GetTransportShip(0).shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(10, 20)), Command.CommandAction.AddToBegining);

        planetFaction.GetTransportShip(0).shipAI.AddUnitAICommand(Command.CreateTransportDelayCommand(tradeStation, shipyard, 1000), Command.CommandAction.AddToEnd);
        planetFaction.GetTransportShip(1).shipAI.AddUnitAICommand(Command.CreateTransportDelayCommand(tradeStation, shipyard, 1000), Command.CommandAction.AddToEnd);

        List<Ship> civilianShips = new List<Ship>();
        for (int i = 0; i < Random.Range(2, 5); i++) {
            civilianShips.Add(tradeStation.BuildShip(new Ship.ShipData(-1, BattleManager.Instance.GetShipBlueprint(Ship.ShipType.Civilian).shipScriptableObject, "Civilian", new Vector2(Random.Range(-50000, 50000), Random.Range(-50000, 50000)), Random.Range(0, 360)), 0, null));
        }
        for (int i = 0; i < Random.Range(5, 15); i++) {
            civilianShips.Add(BattleManager.Instance.stations[Random.Range(0, BattleManager.Instance.stations.Count)].BuildShip(planetFaction.factionIndex, Ship.ShipType.Civilian));
            civilianShips[i].shipAI.AddUnitAICommand(Command.CreateWaitCommand(Random.Range(0, 30)));
        }
        playerFactionAI.SetupPlayerFactionAI(this, playerMiningStation);
        otherMiningFactionAI.SetupOtherMiningFactionAI(this, shipyardFactionAI, otherMiningStation, tradeStation);
        planetFactionAI.SetupPlanetFactionAI(this, shipyardFactionAI, planet, tradeStation, shipyard, civilianShips);
        shipyardFactionAI.SetupShipyardFactionAI(this, planetFactionAI, shipyard);


        int asteroidFieldCount = Random.Range(50, 80);
        for (int i = 0; i < asteroidFieldCount; i++) {
            battleManager.CreateNewAsteroidField(new PositionGiver(Vector2.zero, 1500, 100000, 20000, 300, 1), Random.Range(8, 10), 10);
        }


        planet.AddFactionTerritoryForceFraction(planetFaction, Random.Range(0.01f, 0.05f), Random.Range(30.0f, 50.0f), "Increases space production");
        Faction planetEmpire = battleManager.CreateNewFaction(new Faction.FactionData("Empire", "EMP", 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFactionTerritoryForceFraction(planetEmpire, Random.Range(0.20f, 0.35f), Random.Range(8.0f, 10.0f), "Increases unit production");
        Faction planetDemocracy = battleManager.CreateNewFaction(new Faction.FactionData("Democracy", "DEM", 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFactionTerritoryForceFraction(planetDemocracy, Random.Range(0.30f, 0.40f), Random.Range(6f, 11f), "Increases mining speed");
        Faction planetOligarchy = battleManager.CreateNewFaction(new Faction.FactionData("Oligarchy", "OLG", 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFactionTerritoryForceFraction(planetOligarchy, Random.Range(0.70f, 0.80f), Random.Range(4f, 6f), "Increases research rate");
        Faction minorFactions = battleManager.CreateNewFaction(new Faction.FactionData("Minor Factions", "MIN", 1000000, 1000, 0, 0), new PositionGiver(new Vector2(0, 0), 0, 0, 0, 0, 0), 100);
        planet.AddFactionTerritoryForceFraction(minorFactions, Random.Range(0.70f, 0.80f), Random.Range(2f, 3f), "All base stats improved a little");

        LocalPlayer.Instance.lockedOwnedUnits = true;
        LocalPlayer.Instance.ownedUnits.Add(playerMiningStation);
        LocalPlayer.Instance.SetupFaction(playerFaction);
        LocalPlayer.Instance.GetLocalPlayerInput().SetZoom(400);
        LocalPlayer.Instance.GetLocalPlayerInput().StartFollowingUnit(playerFaction.ships[1]);
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
