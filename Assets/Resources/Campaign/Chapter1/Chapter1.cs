using System.Collections;
using System.Collections.Generic;
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
    PlanetFactionAI planetFactionAI;
    Planet planet;
    Station tradeStation;
    Faction shipyardFaction;
    ShipyardFactionAI shipyardFactionAI;
    Shipyard shipyard;
    Faction researchFaction;
    Station researchStation;
    float metalCost;

    public override void SetupBattle() {
        base.SetupBattle();
        battleManager = BattleManager.Instance;
        battleManager.timeScale = 10;
        metalCost = 2.4f;
        int starCount = Random.Range(1, 4);
        for (int i = 0; i < starCount; i++) {
            battleManager.CreateNewStar();
        }
        playerFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(PlayerFactionAI), "PlayerFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        playerFactionAI = (PlayerFactionAI)playerFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAteroidField(new PositionGiver(playerFaction.factionPosition, 0, 5000, 100, 1000, 2), Random.Range(5, 10), 10);
        }
        playerMiningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(playerFaction.factionIndex, GetPathToChapterFolder() + "/MiningStation", "MiningStation", playerFaction.factionPosition, Random.Range(0, 360), true));
        playerMiningStation.BuildShip(Ship.ShipClass.Transport, 0);
        playerMiningStation.BuildShip(Ship.ShipClass.Transport, 0);

        otherMiningFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(OtherMiningFactionAI), "OtherMiningFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        otherMiningFactionAI = (OtherMiningFactionAI)otherMiningFaction.GetFactionAI();
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAteroidField(new PositionGiver(otherMiningFaction.factionPosition, 0, 5000, 100, 1000, 2), Random.Range(5, 10), 10);
        }
        otherMiningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(otherMiningFaction.factionIndex, GetPathToChapterFolder() + "/MiningStation", "MiningStation", otherMiningFaction.factionPosition, Random.Range(0, 360), true));
        otherMiningStation.BuildShip(Ship.ShipClass.Transport, 0);


        planetFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(PlanetFactionAI), "PlanetFaction", 100000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        planet = battleManager.CreateNewPlanet("Home", planetFaction, new BattleManager.PositionGiver(planetFaction.factionPosition), (long)Random.Range(2, 6) * 10000000000);
        tradeStation = battleManager.CreateNewStation(new Station.StationData(planetFaction.factionIndex, GetPathToChapterFolder() + "/TradeStation", "TradeStation", planet.GetPosition(), Random.Range(0, 360)), new PositionGiver(Vector2.MoveTowards(planet.GetPosition(), Vector2.zero, planet.GetSize() + 300), 0, 1000, 30, 300, 3));
        planetFactionAI = (PlanetFactionAI)planetFaction.GetFactionAI();
        tradeStation.BuildShip(Ship.ShipClass.HeavyTransport, 0);
        tradeStation.BuildShip(Ship.ShipClass.HeavyTransport, 0);

        shipyardFaction = battleManager.CreateNewFaction(new Faction.FactionData(typeof(ShipyardFactionAI), "ShipyardFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        shipyard = (Shipyard)battleManager.CreateNewStation(new Station.StationData(shipyardFaction.factionIndex, Station.StationType.Shipyard, "Shipyard", shipyardFaction.factionPosition, Random.Range(0, 360)));
        shipyardFactionAI = (ShipyardFactionAI)shipyardFaction.GetFactionAI();

        researchFaction = battleManager.CreateNewFaction(new Faction.FactionData("ResearchFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 5000, 2), 100);
        researchStation = battleManager.CreateNewStation(new Station.StationData(researchFaction.factionIndex, GetPathToChapterFolder() + "/ResearchStation", "ResearchStation", researchFaction.factionPosition, Random.Range(0, 360)));

        playerMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        playerFaction.GetTransportShip(1).shipAI.AddUnitAICommand(new Command(Command.CommandType.Wait, Random.Range(40, 80)), ShipAI.CommandAction.AddToBegining);

        otherMiningStation.GetMiningStationAI().SetupWantedTrasports(tradeStation.GetPosition());
        otherMiningFaction.GetTransportShip(0).shipAI.AddUnitAICommand(new Command(Command.CommandType.Wait, Random.Range(10, 20)), ShipAI.CommandAction.AddToBegining);

        planetFaction.GetTransportShip(0).shipAI.AddUnitAICommand(new Command(Command.CommandType.TransportDelay, tradeStation, shipyard, 1000), ShipAI.CommandAction.AddToEnd);
        planetFaction.GetTransportShip(1).shipAI.AddUnitAICommand(new Command(Command.CommandType.TransportDelay, tradeStation, shipyard, 1000), ShipAI.CommandAction.AddToEnd);

        playerFactionAI.SetupPlayerFactionAI(this, shipyardFactionAI, playerMiningStation);
        otherMiningFactionAI.SetupOtherMiningFactionAI(this, shipyardFactionAI, otherMiningStation, tradeStation);
        planetFactionAI.SetupPlanetFactionAI(this, shipyardFactionAI, planet, tradeStation, shipyard);
        shipyardFactionAI.SetupShipyardFactionAI(this, planetFactionAI, shipyard);

        int asteroidFieldCount = Random.Range(50, 80);
        for (int i = 0; i < asteroidFieldCount; i++) {
            battleManager.CreateNewAteroidField(new PositionGiver(Vector2.zero, 1500, 100000, 20000, 300, 1), Random.Range(8, 10), 10);
        }

        LocalPlayer.Instance.lockedOwnedUnits = true;
        LocalPlayer.Instance.ownedUnits.Add(playerMiningStation);
        LocalPlayer.Instance.SetupFaction(playerFaction);
    }


    public override void UpdateControler() {
        base.UpdateControler();
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
}
