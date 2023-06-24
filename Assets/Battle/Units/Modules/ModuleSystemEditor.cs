using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(ModuleSystem))]
public class ModuleSystemEditor : Editor {

    public override VisualElement CreateInspectorGUI() {
        ModuleSystem moduleSystem = (ModuleSystem)target;
        // Create a new VisualElement to be the root of our inspector UI

        VisualElement myInspector = new VisualElement();
        InspectorElement.FillDefaultInspector(myInspector, serializedObject, this);
        SliderInt selectSystem = new SliderInt("TargetSystem");
        selectSystem.showInputField = true;
        selectSystem.lowValue = 0;
        selectSystem.highValue = math.max(0, moduleSystem.systems.Count - 1);
        myInspector.Add(selectSystem);
        Button addSystemButton = new Button(() => { moduleSystem.AddSystem(); selectSystem.highValue = math.max(0, moduleSystem.systems.Count - 1); }) {
            text = "AddSystem"
        };
        DropdownField dropdownField = new DropdownField(new List<string>(Enum.GetNames(ModuleSystem.SystemType.Turret.GetType())), 0);
        
        Button addSystemReplaceButton = new Button(() => { moduleSystem.AddSystemAndReplace((ModuleSystem.SystemType)Enum.Parse(ModuleSystem.SystemType.Turret.GetType(), dropdownField.value)); selectSystem.highValue = math.max(0, moduleSystem.systems.Count - 1); }) {
            text = "AddSystemAndReplace"
        };
        Button removeSystem = new Button(() => { moduleSystem.RemoveSystem(selectSystem.value); selectSystem.highValue = math.max(0, moduleSystem.systems.Count - 1); }) {
            text = "RemoveSystem"
        };
        Button addModule = new Button(() => { moduleSystem.AddModule(selectSystem.value); }) {
            text = "AddModule"
        };
        Button removeAllSystems = new Button(() => { moduleSystem.RemoveAllSystems(); selectSystem.highValue = math.max(0, moduleSystem.systems.Count - 1); }) {
            text = "RemoveAllSystems"
        };
        myInspector.Add(addSystemButton);
        myInspector.Add(dropdownField);
        myInspector.Add(addSystemReplaceButton);
        myInspector.Add(removeSystem);
        myInspector.Add(addModule);
        myInspector.Add(removeAllSystems);
        return myInspector;
    }
}
