using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class StationTradeTests {
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
    public void StationTradeSetup() {
        SetupTradeTests();
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoType.All, true));
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.Zero(testStation.contractedCargo.Count);
        Assert.Zero(testStation.freeCargo.Count);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void StationReservingCargo() {
        SetupTradeTests();
        testStation.ReserveCargo(200, CargoBay.CargoType.Metal);
        Assert.True(testStation.reservedCargo.ContainsKey(CargoBay.CargoType.Metal));
        Assert.Zero(testStation.reservedCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));

        testStation.ReserveCargo(300, CargoBay.CargoType.Metal);
        Assert.Zero(testStation.reservedCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));

        Assert.Zero(testStation.LoadCargo(200, CargoBay.CargoType.Metal));
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoType.Metal].has);
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true));
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoType.Metal].has);
        Assert.Zero(testStation.contractedCargo.Count);
        Assert.Zero(testStation.freeCargo.Count);

        Assert.Zero(testStation.UseCargo(100, CargoBay.CargoType.Metal));
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.AreEqual(100, testStation.reservedCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(100,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoType.Metal)));

        Assert.Zero(testStation.LoadCargo(700, CargoBay.CargoType.Metal));
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoType.Metal].has);
        Assert.True(testStation.freeCargo.ContainsKey(CargoBay.CargoType.Metal));
        Assert.AreEqual(300, testStation.freeCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(300, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(800, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true));
        Assert.AreEqual(800,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoType.Metal)));

        Assert.Zero(testStation.UseCargo(200, CargoBay.CargoType.Metal));
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(100, testStation.freeCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(100, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoType.Metal)));

        testStation.UnReserveCargo(300, CargoBay.CargoType.Metal);
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(400, testStation.freeCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(400, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoType.Metal)));

        testStation.UnReserveCargo(200, CargoBay.CargoType.Metal);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(600, testStation.freeCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoType.Metal)));

        testStation.ReserveCargo(600, CargoBay.CargoType.Metal);
        Assert.AreEqual(1, testStation.reservedCargo.Count);
        Assert.AreEqual(600, testStation.reservedCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(600, testStation.reservedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.Zero(testStation.freeCargo.Count);
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoType.Metal)));
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void StationSupplyContractCargo() {
        SetupTradeTests();
        testFaction.factionTrade.MakeSellTradeAgreement(testFaction2);
        FactionTrade.TradeContract tradeContract = new FactionTrade.TradeContract(testStation, testShip,
            new FactionTrade.Offer(CargoBay.CargoType.Metal, 400, 1.2f));
        testFaction2.AddCredits(10000000);
        testFaction.factionTrade.AddContract(tradeContract, false);
        Assert.AreEqual(1, testStation.contractedCargo.Count);
        Assert.True(testStation.contractedCargo.ContainsKey(CargoBay.CargoType.Metal));
        Assert.AreEqual(400, testStation.contractedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.Zero(testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));

        Assert.False(testStation.LoadContractToShip(200, tradeContract));
        Assert.AreEqual(1, testStation.contractedCargo.Count);
        Assert.True(testStation.contractedCargo.ContainsKey(CargoBay.CargoType.Metal));
        Assert.AreEqual(400, testStation.contractedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.Zero(testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(400, tradeContract.cargo[CargoBay.CargoType.Metal].amount);

        Assert.Zero(testStation.LoadCargo(200, CargoBay.CargoType.Metal));
        Assert.AreEqual(400, testStation.contractedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.AreEqual(200, testStation.contractedCargo[CargoBay.CargoType.Metal].has);
        Assert.Zero(testStation.freeCargo.Count);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true, false));
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, false, true));

        Assert.False(testStation.LoadContractToShip(100, tradeContract));
        Assert.AreEqual(300, testStation.contractedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.AreEqual(100, testStation.contractedCargo[CargoBay.CargoType.Metal].has);
        Assert.Zero(testStation.freeCargo.Count);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true, false));
        Assert.AreEqual(100, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, false, true));
        Assert.AreEqual(100, testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(300, tradeContract.cargo[CargoBay.CargoType.Metal].amount);

        Assert.False(testStation.LoadContractToShip(200, tradeContract));
        Assert.AreEqual(200, testStation.contractedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.Zero(testStation.contractedCargo[CargoBay.CargoType.Metal].has);
        Assert.Zero(testStation.freeCargo.Count);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true, false));
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, false, true));
        Assert.AreEqual(200, testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(200, tradeContract.cargo[CargoBay.CargoType.Metal].amount);

        Assert.Zero(testStation.LoadCargo(400, CargoBay.CargoType.Metal));
        Assert.AreEqual(200, testStation.contractedCargo[CargoBay.CargoType.Metal].wanted);
        Assert.AreEqual(200, testStation.contractedCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(200, testStation.freeCargo[CargoBay.CargoType.Metal].has);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true, false));
        Assert.AreEqual(400, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, false, true));
        Assert.AreEqual(200, testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(200, tradeContract.cargo[CargoBay.CargoType.Metal].amount);

        Assert.True(testStation.LoadContractToShip(400, tradeContract));
        Assert.Zero(testStation.contractedCargo.Count);
        Assert.AreEqual(1, testStation.freeCargo.Count);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, true, false));
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal, false, true));
        Assert.AreEqual(400, testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.Zero(tradeContract.cargo.Count);

    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void StationRequestContractCargo() {
        SetupTradeTests();
        testFaction2.factionTrade.MakeSellTradeAgreement(testFaction);
        FactionTrade.TradeContract tradeContract = new FactionTrade.TradeContract(testShip, testStation,
            new FactionTrade.Offer(CargoBay.CargoType.Metal, 400, 1.2f));
        testFaction2.AddCredits(10000000);
        testFaction.factionTrade.AddContract(tradeContract, false);
        Assert.Zero(testShip.LoadCargo(200, CargoBay.CargoType.Metal));
        Assert.AreEqual(0, testStation.contractedCargo.Count);
        Assert.AreEqual(400, tradeContract.cargo[CargoBay.CargoType.Metal].amount);

        Assert.False(testStation.UnloadContractFromShip(100, tradeContract));
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(100, testStation.freeCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(100, testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(100, testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(300, tradeContract.cargo[CargoBay.CargoType.Metal].amount);

        Assert.False(testStation.UnloadContractFromShip(200, tradeContract));
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(200, testStation.freeCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(0, testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(200, tradeContract.cargo[CargoBay.CargoType.Metal].amount);

        Assert.Zero(testShip.LoadCargo(400, CargoBay.CargoType.Metal));
        Assert.True(testStation.UnloadContractFromShip(400, tradeContract));
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(400, testStation.freeCargo[CargoBay.CargoType.Metal].has);
        Assert.AreEqual(400, testStation.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(200, testShip.GetAllCargoOfType(CargoBay.CargoType.Metal));
        Assert.Zero(tradeContract.cargo.Count);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void AddAndRemoveContract() {
        SetupTradeTests();
        testFaction.factionTrade.MakeSellTradeAgreement(testFaction2);
        FactionTrade.TradeContract tradeContract = new FactionTrade.TradeContract(testStation, testShip,
            new FactionTrade.Offer(CargoBay.CargoType.Metal, 400, 1.2f));
        Assert.AreEqual(0,testStation.contractedCargo.Count);

        testFaction.factionTrade.AddContract(tradeContract, false);
        Assert.AreEqual(1, testStation.contractedCargo.Count);
        Assert.AreEqual(400, testStation.contractedCargo[CargoBay.CargoType.Metal].wanted);

        testFaction.factionTrade.RemoveContract(tradeContract);
        Assert.AreEqual(0, testStation.contractedCargo.Count);
    }
}
