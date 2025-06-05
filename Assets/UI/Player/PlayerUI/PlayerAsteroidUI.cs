using TMPro;
using UnityEngine;

public class PlayerAsteroidUI : PlayerUIMenu<AsteroidUI> {
    [SerializeField] private TMP_Text asteroidName;
    [SerializeField] private TMP_Text resourceType;
    [SerializeField] private TMP_Text resourceAmount;

    protected override void RefreshMiddlePanel() {
        asteroidName.text = displayedObject.asteroid.objectName;
        resourceType.text = "Resource Type: " + displayedObject.asteroid.asteroidScriptableObject.type;
        resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedObject.asteroid.resources);
    }
}
