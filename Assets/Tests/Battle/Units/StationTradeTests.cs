using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEngine;

public class StationTradeTests {
    private TestBattleManager battleManager;
    private Station testStation;
    private Faction testFaction;

    void SetupTradeTests() {
        battleManager = new TestBattleManager();
        testFaction = battleManager.CreateTestFaction();
        CargoBayScriptableObject cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 12;
        testStation = battleManager.CreateNewStation(new BattleObject.BattleObjectData("TestStation", testFaction),
            ScriptableObject.CreateInstance<TestStationScriptableObject>()
                .SetupScriptableObject(cargoBayScriptableObject), true);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void StationTradeSetup() {
        SetupTradeTests();
        Assert.AreEqual(0, testStation.GetAllCargoOfType(CargoBay.CargoTypes.All, true));
        Assert.AreEqual(0, testStation.reservedCargo.Count);
        Assert.AreEqual(0, testStation.contractedCargo.Count);
        Assert.AreEqual(0, testStation.freeCargo.Count);
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void StationReservingCargo() {
        SetupTradeTests();
        testStation.AddReservedCargo(200, CargoBay.CargoTypes.Metal);
        Assert.True(testStation.reservedCargo.ContainsKey(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(0, testStation.reservedCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(0, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal));

        testStation.AddReservedCargo(300, CargoBay.CargoTypes.Metal);
        Assert.AreEqual(0, testStation.reservedCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(0, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal));

        Assert.AreEqual(0, testStation.LoadCargo(200, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(0, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(200, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal, true));
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(0, testStation.contractedCargo.Count);
        Assert.AreEqual(0, testStation.freeCargo.Count);

        Assert.AreEqual(0, testStation.UseCargo(100, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(100, testStation.reservedCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(100,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoTypes.Metal)));

        Assert.AreEqual(0, testStation.LoadCargo(700, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoTypes.Metal].has);
        Assert.True(testStation.freeCargo.ContainsKey(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(300, testStation.freeCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(0, testStation.freeCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(300, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(800, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal, true));
        Assert.AreEqual(800,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoTypes.Metal)));

        Assert.AreEqual(0, testStation.UseCargo(200, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(500, testStation.reservedCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(100, testStation.freeCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(0, testStation.freeCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(100, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoTypes.Metal)));

        testStation.RemoveReservedCargo(300, CargoBay.CargoTypes.Metal);
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(200, testStation.reservedCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(400, testStation.freeCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(0, testStation.freeCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(400, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoTypes.Metal)));

        testStation.RemoveReservedCargo(200, CargoBay.CargoTypes.Metal);
        Assert.AreEqual(0, testStation.reservedCargo.Count);
        Assert.AreEqual(600, testStation.freeCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(0, testStation.freeCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoTypes.Metal)));

        testStation.AddReservedCargo(600, CargoBay.CargoTypes.Metal);
        Assert.AreEqual(1, testStation.reservedCargo.Count);
        Assert.AreEqual(600, testStation.reservedCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(600, testStation.reservedCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(0, testStation.freeCargo[CargoBay.CargoTypes.Metal].has);
        Assert.AreEqual(0, testStation.freeCargo[CargoBay.CargoTypes.Metal].wanted);
        Assert.AreEqual(0, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(600, testStation.GetAllCargoOfType(CargoBay.CargoTypes.Metal, true));
        Assert.AreEqual(600,
            testStation.moduleSystem.Get<CargoBay>().Sum(c => c.GetAllCargo(CargoBay.CargoTypes.Metal)));
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void StationContractCargo() {
        SetupTradeTests();


    }

    class TestStationScriptableObject : StationScriptableObject {
        public TestStationScriptableObject SetupScriptableObject(ComponentScriptableObject componentScriptableObject) {
            var module = new Mock<IModule>();
            module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
            module.Setup(e => e.GetRotation()).Returns(0);
            module.Setup(e => e.GetSystemIndex()).Returns(0);
            modules = new[] { module.Object };
            var system = new ModuleSystem.System("TestSystem", PrefabModuleSystem.SystemType.Any);
            system.moduleCount = 1;
            system.component = componentScriptableObject;
            systems = new[] { system };
            return this;
        }
    }
}
