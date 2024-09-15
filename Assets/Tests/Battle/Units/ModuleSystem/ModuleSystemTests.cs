using System.Collections;
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
        var componentScriptableObject = ScriptableObject.CreateInstance<ThrusterScriptableObject>();
        componentScriptableObject.name = "TestThruster";
        componentScriptableObject.cost = 10;
        componentScriptableObject.thrustSpeed = 1000;
        componentScriptableObject.color = Color.blue;
        componentScriptableObject.resourceTypes = new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Gas };
        componentScriptableObject.resourceCosts = new List<long>() { 55 };
        var mockModule = new MockModule();
        unitScriptableObject.SetupMock("TestUnit", 1000, 10000, new List<ModuleSystem.System>() {
            new ModuleSystem.System(new PrefabModuleSystem.PrefabSystem("TestSystem", PrefabModuleSystem.SystemType.Thruster, 10, 1), componentScriptableObject),
        }, new List<IModule>() { mockModule });

        // What is actually being tested
        var moduleSystem = new ModuleSystem(battleMananger.Object, unit.Object, unitScriptableObject);

        Assert.AreEqual(1, moduleSystem.systems.Count);
        Assert.AreEqual("TestSystem", moduleSystem.systems.First().name);
        Assert.AreEqual(PrefabModuleSystem.SystemType.Thruster, moduleSystem.systems.First().type);
        Assert.AreEqual(componentScriptableObject, moduleSystem.systems.First().component);
        Assert.AreEqual(componentScriptableObject, moduleSystem.modules.First().componentScriptableObject);
        Assert.AreEqual(mockModule, moduleSystem.modules.First().module);
        Assert.AreEqual(Vector2.zero, moduleSystem.modules.First().GetPosition());
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
    }
}
