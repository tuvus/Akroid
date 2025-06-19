using System;
using System.Collections.Generic;
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
        foreach (CargoBay.CargoTypes cargoType in Enum.GetValues(typeof(CargoBay.CargoTypes))
            .Cast<CargoBay.CargoTypes>()) {
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

        Assert.Zero(testStation.LoadCargo(1000, CargoBay.CargoTypes.Metal));
        Assert.True(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].ContainsKey(testStation));
        var offer = factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal][testStation];
        Assert.AreEqual(CargoBay.CargoTypes.Metal, offer.cargoType);
        Assert.AreEqual(1000, offer.amount);

        Assert.Zero(testStation.UseCargo(1000, CargoBay.CargoTypes.Metal));
        Assert.True(factionTrade.resourcesOffered.All(type => type.Value.Count == 0));

        Assert.Zero(testShip.LoadCargo(200, CargoBay.CargoTypes.Gas));
        Assert.Zero(testStation.LoadCargoFromUnit(100, CargoBay.CargoTypes.Gas, testShip));
        Assert.True(factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas].ContainsKey(testStation));
        offer = factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas][testStation];
        Assert.AreEqual(CargoBay.CargoTypes.Gas, offer.cargoType);
        Assert.AreEqual(100, offer.amount);

        Assert.Zero(testStation.LoadCargoFromUnit(100, CargoBay.CargoTypes.Gas, testShip));
        Assert.True(factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas].ContainsKey(testStation));
        offer = factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas][testStation];
        Assert.AreEqual(CargoBay.CargoTypes.Gas, offer.cargoType);
        Assert.AreEqual(200, offer.amount);

        Assert.Zero(testStation.LoadCargo(400, CargoBay.CargoTypes.Gas));
        factionTrade.MakeSellTradeAgreement(testFaction2);
        FactionTrade.Contract contract = new FactionTrade.Contract(testStation, testShip,
            new FactionTrade.Offer(CargoBay.CargoTypes.Gas, 300, 1.4f));
        testStation.AddContract(contract);
        Assert.AreEqual(300, factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas][testStation].amount);

        FactionTrade.Contract contract2 = new FactionTrade.Contract(testStation, testShip,
            new FactionTrade.Offer(CargoBay.CargoTypes.Gas, 200, 1.4f));
        testStation.AddContract(contract2);

        Assert.AreEqual(100, factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas][testStation].amount);

        testStation.RemoveContract(contract);
        Assert.AreEqual(400, factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas][testStation].amount);

        testStation.RemoveContract(contract2);
        Assert.AreEqual(600, factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas][testStation].amount);
        Assert.Zero(testStation.contractedCargo.Count);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestStationResourceRequest() {
        SetupTradeTests();
        FactionTrade factionTrade = testFaction.factionTrade;
        Assert.False(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].ContainsKey(testStation));

        testStation.ReserveCargo(200, CargoBay.CargoTypes.Metal);
        Assert.True(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].ContainsKey(testStation));
        Assert.AreEqual(200, factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal][testStation].amount);

        Assert.Zero(testStation.LoadCargo(100, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(100, factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal][testStation].amount);

        Assert.Zero(testStation.LoadCargo(200, CargoBay.CargoTypes.Metal));
        Assert.False(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].ContainsKey(testStation));
        Assert.AreEqual(100, factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal][testStation].amount);

        testStation.UnReserveCargo(200, CargoBay.CargoTypes.Metal);
        Assert.AreEqual(300, factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal][testStation].amount);
        Assert.False(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].ContainsKey(testStation));
    }


    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestStationFreeCargoRequest() {
        SetupTradeTests();
        FactionTrade factionTrade = testFaction.factionTrade;
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].Count);
        Assert.Zero(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].Count);
        testStation.SetDesiredFreeCargoRange(CargoBay.CargoTypes.Metal, 500, 1000);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].Count);
        Assert.AreEqual(500, factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal][testStation].amount);

        Assert.Zero(testStation.LoadCargo(100, CargoBay.CargoTypes.Metal));
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].Count);
        Assert.AreEqual(400, factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal][testStation].amount);

        Assert.Zero(testStation.LoadCargo(500, CargoBay.CargoTypes.Metal));
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].Count);
        Assert.Zero(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].Count);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoTypes.Metal, 700, 1000);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].Count);
        Assert.AreEqual(100, factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal][testStation].amount);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoTypes.Metal, 500, 1000);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].Count);
        Assert.Zero(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].Count);

        testStation.LoadCargo(500, CargoBay.CargoTypes.Metal);
        Assert.AreEqual(100, factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal][testStation].amount);
        Assert.Zero(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].Count);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoTypes.Metal, 500, 1200);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].Count);
        Assert.Zero(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].Count);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoTypes.Metal, 500, 800);
        Assert.AreEqual(300, factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal][testStation].amount);
        Assert.Zero(factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal].Count);

        testStation.SetDesiredFreeCargoRange(CargoBay.CargoTypes.Metal, 1150, 1200);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoTypes.Metal].Count);
        Assert.AreEqual(50, factionTrade.resourcesRequested[CargoBay.CargoTypes.Metal][testStation].amount);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestStationContractFulfillment() {
        SetupTradeTests();
        FactionTrade factionTrade = testFaction.factionTrade;
        factionTrade.MakeSellTradeAgreement(testFaction2);
        testFaction2.factionTrade.MakeSellTradeAgreement(testFaction);

        testShip.LoadCargo(200, CargoBay.CargoTypes.Gas);
        testStation.ReserveCargo(200, CargoBay.CargoTypes.Gas);
        FactionTrade.Contract contract = new FactionTrade.Contract(testShip, testStation,
            new FactionTrade.Offer(CargoBay.CargoTypes.Gas, 200, 1.2f));
        testStation.AddContract(contract);
        Assert.AreEqual(1, factionTrade.activeContracts.Count);
        Assert.True(testStation.UnloadContractFromShip(400, contract));
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoTypes.Gas].has);
        Assert.Zero(factionTrade.activeContracts.Count);

        testStation.UnReserveCargo(200, CargoBay.CargoTypes.Gas);
        Assert.AreEqual(200, factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas][testStation].amount);

        FactionTrade.Contract contract2 = new FactionTrade.Contract(testStation, testShip,
            new FactionTrade.Offer(CargoBay.CargoTypes.Gas, 200, 1.2f));
        testStation.AddContract(contract2);
        Assert.AreEqual(1, factionTrade.activeContracts.Count);
        Assert.Zero(factionTrade.resourcesOffered[CargoBay.CargoTypes.Gas].Count);
        Assert.False(testStation.LoadContractToShip(100, contract2));
        Assert.AreEqual(100, testShip.GetAllCargoOfType(CargoBay.CargoTypes.Gas));
        Assert.AreEqual(100, testStation.contractedCargo[CargoBay.CargoTypes.Gas].has);
        Assert.True(testStation.LoadContractToShip(100, contract2));
        Assert.Zero(testStation.contractedCargo.Count);
        Assert.Zero(factionTrade.activeContracts.Count);
    }
}
