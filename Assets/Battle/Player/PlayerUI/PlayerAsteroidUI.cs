using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerAsteroidUI : PlayerUIMenu<Asteroid> {
    [SerializeField] TMP_Text asteroidName;
    [SerializeField] TMP_Text resourceType;
    [SerializeField] TMP_Text resourceAmount;

    protected override void RefreshMiddlePanel() {
        asteroidName.text = displayedObject.objectName;
        resourceType.text = "Resource Type: " + displayedObject.asteroidType.ToString();
        resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedObject.resources);
    }
}
