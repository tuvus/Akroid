using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
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
    [SerializeField] Text highQualityPercentLand;
    [SerializeField] Text mediumQualityPercentLand;
    [SerializeField] Text lowQualityPercentLand;
    [SerializeField] Text planetAreas;

    [SerializeField] Transform planetFactionsList;
    [SerializeField] GameObject planetFactionButton;

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
                planetFactionName.text = "Faction: " + displayedPlanet.faction.name;
            } else {
                planetFactionName.text = "Faction" + "Unowned";
            }
            planetPopulation.text = "Population: " + NumFormatter.ConvertNumber(displayedPlanet.population);
            highQualityPercentLand.text = "High Quality Land: " + (displayedPlanet.areas.highQualityArea * 100 / displayedPlanet.totalArea).ToString() + "%";
            mediumQualityPercentLand.text = "Medium Quality Land: " + (displayedPlanet.areas.mediumQualityArea * 100 / displayedPlanet.totalArea).ToString() + "%";
            lowQualityPercentLand.text = "Low Quality Land: " + (displayedPlanet.areas.lowQualityArea * 100 / displayedPlanet.totalArea).ToString() + "%";
            planetAreas.text = "Areas: " + NumFormatter.ConvertNumber(displayedPlanet.totalArea);
        }
        if (planetFactionsUI.activeSelf) {
            UpdatePlanetFactions();
        }
    }

    public void UpdatePlanetFactions() {
        int i = 0;
        List<Planet.PlanetFaction> planetFactions = displayedPlanet.planetFactions.Select(entry => entry.Value).ToList();
        planetFactions.Add(displayedPlanet.GetUnclaimedFaction());
        foreach (Planet.PlanetFaction planetFaction in planetFactions) {
            if (planetFactionsList.childCount <= i) {
                Instantiate(planetFactionButton, planetFactionsList);
            }
            Transform constructionBayButtonTransform = planetFactionsList.GetChild(i);
            if (planetFaction.faction != null) {
                constructionBayButtonTransform.GetChild(0).GetChild(0).GetComponent<Text>().text = planetFaction.faction.name.ToString();
                constructionBayButtonTransform.GetChild(0).GetChild(1).GetComponent<Text>().text = planetFaction.faction.abbreviatedName.ToString();
            } else {
                constructionBayButtonTransform.GetChild(0).GetChild(0).GetComponent<Text>().text = "Unclaimed Territory";
                constructionBayButtonTransform.GetChild(0).GetChild(1).GetComponent<Text>().text = "";
            }
            constructionBayButtonTransform.GetChild(1).GetChild(0).GetComponent<Text>().text = "Force: " + NumFormatter.ConvertNumber(planetFaction.force);
            constructionBayButtonTransform.GetChild(1).GetChild(1).GetComponent<Text>().text = "Territory: " + NumFormatter.ConvertNumber(planetFaction.territory.GetTotalAreas());
            constructionBayButtonTransform.GetChild(1).GetChild(2).GetComponent<Text>().text = (planetFaction.territory.GetTotalAreas() * 100 / displayedPlanet.areas.GetTotalAreas()).ToString() + "%";
            constructionBayButtonTransform.GetChild(2).GetChild(0).GetComponent<Text>().text = planetFaction.special;
            constructionBayButtonTransform.GetChild(0).GetComponent<Image>().color = LocalPlayer.Instance.GetColorOfRelationType(LocalPlayer.Instance.GetRelationToFaction(planetFaction.faction));
            i++;
        }
        for (; i < planetFactionsList.childCount; i++) {
            planetFactionsList.GetChild(i).gameObject.SetActive(false);
        }
    }
}
