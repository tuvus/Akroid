using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class FactionTradeTests {
    private TestBattleManager battleManager;
    private Station testStation;
    private Faction testFaction;
    private Faction testFaction2;
    private Ship testShip;

    void SetupTradeTests() {
        battleManager = new TestBattleManager();
        testFaction = battleManager.CreateTestFaction();
        CargoBayScriptableObject cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 12;
        HangarScriptableObject hangarScriptableObject = ScriptableObject.CreateInstance<HangarScriptableObject>();
        hangarScriptableObject.maxDockSpace = 10000;
        testStation = battleManager.CreateNewStation(new BattleObject.BattleObjectData("TestStation", testFaction),
            ScriptableObject.CreateInstance<TestUtils.TestStationScriptableObject>()
                .SetupScriptableObject(cargoBayScriptableObject, hangarScriptableObject), true);
        testFaction2 = battleManager.CreateTestFaction();
        testShip = testStation.BuildShip(new BattleObject.BattleObjectData("TestShip", testFaction2),
            ScriptableObject.CreateInstance<TestUtils.TestShipScriptableObject>()
                .SetupScriptableObject(cargoBayScriptableObject));
        Assert.AreEqual(testStation, testShip.dockedStation);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestFactionTradeSetup() {
        SetupTradeTests();
        FactionTrade factionTrade = testFaction.factionTrade;
        foreach (CargoBay.CargoType cargoType in CargoBay.allCargoTypes) {
            Assert.True(factionTrade.resourcesOffered.ContainsKey(cargoType));
            Assert.AreEqual(0, factionTrade.resourcesOffered[cargoType].Count);
            Assert.True(factionTrade.resourcesRequested.ContainsKey(cargoType));
            Assert.AreEqual(0, factionTrade.resourcesRequested[cargoType].Count);
        }
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestStationResourcesOffered() {
        SetupTradeTests();
        FactionTrade factionTrade = testFaction.factionTrade;
        Assert.True(factionTrade.resourcesOffered.All(type => type.Value.Count == 0));

        Assert.Zero(testStation.LoadCargo(1000, CargoBay.CargoType.Metal));
        Assert.True(factionTrade.resourcesOffered[CargoBay.CargoType.Metal].ContainsKey(testStation));
        var offer = factionTrade.resourcesOffered[CargoBay.CargoType.Metal][testStation];
        Assert.AreEqual(CargoBay.CargoType.Metal, offer.cargoType);
        Assert.AreEqual(1000, offer.amount);

        Assert.Zero(testStation.UseCargo(1000, CargoBay.CargoType.Metal));
        Assert.True(factionTrade.resourcesOffered.All(type => type.Value.Count == 0));

        Assert.Zero(testShip.LoadCargo(200, CargoBay.CargoType.Gas));
        Assert.Zero(testStation.LoadCargoFromUnit(100, CargoBay.CargoType.Gas, testShip));
        Assert.True(factionTrade.resourcesOffered[CargoBay.CargoType.Gas].ContainsKey(testStation));
        offer = factionTrade.resourcesOffered[CargoBay.CargoType.Gas][testStation];
        Assert.AreEqual(CargoBay.CargoType.Gas, offer.cargoType);
        Assert.AreEqual(100, offer.amount);

        Assert.Zero(testStation.LoadCargoFromUnit(100, CargoBay.CargoType.Gas, testShip));
        Assert.True(factionTrade.resourcesOffered[CargoBay.CargoType.Gas].ContainsKey(testStation));
        offer = factionTrade.resourcesOffered[CargoBay.CargoType.Gas][testStation];
        Assert.AreEqual(CargoBay.CargoType.Gas, offer.cargoType);
        Assert.AreEqual(200, offer.amount);

        Assert.Zero(testStation.LoadCargo(400, CargoBay.CargoType.Gas));
        factionTrade.MakeSellTradeAgreement(testFaction2);
        FactionTrade.TradeContract tradeContract = new FactionTrade.TradeContract(testStation, testShip,
            new FactionTrade.TradeOffer(CargoBay.CargoType.Gas, 300, 1.4f));
        factionTrade.AddContract(tradeContract);
        Assert.AreEqual(300, factionTrade.resourcesOffered[CargoBay.CargoType.Gas][testStation].amount);

        FactionTrade.TradeContract contract2 = new FactionTrade.TradeContract(testStation, testShip,
            new FactionTrade.TradeOffer(CargoBay.CargoType.Gas, 200, 1.4f));
        factionTrade.AddContract(contract2);

        Assert.AreEqual(100, factionTrade.resourcesOffered[CargoBay.CargoType.Gas][testStation].amount);

        factionTrade.RemoveContract(tradeContract);
        Assert.AreEqual(400, factionTrade.resourcesOffered[CargoBay.CargoType.Gas][testStation].amount);

        factionTrade.RemoveContract(contract2);
        Assert.AreEqual(600, factionTrade.resourcesOffered[CargoBay.CargoType.Gas][testStation].amount);
        Assert.Zero(testStation.contractedCargo.Count);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestStationResourceRequest() {
        SetupTradeTests();
        FactionTrade factionTrade = testFaction.factionTrade;
        Assert.False(factionTrade.resourcesRequested[CargoBay.CargoType.Metal].ContainsKey(testStation));

        testStation.ReserveCargo(200, CargoBay.CargoType.Metal);
        Assert.True(factionTrade.resourcesRequested[CargoBay.CargoType.Metal].ContainsKey(testStation));
        Assert.AreEqual(200, factionTrade.resourcesRequested[CargoBay.CargoType.Metal][testStation].amount);

        Assert.Zero(testStation.LoadCargo(100, CargoBay.CargoType.Metal));
        Assert.AreEqual(100, factionTrade.resourcesRequested[CargoBay.CargoType.Metal][testStation].amount);

        Assert.Zero(testStation.LoadCargo(200, CargoBay.CargoType.Metal));
        Assert.False(factionTrade.resourcesRequested[CargoBay.CargoType.Metal].ContainsKey(testStation));
        Assert.AreEqual(100, factionTrade.resourcesOffered[CargoBay.CargoType.Metal][testStation].amount);

        testStation.UnReserveCargo(200, CargoBay.CargoType.Metal);
        Assert.AreEqual(300, factionTrade.resourcesOffered[CargoBay.CargoType.Metal][testStation].amount);
        Assert.False(factionTrade.resourcesRequested[CargoBay.CargoType.Metal].ContainsKey(testStation));
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestStationFreeCargoRequest() {
        SetupTradeTests();
        FactionTrade factionTrade = testFaction.factionTrade;
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoType.Metal].Count);
        Assert.Zero(factionTrade.resourcesRequested[CargoBay.CargoType.Metal].Count);
        testStation.SetDesiredFreeCargoRange(CargoBay.CargoType.Metal, 500, 1000);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoType.Metal].Count);
        Assert.AreEqual(1000, factionTrade.resourcesRequested[CargoBay.CargoType.Metal][testStation].amount);

        Assert.Zero(testStation.LoadCargo(100, CargoBay.CargoType.Metal));
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoType.Metal].Count);
        Assert.AreEqual(900, factionTrade.resourcesRequested[CargoBay.CargoType.Metal][testStation].amount);

        Assert.Zero(testStation.LoadCargo(500, CargoBay.CargoType.Metal));
        Assert.AreEqual(100, factionTrade.resourcesOffered[CargoBay.CargoType.Metal][testStation].amount);
        Assert.AreEqual(400, factionTrade.resourcesRequested[CargoBay.CargoType.Metal][testStation].amount);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoType.Metal, 700, 1100);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoType.Metal].Count);
        Assert.AreEqual(500, factionTrade.resourcesRequested[CargoBay.CargoType.Metal][testStation].amount);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoType.Metal, 500, 1000);
        Assert.AreEqual(100, factionTrade.resourcesOffered[CargoBay.CargoType.Metal][testStation].amount);
        Assert.AreEqual(400, factionTrade.resourcesRequested[CargoBay.CargoType.Metal][testStation].amount);

        testStation.LoadCargo(500, CargoBay.CargoType.Metal);
        Assert.AreEqual(600, factionTrade.resourcesOffered[CargoBay.CargoType.Metal][testStation].amount);
        Assert.Zero(factionTrade.resourcesRequested[CargoBay.CargoType.Metal].Count);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoType.Metal, 500, 1200);
        Assert.AreEqual(600, factionTrade.resourcesOffered[CargoBay.CargoType.Metal][testStation].amount);
        Assert.AreEqual(100, factionTrade.resourcesRequested[CargoBay.CargoType.Metal][testStation].amount);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoType.Metal, 500, 800);
        Assert.AreEqual(600, factionTrade.resourcesOffered[CargoBay.CargoType.Metal][testStation].amount);
        Assert.Zero(factionTrade.resourcesRequested[CargoBay.CargoType.Metal].Count);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoType.Metal, 1150, 1200);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoType.Metal].Count);
        Assert.AreEqual(100, factionTrade.resourcesRequested[CargoBay.CargoType.Metal][testStation].amount);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestStationFreeReservedCargoRequest() {
        SetupTradeTests();
        FactionTrade factionTrade = testFaction.factionTrade;
        testStation.SetDesiredFreeCargoRange(CargoBay.CargoType.Gas, 500, 800);
        testStation.ReserveCargo(200, CargoBay.CargoType.Gas);
        Assert.AreEqual(1, factionTrade.resourcesRequested[CargoBay.CargoType.Gas].Count);
        Assert.AreEqual(1000, factionTrade.resourcesRequested[CargoBay.CargoType.Gas][testStation].amount);

        testStation.UnReserveCargo(200, CargoBay.CargoType.Gas);
        Assert.AreEqual(800, factionTrade.resourcesRequested[CargoBay.CargoType.Gas][testStation].amount);

        Assert.Zero(testStation.LoadCargo(200, CargoBay.CargoType.Gas));
        Assert.AreEqual(600, factionTrade.resourcesRequested[CargoBay.CargoType.Gas][testStation].amount);

        testStation.ReserveCargo(300, CargoBay.CargoType.Gas);
        Assert.AreEqual(900, factionTrade.resourcesRequested[CargoBay.CargoType.Gas][testStation].amount);
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoType.Gas].has);

    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestStationContractFulfillment() {
        SetupTradeTests();
        FactionTrade factionTrade = testFaction.factionTrade;
        factionTrade.MakeSellTradeAgreement(testFaction2);
        testFaction2.factionTrade.MakeSellTradeAgreement(testFaction);

        testShip.LoadCargo(200, CargoBay.CargoType.Gas);
        testStation.ReserveCargo(200, CargoBay.CargoType.Gas);
        FactionTrade.TradeContract tradeContract = new FactionTrade.TradeContract(testShip, testStation,
            new FactionTrade.TradeOffer(CargoBay.CargoType.Gas, 200, 1.2f));
        factionTrade.AddContract(tradeContract);
        Assert.AreEqual(1, factionTrade.activeContracts.Count);
        Assert.True(testStation.UnloadContractFromShip(400, tradeContract));
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoType.Gas].has);
        Assert.Zero(factionTrade.activeContracts.Count);

        testStation.UnReserveCargo(200, CargoBay.CargoType.Gas);
        Assert.AreEqual(200, factionTrade.resourcesOffered[CargoBay.CargoType.Gas][testStation].amount);

        FactionTrade.TradeContract contract2 = new FactionTrade.TradeContract(testStation, testShip,
            new FactionTrade.TradeOffer(CargoBay.CargoType.Gas, 200, 1.2f));
        factionTrade.AddContract(contract2);
        Assert.AreEqual(1, factionTrade.activeContracts.Count);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoType.Gas].Count);
        Assert.False(testStation.LoadTradeContractToShip(100, contract2));
        Assert.AreEqual(100, testShip.GetAllCargoOfType(CargoBay.CargoType.Gas));
        Assert.AreEqual(100, testStation.contractedCargo[CargoBay.CargoType.Gas].has);
        Assert.True(testStation.LoadTradeContractToShip(100, contract2));
        Assert.Zero(testStation.contractedCargo.Count);
        Assert.Zero(factionTrade.activeContracts.Count);
    }
}
