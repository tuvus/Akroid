using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEngine;

public class ModuleSystemTests {
    [Explicit] [Category("Unit Tests")]
    [Test]
    public void SetupFromScriptableObject() {
        // Setup
        MockUnitScriptableObject unitScriptableObject = ScriptableObject.CreateInstance<MockUnitScriptableObject>();
        MockFaction faction = new MockFaction();
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);

        ThrusterScriptableObject component = ScriptableObject.CreateInstance<ThrusterScriptableObject>();
        component.name = "TestThruster";
        component.cost = 10;
        component.thrustSpeed = 1000;
        component.color = Color.blue;
        component.resourceTypes = new List<CargoBay.CargoType> { CargoBay.CargoType.Gas };
        component.resourceCosts = new List<long> { 55 };
        MockModule mockModule = new MockModule();
        unitScriptableObject.SetupMock("TestUnit", 1000, 10000, new List<ModuleSystem.System> {
            new ModuleSystem.System(
                new PrefabModuleSystem.PrefabSystem("TestSystem", ModuleSystem.SystemType.Thruster, 10, 1),
                component)
        }, new List<IModule> { mockModule });

        // What is actually being tested
        var unit = new Mock<Unit>(new BattleObject.BattleObjectData("TestUnit", faction), battleManager.Object,
            unitScriptableObject);
        ModuleSystem moduleSystem = unit.Object.moduleSystem;
        Assert.AreEqual(1, moduleSystem.systems.Count);
        Assert.AreEqual("TestSystem", moduleSystem.systems.First().name);
        Assert.AreEqual(ModuleSystem.SystemType.Thruster, moduleSystem.systems.First().type);
        Assert.AreEqual(component, moduleSystem.systems.First().component);
        Assert.AreEqual(component, moduleSystem.modules.First().componentScriptableObject);
        Assert.AreEqual(mockModule, moduleSystem.modules.First().module);
        Assert.AreEqual(Vector2.zero, moduleSystem.modules.First().GetPosition());
    }

    [Explicit] [Category("Unit Tests")]
    [Test]
    public void UpgradeModuleComponent() {
        // Setup
        MockUnitScriptableObject unitScriptableObject = ScriptableObject.CreateInstance<MockUnitScriptableObject>();
        MockFaction faction = new MockFaction();
        var battleManager = new Mock<BattleManager>();
        battleManager.Setup(e => e.GetRandomSeed()).Returns(1);
        ThrusterScriptableObject component = ScriptableObject.CreateInstance<ThrusterScriptableObject>();
        component.name = "TestThruster";
        component.cost = 10;
        component.thrustSpeed = 1000;
        component.color = Color.blue;
        component.resourceTypes = new List<CargoBay.CargoType> { CargoBay.CargoType.Gas };
        component.resourceCosts = new List<long> { 55 };
        component.name = "TestThruster";
        ThrusterScriptableObject upgradeComponent = ScriptableObject.CreateInstance<ThrusterScriptableObject>();
        upgradeComponent.cost = 20;
        upgradeComponent.resourceTypes = new List<CargoBay.CargoType>();
        upgradeComponent.resourceCosts = new List<long>();
        upgradeComponent.name = "UpgradedTestThruster";
        component.upgrade = upgradeComponent;
        MockModule mockModule = new MockModule();
        unitScriptableObject.SetupMock("TestUnit", 1000, 10000, new List<ModuleSystem.System> {
            new ModuleSystem.System(
                new PrefabModuleSystem.PrefabSystem("TestSystem", ModuleSystem.SystemType.Thruster, 10, 1),
                component)
        }, new List<IModule> { mockModule });

        var unit = new Mock<Unit>(new BattleObject.BattleObjectData("TestUnit", faction), battleManager.Object,
            unitScriptableObject);
        ModuleSystem moduleSystem = unit.Object.moduleSystem;

        Assert.AreEqual(component, moduleSystem.modules.First().componentScriptableObject);
        moduleSystem.UpgradeSystem(0, unit.Object);
        Assert.AreEqual(upgradeComponent, moduleSystem.modules.First().componentScriptableObject);
        Assert.False(moduleSystem.modules.Any(m => m.componentScriptableObject == component));
        Assert.False(moduleSystem.moduleToSystem.Keys.Any(m => m.componentScriptableObject == component));
        Assert.True(moduleSystem.modules.Any(m => m.componentScriptableObject == upgradeComponent));
        Assert.True(moduleSystem.moduleToSystem.Keys.Any(m => m.componentScriptableObject == upgradeComponent));
        Assert.True(
            moduleSystem.moduleToSystem[
                moduleSystem.modules.First(m => m.componentScriptableObject == upgradeComponent)] ==
            moduleSystem.systems.First());
    }

    private class MockUnitScriptableObject : UnitScriptableObject {
        public void SetupMock(string name, int maxHealth, long cost, List<ModuleSystem.System> systems,
            List<IModule> modules) {
            resourceCosts = new List<long>();
            resourceTypes = new List<CargoBay.CargoType>();
            this.systems = systems.ToArray();
            this.modules = modules.ToArray();
            this.name = name;
            this.maxHealth = maxHealth;
            this.cost = cost;
            this.systems = systems.ToArray();
            UpdateCosts();
        }
    }

    private class MockModule : IModule {
        public Vector2 GetPosition() {
            return Vector2.zero;
        }

        public float GetRotation() {
            return 0;
        }

        public float GetMinRotation() {
            return 0;
        }

        public float GetMaxRotation() {
            return 0;
        }

        public int GetSystemIndex() {
            return 0;
        }
    }

    private class MockFaction : Faction {
        public MockFaction() {
            credits = 1000;
        }

        public override float GetImprovementModifier(ImprovementAreas improvementArea) {
            return 1f;
        }
    }
}
