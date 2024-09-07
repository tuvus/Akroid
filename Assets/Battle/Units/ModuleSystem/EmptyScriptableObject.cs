using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/EmptyScriptableObject", menuName = "Components/EmptyComponent", order = 10)]
public class EmptyScriptableObject : ComponentScriptableObject {

    public override Type GetComponentType() {
        return typeof(EmptyComponent);
    }
}
