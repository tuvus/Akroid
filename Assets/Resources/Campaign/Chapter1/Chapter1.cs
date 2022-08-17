using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BattleManager;

public class Chapter1 : CampaingController {
    Faction playerFaction;
    MiningStation playerMiningStation;
    Faction otherMiningFaction;
    MiningStation otherMiningStation;
    Faction planetFaction;
    Planet planet;
    Station tradeStation;
    Faction shipyardFaction;
    Shipyard shipyard;
    Faction researchFaction;
    Station researchStation;

    public override void SetupBattle() {
        base.SetupBattle();
        BattleManager battleManager = BattleManager.Instance;
        int starCount = Random.Range(1, 4);
        for (int i = 0; i < starCount; i++) {
            battleManager.CreateNewStar();
        }
        playerFaction = battleManager.CreateNewFaction(new Faction.FactionData("PlayerFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAteroidField(new PositionGiver(playerFaction.factionPosition, 0, 5000, 100, 1000, 2), Random.Range(5, 10));
        }
        playerMiningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(playerFaction.factionIndex, GetPathToChapterFolder() + "/MiningStation", "MiningStation", playerFaction.factionPosition, Random.Range(0, 360), true));

        otherMiningFaction = battleManager.CreateNewFaction(new Faction.FactionData("OtherMiningFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAteroidField(new PositionGiver(otherMiningFaction.factionPosition, 0, 5000, 100, 1000, 2), Random.Range(5, 10));
        }
        otherMiningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(otherMiningFaction.factionIndex, GetPathToChapterFolder() + "/MiningStation", "MiningStation", otherMiningFaction.factionPosition, Random.Range(0, 360), true));

        planetFaction = battleManager.CreateNewFaction(new Faction.FactionData("PlanetFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        planet = battleManager.CreateNewPlanet("Home", planetFaction, new BattleManager.PositionGiver(planetFaction.factionPosition));
        tradeStation = battleManager.CreateNewStation(new Station.StationData(planetFaction.factionIndex, GetPathToChapterFolder() + "/TradeStation", "TradeStation", planet.GetPosition(), Random.Range(0, 360)), new PositionGiver(Vector2.MoveTowards(planet.GetPosition(), Vector2.zero, planet.GetSize() + 300), 0, 1000, 30, 300, 3));

        shipyardFaction = battleManager.CreateNewFaction(new Faction.FactionData("ShipyardFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        shipyard = (Shipyard)battleManager.CreateNewStation(new Station.StationData(shipyardFaction.factionIndex, Station.StationType.Shipyard, "Shipyard", shipyardFaction.factionPosition, Random.Range(0, 360)));

        researchFaction = battleManager.CreateNewFaction(new Faction.FactionData("ResearchFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 5000, 2), 100);
        researchStation = battleManager.CreateNewStation(new Station.StationData(researchFaction.factionIndex, GetPathToChapterFolder() + "/ResearchStation", "ResearchStation", researchFaction.factionPosition, Random.Range(0, 360)));


        int asteroidFieldCount = Random.Range(50, 80);
        for (int i = 0; i < asteroidFieldCount; i++) {
            battleManager.CreateNewAteroidField(new PositionGiver(Vector2.zero, 1500, 100000, 20000, 300, 1), Random.Range(8, 10));
        }

        LocalPlayer.Instance.SetupFaction(playerFaction);
    }

    public override void UpdateControler() {
        base.UpdateControler();
    }

    public override string GetPathToChapterFolder() {
        return "Campaign/Chapter1";
    }
}
