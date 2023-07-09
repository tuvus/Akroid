using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/HangerScriptableObject", menuName = "Components/Hanger", order = 1)]
public class HangerScriptableObject : ComponentScriptableObject {
    public int maxDockSpace;

    public override Type GetComponentType() {
        return typeof(Hanger);
    }
}
