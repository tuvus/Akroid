using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEngine;

public class CargoBayTests {
    [Explicit, Category("Unit Tests")]
    [Test]
    public void TestCargoBaySetup() {
        var module = new Mock<IModule>();
        module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
        module.Setup(e => e.GetRotation()).Returns(0);
        var unit = new Mock<Unit>();
        var cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 6;
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);
        CargoBay cargoBay = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        Assert.False(cargoBay.cargoBays.ContainsKey(CargoBay.CargoTypes.All));
        Assert.AreEqual(100, cargoBay.GetCargoBayCapacity());
        Assert.AreEqual(6, cargoBay.GetMaxCargoBays());

        foreach (var cargoType in Enum.GetValues(typeof(CargoBay.CargoTypes)).Cast<CargoBay.CargoTypes>()) {
            Assert.AreEqual(0, cargoBay.GetAllCargo(cargoType));
            Assert.AreEqual(600, cargoBay.GetOpenCargoCapacityOfType(cargoType));
        }
    }

    [Explicit, Category("Unit Tests")]
    [Test]
    public void TestAddingAndRemovingCargo() {
        var module = new Mock<IModule>();
        module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
        module.Setup(e => e.GetRotation()).Returns(0);
        var unit = new Mock<Unit>();
        var cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 6;
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);
        CargoBay cargoBay = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        Assert.AreEqual(0, cargoBay.LoadCargo(100, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(100, cargoBay.GetAllCargo(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(100, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(200, cargoBay.GetAllCargo(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(0, cargoBay.UseCargo(100, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(100, cargoBay.GetAllCargo(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(100, cargoBay.UseCargo(200, CargoBay.CargoTypes.Metal));
        // Cargo bay is now empty
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoTypes.All));

        Assert.AreEqual(200, cargoBay.UseCargo(200, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(600, CargoBay.CargoTypes.Gas));
        Assert.AreEqual(100, cargoBay.UseCargo(100, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(100, cargoBay.LoadCargo(100, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(100, cargoBay.LoadCargo(100, CargoBay.CargoTypes.Gas));
        Assert.AreEqual(0, cargoBay.UseCargo(50, CargoBay.CargoTypes.Gas));
        Assert.AreEqual(100, cargoBay.LoadCargo(100, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(50, CargoBay.CargoTypes.Gas));
        Assert.AreEqual(0, cargoBay.UseCargo(600, CargoBay.CargoTypes.Gas));
        // Cargo Bays is now empty
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoTypes.All));
        Assert.AreEqual(0, cargoBay.LoadCargo(50, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(550, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(500, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Gas));
    }

    [Explicit, Category("Unit Tests")]
    [Test]
    public void TestReservedBays() {
        var module = new Mock<IModule>();
        module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
        module.Setup(e => e.GetRotation()).Returns(0);
        var unit = new Mock<Unit>();
        var cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 6;
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);
        CargoBay cargoBay = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        Assert.AreEqual(0, cargoBay.LoadCargo(400, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(200, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Gas));
        cargoBay.AddReservedCargoBays(CargoBay.CargoTypes.Metal, 2);
        Assert.AreEqual(200, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Gas));
        Assert.AreEqual(100, cargoBay.LoadCargo(300, CargoBay.CargoTypes.Gas));
        // 400 metal, 200 gas, 2 bays reserved for metal
        Assert.AreEqual(0, cargoBay.UseCargo(400, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(200, cargoBay.LoadCargo(400, CargoBay.CargoTypes.Gas));
        Assert.AreEqual(50, cargoBay.LoadCargo(50, CargoBay.CargoTypes.Gas));
        Assert.AreEqual(0, cargoBay.LoadCargo(200, CargoBay.CargoTypes.Metal));
        Assert.AreEqual(0, cargoBay.UseCargo(400, CargoBay.CargoTypes.Gas));
        Assert.AreEqual(400, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Gas));
        cargoBay.AddReservedCargoBays(CargoBay.CargoTypes.Metal, 2);
        Assert.AreEqual(400, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(200, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Gas));
        // 200 Metal, 4 bays reserved for metal
        Assert.AreEqual(200, cargoBay.LoadCargo(400, CargoBay.CargoTypes.Gas));
        Assert.AreEqual(0, cargoBay.UseCargo(200, CargoBay.CargoTypes.Gas));
        cargoBay.AddReservedCargoBays(CargoBay.CargoTypes.Gas, 2);
        Assert.AreEqual(200, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal));
    }

    [Explicit, Category("Unit Tests")]
    [Test]
    public void TestCargoBayLoadingFromAnotherCargoBay() {
        var module = new Mock<IModule>();
        module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
        module.Setup(e => e.GetRotation()).Returns(0);
        var unit = new Mock<Unit>();
        var cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 6;
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);
        CargoBay cargoBay = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        CargoBay cargoBay2 = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        cargoBay.LoadCargoFromBay(cargoBay2, CargoBay.CargoTypes.Metal, 800);
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoTypes.All));
        Assert.AreEqual(0, cargoBay2.GetAllCargo(CargoBay.CargoTypes.All));
        Assert.AreEqual(0, cargoBay.LoadCargo(200, CargoBay.CargoTypes.Metal));
        cargoBay2.LoadCargoFromBay(cargoBay, CargoBay.CargoTypes.Metal, 400);
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(200, cargoBay2.GetAllCargo(CargoBay.CargoTypes.Metal));
        cargoBay.LoadCargo(600, CargoBay.CargoTypes.Metal);
        cargoBay2.LoadCargoFromBay(cargoBay, CargoBay.CargoTypes.Metal);
        Assert.AreEqual(200, cargoBay.GetAllCargo(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(600, cargoBay2.GetAllCargo(CargoBay.CargoTypes.Metal));
        cargoBay.LoadCargoFromBay(cargoBay2, CargoBay.CargoTypes.Metal, 50);
        Assert.AreEqual(250, cargoBay.GetAllCargo(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(550, cargoBay2.GetAllCargo(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(350, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(100, CargoBay.CargoTypes.Gas));
        Assert.AreEqual(0, cargoBay2.UseCargo(500, CargoBay.CargoTypes.Metal));
        cargoBay2.LoadCargoFromBay(cargoBay, CargoBay.CargoTypes.All);
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoTypes.All));
        Assert.AreEqual(400, cargoBay2.GetAllCargo(CargoBay.CargoTypes.All));
        Assert.AreEqual(300, cargoBay2.GetAllCargo(CargoBay.CargoTypes.Metal));
        Assert.AreEqual(100, cargoBay2.GetAllCargo(CargoBay.CargoTypes.Gas));
    }

}
