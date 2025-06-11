using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class FactionTradeTests {

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestFactionTradeSetup() {
        var battleManager = new BattleManager();
        battleManager.shipBlueprints = new List<Ship.ShipBlueprint>();
        battleManager.stationBlueprints = new List<Station.StationBlueprint>();
        battleManager.InitializeBattle();
        battleManager.SetupBattle(new BattleManager.BattleSettings(),
            new List<Faction.FactionData>() {
                new Faction.FactionData(typeof(FactionAI), "TestFaction", "TSF", Color.blue,
                    new Character("TestCharacter", Resources.Load<GameObject>("Prefabs/Characters/Firon")), 100000, 0, 0, 0)
            });
        FactionTrade factionTrade = battleManager.factions.First().factionTrade;
        foreach (CargoBay.CargoTypes cargoType in Enum.GetValues(typeof(CargoBay.CargoTypes)).Cast<CargoBay.CargoTypes>()) {
            Assert.True(factionTrade.resourcesOffered.ContainsKey(cargoType));
            Assert.AreEqual(0, factionTrade.resourcesOffered[cargoType].Count);
            Assert.True(factionTrade.resourcesRequested.ContainsKey(cargoType));
            Assert.AreEqual(0, factionTrade.resourcesRequested[cargoType].Count);
        }
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestStationResourcesOffered() {
        var battleManager = new BattleManager();
        battleManager.shipBlueprints = new List<Ship.ShipBlueprint>();
        battleManager.stationBlueprints = new List<Station.StationBlueprint>();
        battleManager.InitializeBattle();
        battleManager.SetupBattle(new BattleManager.BattleSettings(),
            new List<Faction.FactionData>() {
                new Faction.FactionData(typeof(FactionAI), "TestFaction", "TSF", Color.blue,
                    new Character("TestCharacter", Resources.Load<GameObject>("Prefabs/Characters/Firon")), 100000, 0, 0, 0)
            });
        Faction faction = battleManager.factions.First();
        FactionTrade factionTrade = faction.factionTrade;
        StationScriptableObject shipyard = Resources.Load<StationScriptableObject>("Units/Stations/Shipyard");
        Station station =
            battleManager.CreateNewStation(new BattleObject.BattleObjectData("TestStation", faction), shipyard, true);
        Assert.False(factionTrade.resourcesOffered.Any(type => type.Value.Count != 0));
        station.LoadCargo(1000, CargoBay.CargoTypes.Metal);
        Assert.True(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].ContainsKey(station));
        var offer = factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal][station];
        Assert.AreEqual(CargoBay.CargoTypes.Metal, offer.cargoType);
        Assert.AreEqual(1000, offer.amount);

    }
}
