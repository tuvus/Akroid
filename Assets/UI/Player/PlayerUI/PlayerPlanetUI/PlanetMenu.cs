using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlanetMenu : PlayerUIMenu<PlanetUI> {
    [SerializeField] private TMP_Text planetType;
    [SerializeField] private TMP_Text planetFactionName;
    [SerializeField] private TMP_Text planetPopulation;
    [SerializeField] private TMP_Text highQualityPercentLand;
    [SerializeField] private TMP_Text mediumQualityPercentLand;
    [SerializeField] private TMP_Text lowQualityPercentLand;
    [SerializeField] private TMP_Text planetAreas;

    [SerializeField] private Transform planetFactionsList;
    [SerializeField] private GameObject planetFactionButton;

    protected override bool ShouldShowStatusPanel() {
        return displayedObject != null;
    }

    protected override bool ShouldShowLeftPanel() {
        return displayedObject != null && displayedObject.planet.areas.GetTotalAreas() != 0;
    }

    protected override void RefreshStatusPanel() {
        if (displayedObject.planet.faction != null) {
            planetFactionName.text = displayedObject.planet.faction.name;
        } else {
            planetFactionName.text = "Faction" + "Unowned";
        }

        planetPopulation.text = "Population: " + NumFormatter.ConvertNumber(displayedObject.planet.GetPopulation());
        highQualityPercentLand.text = "High Quality Land: " +
            displayedObject.planet.areas.highQualityArea * 100 / displayedObject.planet.totalArea + "%";
        mediumQualityPercentLand.text = "Medium Quality Land: " +
            displayedObject.planet.areas.mediumQualityArea * 100 / displayedObject.planet.totalArea + "%";
        lowQualityPercentLand.text =
            "Low Quality Land: " +
            displayedObject.planet.areas.lowQualityArea * 100 / displayedObject.planet.totalArea + "%";
        planetAreas.text = "Districts: " + NumFormatter.ConvertNumber(displayedObject.planet.totalArea);
    }

    protected override void RefreshLeftPanel() {
        List<PlanetFaction> planetFactions =
            displayedObject.planet.planetFactions.Select(entry => entry.Value).ToList();
        planetFactions.Add(displayedObject.planet.GetUnclaimedFaction());
        int i = 0;
        foreach (PlanetFaction planetFaction in planetFactions) {
            if (planetFactionsList.childCount <= i) {
                Instantiate(planetFactionButton, planetFactionsList);
            }

            Transform factionButtonTransorm = planetFactionsList.GetChild(i);
            factionButtonTransorm.gameObject.SetActive(true);
            Button factionButton = factionButtonTransorm.GetChild(0).GetComponent<Button>();
            factionButton.onClick.RemoveAllListeners();

            if (planetFaction.faction != null) {
                factionButton.onClick.AddListener(() =>
                    playerUI.ShowFactionUI(uiBattleManager.factionUIs[planetFaction.faction]));
                factionButtonTransorm.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text =
                    planetFaction.faction.name;
                factionButtonTransorm.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text =
                    planetFaction.faction.abbreviatedName;
            } else {
                factionButtonTransorm.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = "Unclaimed Territory";
                factionButtonTransorm.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = "";
            }

            factionButtonTransorm.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text =
                "Population: " + NumFormatter.ConvertNumber(planetFaction.population.TotalPopulation());
            factionButtonTransorm.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text =
                "Force: " + NumFormatter.ConvertNumber(planetFaction.population.marines);
            factionButtonTransorm.GetChild(1).GetChild(2).GetComponent<TMP_Text>().text =
                planetFaction.territory.GetTotalAreas() * 100 / displayedObject.planet.areas.GetTotalAreas() + "%";
            //constructionBayButtonTransform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = planetFaction.special;
            factionButtonTransorm.GetChild(0).GetComponent<Image>().color =
                LocalPlayer.Instance.GetColorOfRelationType(
                    LocalPlayer.Instance.GetRelationToFaction(planetFaction.faction));
            i++;
        }

        for (; i < planetFactionsList.childCount; i++) {
            planetFactionsList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void OpenFactionMenu() {
        Faction faction = displayedObject.planet.faction;
        playerUI.ShowFactionUI(uiBattleManager.factionUIs[faction]);
    }
}
