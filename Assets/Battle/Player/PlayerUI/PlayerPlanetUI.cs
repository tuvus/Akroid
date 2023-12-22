using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPlanetUI : MonoBehaviour {
    PlayerUI playerUI;
    float updateSpeed;
    private float updateTime;

    [SerializeField] GameObject planetStatusUI;
    [SerializeField] GameObject planetFactionsUI;

    public Planet displayedPlanet { get; private set; }

    [SerializeField] Text planetName;
    [SerializeField] Text planetFactionName;
    [SerializeField] Text planetType;
    [SerializeField] Text planetPopulation;


    public void SetupPlayerPlanetUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void UpdatePlanetUI() {
        updateTime -= Time.deltaTime;
        if (updateTime <= 0) {
            updateTime += updateSpeed;
            UpdatePlanetDisplay();
        }
    }

    public void DisplayPlanet(Planet planet) {
        displayedPlanet = planet;
        planetStatusUI.SetActive(planet != null);
        planetFactionsUI.SetActive(planet != null);
        UpdatePlanetDisplay();
    }

    public void UpdatePlanetDisplay() {
        if (planetStatusUI.activeSelf) {
            planetName.text = displayedPlanet.objectName;
            if (displayedPlanet.faction != null) {
                planetFactionName.text = displayedPlanet.faction.name;
            } else {
                planetFactionName.text = "Unowned";
            }
            planetPopulation.text = displayedPlanet.population.ToString();
        }
    }
}
