using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using UnityEngine;

public class ModuleSystemTests {
    [Test]
    public void SetupFromScriptableObject() {
        // Setup
        var battleMananger = new Mock<BattleManager>();
        var unitScriptableObject = ScriptableObject.CreateInstance<MockUnitScriptableObject>();
        var unit = new Mock<Unit>();
        var component = ScriptableObject.CreateInstance<ThrusterScriptableObject>();
        component.name = "TestThruster";
        component.cost = 10;
        component.thrustSpeed = 1000;
        component.color = Color.blue;
        component.resourceTypes = new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Gas };
        component.resourceCosts = new List<long>() { 55 };
        var mockModule = new MockModule();
        unitScriptableObject.SetupMock("TestUnit", 1000, 10000, new List<ModuleSystem.System>() {
            new ModuleSystem.System(new PrefabModuleSystem.PrefabSystem("TestSystem", PrefabModuleSystem.SystemType.Thruster, 10, 1),
                component),
        }, new List<IModule>() { mockModule });

        // What is actually being tested
        var moduleSystem = new ModuleSystem(battleMananger.Object, unit.Object, unitScriptableObject);

        Assert.AreEqual(1, moduleSystem.systems.Count);
        Assert.AreEqual("TestSystem", moduleSystem.systems.First().name);
        Assert.AreEqual(PrefabModuleSystem.SystemType.Thruster, moduleSystem.systems.First().type);
        Assert.AreEqual(component, moduleSystem.systems.First().component);
        Assert.AreEqual(component, moduleSystem.modules.First().componentScriptableObject);
        Assert.AreEqual(mockModule, moduleSystem.modules.First().module);
        Assert.AreEqual(Vector2.zero, moduleSystem.modules.First().GetPosition());
    }

    [Test]
    public void UpgradeModuleComponent() {
        // Setup
        var battleMananger = new Mock<BattleManager>();
        var unitScriptableObject = ScriptableObject.CreateInstance<MockUnitScriptableObject>();
        var faction = new MockFaction();
        var unit = new Mock<Unit>();
        unit.Object.SetFaction(faction);
        var component = ScriptableObject.CreateInstance<ThrusterScriptableObject>();
        component.name = "TestThruster";
        component.cost = 10;
        component.thrustSpeed = 1000;
        component.color = Color.blue;
        component.resourceTypes = new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Gas };
        component.resourceCosts = new List<long>() { 55 };
        component.name = "TestThruster";
        var upgradeComponent = ScriptableObject.CreateInstance<ThrusterScriptableObject>();
        upgradeComponent.cost = 20;
        upgradeComponent.resourceTypes = new List<CargoBay.CargoTypes>();
        upgradeComponent.resourceCosts = new List<long>();
        upgradeComponent.name = "UpgradedTestThruster";
        component.upgrade = upgradeComponent;
        var mockModule = new MockModule();
        unitScriptableObject.SetupMock("TestUnit", 1000, 10000, new List<ModuleSystem.System>() {
            new ModuleSystem.System(new PrefabModuleSystem.PrefabSystem("TestSystem", PrefabModuleSystem.SystemType.Thruster, 10, 1),
                component),
        }, new List<IModule>() { mockModule });

        var moduleSystem = new ModuleSystem(battleMananger.Object, unit.Object, unitScriptableObject);

        Assert.AreEqual(component, moduleSystem.modules.First().componentScriptableObject);
        moduleSystem.UpgradeSystem(0, unit.Object);
        Assert.AreEqual(upgradeComponent, moduleSystem.modules.First().componentScriptableObject);
        Assert.False(moduleSystem.modules.Any(m => m.componentScriptableObject == component));
        Assert.False(moduleSystem.moduleToSystem.Keys.Any(m => m.componentScriptableObject == component));
        Assert.True(moduleSystem.modules.Any(m => m.componentScriptableObject == upgradeComponent));
        Assert.True(moduleSystem.moduleToSystem.Keys.Any(m => m.componentScriptableObject == upgradeComponent));
        Assert.True(moduleSystem.moduleToSystem[moduleSystem.modules.First(m => m.componentScriptableObject == upgradeComponent)] ==
                    moduleSystem.systems.First());
    }

    class MockUnitScriptableObject : UnitScriptableObject {
        public void SetupMock(string name, int maxHealth, long cost, List<ModuleSystem.System> systems, List<IModule> modules) {
            resourceCosts = new List<long>();
            resourceTypes = new List<CargoBay.CargoTypes>();
            base.systems = systems.ToArray();
            base.modules = modules.ToArray();
            this.name = name;
            this.maxHealth = maxHealth;
            this.cost = cost;
            this.systems = systems.ToArray();
            UpdateCosts();
        }
    }

    class MockModule : IModule {
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

    class MockFaction : Faction {
        public MockFaction() {
            credits = 1000;
        }
    }
}
