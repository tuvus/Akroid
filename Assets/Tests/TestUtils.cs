using System.Collections.Generic;
using Moq;
using UnityEngine;

public class TestUtils {
    public class TestStationScriptableObject : StationScriptableObject {
        public TestStationScriptableObject SetupScriptableObject(
            params ComponentScriptableObject[] componentScriptableObject) {
            List<IModule> setupModules = new();
            List<ModuleSystem.System> setupSystems = new();
            foreach (ComponentScriptableObject scriptableObject in componentScriptableObject) {
                var system = new ModuleSystem.System("TestSystem", ModuleSystem.SystemType.Any);
                system.moduleCount = 1;
                system.component = scriptableObject;
                setupSystems.Add(system);
                var module = new Mock<IModule>();
                module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
                module.Setup(e => e.GetRotation()).Returns(0);
                module.Setup(e => e.GetSystemIndex()).Returns(setupSystems.Count - 1);
                setupModules.Add(module.Object);
            }
            modules = setupModules.ToArray();
            systems = setupSystems.ToArray();
            return this;
        }
    }

    public class TestShipScriptableObject : ShipScriptableObject {
        public TestShipScriptableObject SetupScriptableObject(
            params ComponentScriptableObject[] componentScriptableObject) {
            List<IModule> setupModules = new();
            List<ModuleSystem.System> setupSystems = new();
            foreach (ComponentScriptableObject scriptableObject in componentScriptableObject) {
                var system = new ModuleSystem.System("TestSystem", ModuleSystem.SystemType.Any);
                system.moduleCount = 1;
                system.component = scriptableObject;
                setupSystems.Add(system);
                var module = new Mock<IModule>();
                module.Setup(e => e.GetPosition()).Returns(Vector2.zero);
                module.Setup(e => e.GetRotation()).Returns(0);
                module.Setup(e => e.GetSystemIndex()).Returns(setupSystems.Count - 1);
                setupModules.Add(module.Object);
            }
            modules = setupModules.ToArray();
            systems = setupSystems.ToArray();
            return this;
        }
    }
}
