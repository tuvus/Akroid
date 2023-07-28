using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Module))]
[CanEditMultipleObjects]
public class ModuleEditor : Editor {
    public override VisualElement CreateInspectorGUI() {
        Module module = (Module)target;
        // Create a new VisualElement to be the root of our inspector UI

        VisualElement myInspector = new VisualElement();
        InspectorElement.FillDefaultInspector(myInspector, serializedObject, this);
        Button removeModule = new Button(() => module.moduleSystem.RemoveModule(module)) {
            text = "RemoveModule"
        };

        myInspector.Add(removeModule);
        return myInspector;
    }
}
