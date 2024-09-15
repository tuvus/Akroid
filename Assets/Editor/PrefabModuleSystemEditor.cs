using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(PrefabModuleSystem))]
public class PrefabModuleSystemEditor : Editor {
    public override VisualElement CreateInspectorGUI() {
        PrefabModuleSystem moduleSystem = (PrefabModuleSystem)target;
        // Create a new VisualElement to be the root of our inspector UI

        VisualElement myInspector = new VisualElement();
        InspectorElement.FillDefaultInspector(myInspector, serializedObject, this);
        SliderInt selectSystem = new SliderInt("TargetSystem");
        selectSystem.showInputField = true;
        selectSystem.lowValue = 0;
        selectSystem.highValue = math.max(0, moduleSystem.systems.Count - 1);
        myInspector.Add(selectSystem);
        DropdownField dropdownField = new DropdownField(new List<string>(Enum.GetNames(PrefabModuleSystem.SystemType.Turret.GetType())), 0);
        Button addSystemButton = new Button(() => {
            moduleSystem.AddSystem(dropdownField.value, (PrefabModuleSystem.SystemType)dropdownField.index);
            selectSystem.highValue = math.max(0, moduleSystem.systems.Count - 1);
        }) {
            text = "AddSystem"
        };
        Button addModule = new Button(() => { moduleSystem.AddModule(selectSystem.value); }) {
            text = "AddModuleToSystem"
        };
        Button removeSystem = new Button(() => {
            moduleSystem.RemoveSystem(selectSystem.value);
            selectSystem.highValue = math.max(0, moduleSystem.systems.Count - 1);
        }) {
            text = "RemoveSystem"
        };
        Button removeAllSystems = new Button(() => {
            moduleSystem.RemoveAllSystems();
            selectSystem.highValue = math.max(0, moduleSystem.systems.Count - 1);
        }) {
            text = "RemoveAllSystems"
        };
        Button refresh = new Button(() => { moduleSystem.RefreshComponents(); }) {
            text = "Refresh"
        };
        myInspector.Add(addSystemButton);
        myInspector.Add(dropdownField);
        myInspector.Add(removeSystem);
        myInspector.Add(addModule);
        myInspector.Add(removeAllSystems);
        myInspector.Add(refresh);
        return myInspector;
    }
}
