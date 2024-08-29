using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerAsteroidUI : MonoBehaviour {
    PlayerUI playerUI;

    [SerializeField] GameObject asteroidStatsUI;

    public Asteroid displayedAsteroid { get; private set; }
    [SerializeField] TMP_Text asteroidName;
    [SerializeField] TMP_Text resourceType;
    [SerializeField] TMP_Text resourceAmount;

    public void SetupPlayerAsteroidUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void DisplayAsteroid(Asteroid asteroid) {
        displayedAsteroid = asteroid;
        asteroidStatsUI.SetActive(asteroid != null);
        UpdateAsteroidDisplay();
    }

    public void UpdateAsteroidUI() {
        UpdateAsteroidDisplay();
    }

    public void UpdateAsteroidDisplay() {
        if (asteroidStatsUI.activeSelf) {
            asteroidName.text = displayedAsteroid.objectName;
            resourceType.text = "Resource Type: " + displayedAsteroid.asteroidType.ToString();
            resourceAmount.text = "Resources: " + NumFormatter.ConvertNumber(displayedAsteroid.resources);
        }
    }
}
