using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Resources/Components/ResearchEquipmentScriptableObject", menuName = "Components/ResearchEquipment", order = 1)]
public class ResearchEquipmentScriptableObject : ComponentScriptableObject {
    public int maxData;
    public int researchAmount;
    public float researchSpeed;

    public override Type GetComponentType() {
        return typeof(ResearchEquipment);
    }
}
