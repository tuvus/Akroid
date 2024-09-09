using System.Collections;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using UnityEngine;

public class ModuleSystemTests {
    [Test]
    public void SetupFromScriptableObject() {
        var battleMananger = new Mock<BattleManager>();
        var unitScriptableObject = ScriptableObject.CreateInstance<MockUnitScriptableObject>();
        var faction = new Mock<Faction>();
        var unit = new Mock<Unit>(new BattleObject.BattleObjectData("TestUnit", new BattleManager.PositionGiver(Vector2.zero), 0, faction.Object), battleMananger.Object, unitScriptableObject);
        var componentScriptableObject = ScriptableObject.CreateInstance<ThrusterScriptableObject>();
        componentScriptableObject.name = "TestThruster";
        componentScriptableObject.cost = 10;
        componentScriptableObject.thrustSpeed = 1000;
        componentScriptableObject.color = Color.blue;
        componentScriptableObject.resourceTypes = new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Gas };
        componentScriptableObject.resourceCosts = new List<long>() { 55 };
        unitScriptableObject.SetupMock("TestUnit", 1000, 10000, new List<ModuleSystem.System>() {
            new ModuleSystem.System(new PrefabModuleSystem.PrefabSystem("TestSystem", PrefabModuleSystem.SystemType.Thruster), componentScriptableObject)
        });

        var moduleSystem = new ModuleSystem(battleMananger.Object, unit.Object, unitScriptableObject);

    }

    class MockUnitScriptableObject : UnitScriptableObject {
        public void SetupMock(string name, int maxHealth, long cost, List<ModuleSystem.System> systems) {
            resourceCosts = new List<long>();
            resourceTypes = new List<CargoBay.CargoTypes>();
            this.name = name;
            this.maxHealth = maxHealth;
            this.cost = cost;
            this.systems = systems.ToArray();
            UpdateCosts();
        }
    }
}
