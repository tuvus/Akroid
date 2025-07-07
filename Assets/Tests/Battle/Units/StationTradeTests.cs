using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CargoTypes = CargoBay.CargoTypes;

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
        Assert.Zero(testStation.GetAllCargoOfType(CargoBay.CargoTypes.All, true));
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.Zero(testStation.contractedCargo.Count);
        Assert.Zero(testStation.freeCargo.Count);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void StationReservingCargo() {
        SetupTradeTests();
        testStation.ReserveCargo(200, CargoTypes.Metal);
        Assert.True(testStation.reservedCargo.ContainsKey(CargoTypes.Metal));
        Assert.Zero(testStation.reservedCargo[CargoTypes.Metal].has);
        Assert.AreEqual(200, testStation.reservedCargo[CargoTypes.Metal].wanted);
        Assert.Zero(testStation.GetAllCargoOfType(CargoTypes.Metal));

        testStation.ReserveCargo(300, CargoTypes.Metal);
        Assert.Zero(testStation.reservedCargo[CargoTypes.Metal].has);
        Assert.AreEqual(500, testStation.reservedCargo[CargoTypes.Metal].wanted);
        Assert.Zero(testStation.GetAllCargoOfType(CargoTypes.Metal));

        Assert.Zero(testStation.LoadCargo(200, CargoTypes.Metal));
        Assert.AreEqual(200, testStation.reservedCargo[CargoTypes.Metal].has);
        Assert.Zero(testStation.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoTypes.Metal, true));
        Assert.AreEqual(500, testStation.reservedCargo[CargoTypes.Metal].wanted);
        Assert.AreEqual(200, testStation.reservedCargo[CargoTypes.Metal].has);
        Assert.Zero(testStation.contractedCargo.Count);
        Assert.Zero(testStation.freeCargo.Count);

        Assert.Zero(testStation.UseCargo(100, CargoTypes.Metal));
        Assert.AreEqual(500, testStation.reservedCargo[CargoTypes.Metal].wanted);
        Assert.AreEqual(100, testStation.reservedCargo[CargoTypes.Metal].has);
        Assert.AreEqual(100,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoTypes.Metal)));

        Assert.Zero(testStation.LoadCargo(700, CargoTypes.Metal));
        Assert.AreEqual(500, testStation.reservedCargo[CargoTypes.Metal].wanted);
        Assert.AreEqual(500, testStation.reservedCargo[CargoTypes.Metal].has);
        Assert.True(testStation.freeCargo.ContainsKey(CargoTypes.Metal));
        Assert.AreEqual(300, testStation.freeCargo[CargoTypes.Metal].has);
        Assert.AreEqual(300, testStation.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(800, testStation.GetAllCargoOfType(CargoTypes.Metal, true));
        Assert.AreEqual(800,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoTypes.Metal)));

        Assert.Zero(testStation.UseCargo(200, CargoTypes.Metal));
        Assert.AreEqual(500, testStation.reservedCargo[CargoTypes.Metal].wanted);
        Assert.AreEqual(500, testStation.reservedCargo[CargoTypes.Metal].has);
        Assert.AreEqual(100, testStation.freeCargo[CargoTypes.Metal].has);
        Assert.AreEqual(100, testStation.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoTypes.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoTypes.Metal)));

        testStation.UnReserveCargo(300, CargoTypes.Metal);
        Assert.AreEqual(200, testStation.reservedCargo[CargoTypes.Metal].wanted);
        Assert.AreEqual(200, testStation.reservedCargo[CargoTypes.Metal].has);
        Assert.AreEqual(400, testStation.freeCargo[CargoTypes.Metal].has);
        Assert.AreEqual(400, testStation.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoTypes.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoTypes.Metal)));

        testStation.UnReserveCargo(200, CargoTypes.Metal);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(600, testStation.freeCargo[CargoTypes.Metal].has);
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoTypes.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoTypes.Metal)));

        testStation.ReserveCargo(600, CargoTypes.Metal);
        Assert.AreEqual(1, testStation.reservedCargo.Count);
        Assert.AreEqual(600, testStation.reservedCargo[CargoTypes.Metal].has);
        Assert.AreEqual(600, testStation.reservedCargo[CargoTypes.Metal].wanted);
        Assert.Zero(testStation.freeCargo.Count);
        Assert.Zero(testStation.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoTypes.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoTypes.Metal)));
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void StationSupplyContractCargo() {
        SetupTradeTests();
        testFaction.factionTrade.MakeSellTradeAgreement(testFaction2);
        FactionTrade.Contract contract = new FactionTrade.Contract(testStation, testShip,
            new FactionTrade.Offer(CargoTypes.Metal, 400, 1.2f));
        testFaction2.AddCredits(10000000);
        testFaction.factionTrade.AddContract(contract, false);
        Assert.AreEqual(1, testStation.contractedCargo.Count);
        Assert.True(testStation.contractedCargo.ContainsKey(CargoTypes.Metal));
        Assert.AreEqual(400, testStation.contractedCargo[CargoTypes.Metal].wanted);
        Assert.Zero(testShip.GetAllCargoOfType(CargoTypes.Metal));

        Assert.False(testStation.LoadContractToShip(200, contract));
        Assert.AreEqual(1, testStation.contractedCargo.Count);
        Assert.True(testStation.contractedCargo.ContainsKey(CargoTypes.Metal));
        Assert.AreEqual(400, testStation.contractedCargo[CargoTypes.Metal].wanted);
        Assert.Zero(testShip.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(400, contract.cargo[CargoTypes.Metal].amount);

        Assert.Zero(testStation.LoadCargo(200, CargoTypes.Metal));
        Assert.AreEqual(400, testStation.contractedCargo[CargoTypes.Metal].wanted);
        Assert.AreEqual(200, testStation.contractedCargo[CargoTypes.Metal].has);
        Assert.Zero(testStation.freeCargo.Count);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.Zero(testStation.GetAllCargoOfType(CargoTypes.Metal, true, false));
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoTypes.Metal, false, true));

        Assert.False(testStation.LoadContractToShip(100, contract));
        Assert.AreEqual(300, testStation.contractedCargo[CargoTypes.Metal].wanted);
        Assert.AreEqual(100, testStation.contractedCargo[CargoTypes.Metal].has);
        Assert.Zero(testStation.freeCargo.Count);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.Zero(testStation.GetAllCargoOfType(CargoTypes.Metal, true, false));
        Assert.AreEqual(100, testStation.GetAllCargoOfType(CargoTypes.Metal, false, true));
        Assert.AreEqual(100, testShip.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(300, contract.cargo[CargoTypes.Metal].amount);

        Assert.False(testStation.LoadContractToShip(200, contract));
        Assert.AreEqual(200, testStation.contractedCargo[CargoTypes.Metal].wanted);
        Assert.Zero(testStation.contractedCargo[CargoTypes.Metal].has);
        Assert.Zero(testStation.freeCargo.Count);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.Zero(testStation.GetAllCargoOfType(CargoTypes.Metal, true, false));
        Assert.Zero(testStation.GetAllCargoOfType(CargoTypes.Metal, false, true));
        Assert.AreEqual(200, testShip.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(200, contract.cargo[CargoTypes.Metal].amount);

        Assert.Zero(testStation.LoadCargo(400, CargoTypes.Metal));
        Assert.AreEqual(200, testStation.contractedCargo[CargoTypes.Metal].wanted);
        Assert.AreEqual(200, testStation.contractedCargo[CargoTypes.Metal].has);
        Assert.AreEqual(200, testStation.freeCargo[CargoTypes.Metal].has);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoTypes.Metal, true, false));
        Assert.AreEqual(400, testStation.GetAllCargoOfType(CargoTypes.Metal, false, true));
        Assert.AreEqual(200, testShip.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(200, contract.cargo[CargoTypes.Metal].amount);

        Assert.True(testStation.LoadContractToShip(400, contract));
        Assert.Zero(testStation.contractedCargo.Count);
        Assert.AreEqual(1, testStation.freeCargo.Count);
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoTypes.Metal, true, false));
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoTypes.Metal, false, true));
        Assert.AreEqual(400, testShip.GetAllCargoOfType(CargoTypes.Metal));
        Assert.Zero(contract.cargo.Count);

    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void StationRequestContractCargo() {
        SetupTradeTests();
        testFaction2.factionTrade.MakeSellTradeAgreement(testFaction);
        FactionTrade.Contract contract = new FactionTrade.Contract(testShip, testStation,
            new FactionTrade.Offer(CargoTypes.Metal, 400, 1.2f));
        testFaction2.AddCredits(10000000);
        testFaction.factionTrade.AddContract(contract, false);
        Assert.Zero(testShip.LoadCargo(200, CargoTypes.Metal));
        Assert.AreEqual(0, testStation.contractedCargo.Count);
        Assert.AreEqual(400, contract.cargo[CargoTypes.Metal].amount);

        Assert.False(testStation.UnloadContractFromShip(100, contract));
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(100, testStation.freeCargo[CargoTypes.Metal].has);
        Assert.AreEqual(100, testShip.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(100, testShip.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(300, contract.cargo[CargoTypes.Metal].amount);

        Assert.False(testStation.UnloadContractFromShip(200, contract));
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(200, testStation.freeCargo[CargoTypes.Metal].has);
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(0, testShip.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(200, contract.cargo[CargoTypes.Metal].amount);

        Assert.Zero(testShip.LoadCargo(400, CargoTypes.Metal));
        Assert.True(testStation.UnloadContractFromShip(400, contract));
        Assert.Zero(testStation.reservedCargo.Count);
        Assert.AreEqual(400, testStation.freeCargo[CargoTypes.Metal].has);
        Assert.AreEqual(400, testStation.GetAllCargoOfType(CargoTypes.Metal));
        Assert.AreEqual(200, testShip.GetAllCargoOfType(CargoTypes.Metal));
        Assert.Zero(contract.cargo.Count);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void AddAndRemoveContract() {
        SetupTradeTests();
        testFaction.factionTrade.MakeSellTradeAgreement(testFaction2);
        FactionTrade.Contract contract = new FactionTrade.Contract(testStation, testShip,
            new FactionTrade.Offer(CargoTypes.Metal, 400, 1.2f));
        Assert.AreEqual(0,testStation.contractedCargo.Count);

        testFaction.factionTrade.AddContract(contract, false);
        Assert.AreEqual(1, testStation.contractedCargo.Count);
        Assert.AreEqual(400, testStation.contractedCargo[CargoTypes.Metal].wanted);

        testFaction.factionTrade.RemoveContract(contract);
        Assert.AreEqual(0, testStation.contractedCargo.Count);
    }
}
