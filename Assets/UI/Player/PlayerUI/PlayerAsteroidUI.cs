using TMPro;
using UnityEngine;

public class PlayerAsteroidUI : PlayerUIMenu<AsteroidUI> {
    [SerializeField] TMP_Text asteroidName;
    [SerializeField] TMP_Text resourceType;
    [SerializeField] TMP_Text resourceAmount;

    protected override void RefreshMiddlePanel() {
        asteroidName.text = displayedObject.asteroid.objectName;
        resourceType.text = "Resource Type: " + displayedObject.asteroid.asteroidScriptableObject.type.ToString();
        resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedObject.asteroid.resources);
    }
}
