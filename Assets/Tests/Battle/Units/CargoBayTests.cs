using System;
using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEngine;

public class CargoBayTests {
    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestCargoBaySetup() {
        var module = new Mock<IModule>();
        module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
        module.Setup(e => e.GetRotation()).Returns(0);
        var unit = new Mock<Unit>();
        CargoBayScriptableObject cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 6;
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);
        CargoBay cargoBay = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        Assert.False(cargoBay.cargoBays.ContainsKey(CargoBay.CargoType.All));
        Assert.AreEqual(100, cargoBay.GetCargoBayCapacity());
        Assert.AreEqual(6, cargoBay.GetMaxCargoBays());

        foreach (CargoBay.CargoType cargoType in CargoBay.allCargoTypes) {
            Assert.AreEqual(0, cargoBay.GetAllCargo(cargoType));
            Assert.AreEqual(600, cargoBay.GetOpenCargoCapacityOfType(cargoType));
        }
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestAddingAndRemovingCargo() {
        var module = new Mock<IModule>();
        module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
        module.Setup(e => e.GetRotation()).Returns(0);
        var unit = new Mock<Unit>();
        CargoBayScriptableObject cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 6;
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);
        CargoBay cargoBay = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        Assert.AreEqual(0, cargoBay.LoadCargo(100, CargoBay.CargoType.Metal));
        Assert.AreEqual(100, cargoBay.GetAllCargo(CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(100, CargoBay.CargoType.Metal));
        Assert.AreEqual(200, cargoBay.GetAllCargo(CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.UseCargo(100, CargoBay.CargoType.Metal));
        Assert.AreEqual(100, cargoBay.GetAllCargo(CargoBay.CargoType.Metal));
        Assert.AreEqual(100, cargoBay.UseCargo(200, CargoBay.CargoType.Metal));
        // Cargo bay is now empty
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoType.All));

        Assert.AreEqual(200, cargoBay.UseCargo(200, CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(600, CargoBay.CargoType.Gas));
        Assert.AreEqual(100, cargoBay.UseCargo(100, CargoBay.CargoType.Metal));
        Assert.AreEqual(100, cargoBay.LoadCargo(100, CargoBay.CargoType.Metal));
        Assert.AreEqual(100, cargoBay.LoadCargo(100, CargoBay.CargoType.Gas));
        Assert.AreEqual(0, cargoBay.UseCargo(50, CargoBay.CargoType.Gas));
        Assert.AreEqual(100, cargoBay.LoadCargo(100, CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(50, CargoBay.CargoType.Gas));
        Assert.AreEqual(0, cargoBay.UseCargo(600, CargoBay.CargoType.Gas));
        // Cargo Bays is now empty
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoType.All));
        Assert.AreEqual(0, cargoBay.LoadCargo(50, CargoBay.CargoType.Metal));
        Assert.AreEqual(550, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(500, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoType.Gas));
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestCargoBayLoadingFromAnotherCargoBay() {
        var module = new Mock<IModule>();
        module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
        module.Setup(e => e.GetRotation()).Returns(0);
        var unit = new Mock<Unit>();
        CargoBayScriptableObject cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 6;
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);
        CargoBay cargoBay = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        CargoBay cargoBay2 = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        cargoBay.LoadCargoFromBay(cargoBay2, CargoBay.CargoType.Metal, 800);
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoType.All));
        Assert.AreEqual(0, cargoBay2.GetAllCargo(CargoBay.CargoType.All));
        Assert.AreEqual(0, cargoBay.LoadCargo(200, CargoBay.CargoType.Metal));
        cargoBay2.LoadCargoFromBay(cargoBay, CargoBay.CargoType.Metal, 400);
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoType.Metal));
        Assert.AreEqual(200, cargoBay2.GetAllCargo(CargoBay.CargoType.Metal));
        cargoBay.LoadCargo(600, CargoBay.CargoType.Metal);
        cargoBay2.LoadCargoFromBay(cargoBay, CargoBay.CargoType.Metal);
        Assert.AreEqual(200, cargoBay.GetAllCargo(CargoBay.CargoType.Metal));
        Assert.AreEqual(600, cargoBay2.GetAllCargo(CargoBay.CargoType.Metal));
        cargoBay.LoadCargoFromBay(cargoBay2, CargoBay.CargoType.Metal, 50);
        Assert.AreEqual(250, cargoBay.GetAllCargo(CargoBay.CargoType.Metal));
        Assert.AreEqual(550, cargoBay2.GetAllCargo(CargoBay.CargoType.Metal));
        Assert.AreEqual(350, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(100, CargoBay.CargoType.Gas));
        Assert.AreEqual(0, cargoBay2.UseCargo(500, CargoBay.CargoType.Metal));
        cargoBay2.LoadCargoFromBay(cargoBay, CargoBay.CargoType.All);
        Assert.AreEqual(0, cargoBay.GetAllCargo(CargoBay.CargoType.All));
        Assert.AreEqual(400, cargoBay2.GetAllCargo(CargoBay.CargoType.All));
        Assert.AreEqual(300, cargoBay2.GetAllCargo(CargoBay.CargoType.Metal));
        Assert.AreEqual(100, cargoBay2.GetAllCargo(CargoBay.CargoType.Gas));
    }

        [Explicit] [Category("Unit Tests")]
    [Test]
    public void TestAvailableCargoSpace() {
        var module = new Mock<IModule>();
        module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
        module.Setup(e => e.GetRotation()).Returns(0);
        var unit = new Mock<Unit>();
        CargoBayScriptableObject cargoBayScriptableObject = ScriptableObject.CreateInstance<CargoBayScriptableObject>();
        cargoBayScriptableObject.cargoBaySize = 100;
        cargoBayScriptableObject.maxCargoBays = 6;
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);
        CargoBay cargoBay = new CargoBay(battleManager.Object, module.Object, unit.Object, cargoBayScriptableObject);
        Assert.AreEqual(0, cargoBay.LoadCargo(100, CargoBay.CargoType.Metal));
        Assert.AreEqual(500, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(50, CargoBay.CargoType.Metal));
        Assert.AreEqual(450, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(149, CargoBay.CargoType.Metal));
        Assert.AreEqual(301, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.UseCargo(149, CargoBay.CargoType.Metal));
        Assert.AreEqual(450, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.LoadCargo(450, CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoType.Metal));
        Assert.AreEqual(0, cargoBay.UseCargo(600, CargoBay.CargoType.Metal));
        Assert.AreEqual(600, cargoBay.GetOpenCargoCapacityOfType(CargoBay.CargoType.Metal));
    }
}
