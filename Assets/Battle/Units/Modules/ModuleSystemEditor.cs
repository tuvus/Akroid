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
        Button addSystemButton = new Button(() => moduleSystem.AddSystem()) {
            text = "AddSystem"
        };
        myInspector.Add(addSystemButton);
        SliderInt selectSystem = new SliderInt("TargetSystem");
        selectSystem.showInputField = true;
        selectSystem.lowValue = 0;
        selectSystem.highValue = math.max(0,moduleSystem.systems.Count - 1);
        myInspector.Add(selectSystem);
        Button removeSystem = new Button(() => moduleSystem.RemoveSystem(selectSystem.value)) {
            text = "RemoveSystem"
        };

        Button addModule = new Button(() => moduleSystem.AddModule(selectSystem.value)) {
            text = "AddModule"
        };
        myInspector.Add(removeSystem);
        myInspector.Add(addModule);
        return myInspector;
    }
}
