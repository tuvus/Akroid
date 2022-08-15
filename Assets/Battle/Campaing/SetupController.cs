using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BattleManager;

[System.Serializable]
public class SetupController {
    public void Setup() {
        BattleManager battleManager = BattleManager.Instance;
        int starCount = Random.Range(1, 4);
        for (int i = 0; i < starCount; i++) {
            battleManager.CreateNewStar();
        }
        Faction playerFaction = battleManager.CreateNewFaction(new Faction.FactionData("PlayerFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAteroidField(new PositionGiver(playerFaction.factionPosition, 0, 5000, 100, 1000, 2), Random.Range(5, 10));
        }
        MiningStation playerMinningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(playerFaction.factionIndex, Station.StationType.MiningStation, "MiningStation", playerFaction.factionPosition, Random.Range(0, 360), true));

        Faction otherMinningFaction = battleManager.CreateNewFaction(new Faction.FactionData("OtherMinningFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        for (int i = 0; i < Random.Range(12, 17); i++) {
            battleManager.CreateNewAteroidField(new PositionGiver(otherMinningFaction.factionPosition, 0, 5000, 100, 1000, 2), Random.Range(5, 10));
        }
        MiningStation otherMinningStation = (MiningStation)BattleManager.Instance.CreateNewStation(new Station.StationData(otherMinningFaction.factionIndex, Station.StationType.MiningStation, "MiningStation", otherMinningFaction.factionPosition, Random.Range(0, 360), true));

        Faction planetFaction = battleManager.CreateNewFaction(new Faction.FactionData("PlanetFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        Planet planet = battleManager.CreateNewPlanet("Home", planetFaction, new BattleManager.PositionGiver(planetFaction.factionPosition));

        Faction shipyardFaction = battleManager.CreateNewFaction(new Faction.FactionData("ShipyardFaction", 1000, 0, 0, 0), new BattleManager.PositionGiver(Vector2.zero, 10000, 50000, 500, 1000, 10), 100);
        FleetCommand fleetCommand = (FleetCommand)battleManager.CreateNewStation(new Station.StationData(shipyardFaction.factionIndex, Station.StationType.FleetCommmand, "Shipyard", shipyardFaction.factionPosition, Random.Range(0, 360)));

        int asteroidFieldCount = Random.Range(50, 80);
        for (int i = 0; i < asteroidFieldCount; i++) {
            battleManager.CreateNewAteroidField(new PositionGiver(Vector2.zero, 1500, 100000, 20000, 300, 1), Random.Range(8, 10));
        }

        LocalPlayer.Instance.SetupFaction(playerFaction);

    }
}
