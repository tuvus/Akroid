using TMPro;
using UnityEngine;

public class AsteroidMenu : PlayerUIMenu<AsteroidUI> {
    [SerializeField] private TMP_Text resourceType;
    [SerializeField] private TMP_Text resourceAmount;
    [SerializeField] private TMP_Text asteroidFieldResources;

    protected override void RefreshStatusPanel() {
        resourceType.text = "Resource Type: " + displayedObject.asteroid.asteroidScriptableObject.type;
        resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedObject.asteroid.resources);
        asteroidFieldResources.text = "Resources: " +
            NumFormatter.ConvertNumber(displayedObject.asteroid.asteroidField.totalResources);
    }
}
