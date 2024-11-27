using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPlanetUI : PlayerUIMenu<PlanetUI> {
    [SerializeField] TMP_Text planetName;
    [SerializeField] TMP_Text planetFactionName;
    [SerializeField] TMP_Text planetType;
    [SerializeField] TMP_Text planetPopulation;
    [SerializeField] TMP_Text highQualityPercentLand;
    [SerializeField] TMP_Text mediumQualityPercentLand;
    [SerializeField] TMP_Text lowQualityPercentLand;
    [SerializeField] TMP_Text planetAreas;

    [SerializeField] Transform planetFactionsList;
    [SerializeField] GameObject planetFactionButton;

    protected override bool ShouldShowMiddlePanel() {
        return displayedObject != null;
    }

    protected override bool ShouldShowLeftPanel() {
        return displayedObject != null;
    }

    protected override void RefreshMiddlePanel() {
        planetName.text = displayedObject.planet.objectName;
        if (displayedObject.planet.faction != null) {
            planetFactionName.text = displayedObject.planet.faction.name;
        } else {
            planetFactionName.text = "Faction" + "Unowned";
        }

        planetPopulation.text = "Population: " + NumFormatter.ConvertNumber(displayedObject.planet.GetPopulation());
        highQualityPercentLand.text = "High Quality Land: " +
                                      (displayedObject.planet.areas.highQualityArea * 100 / displayedObject.planet.totalArea).ToString() + "%";
        mediumQualityPercentLand.text = "Medium Quality Land: " +
                                        (displayedObject.planet.areas.mediumQualityArea * 100 / displayedObject.planet.totalArea).ToString() + "%";
        lowQualityPercentLand.text =
            "Low Quality Land: " + (displayedObject.planet.areas.lowQualityArea * 100 / displayedObject.planet.totalArea).ToString() + "%";
        planetAreas.text = "Areas: " + NumFormatter.ConvertNumber(displayedObject.planet.totalArea);
    }

    protected override void RefreshLeftPanel() {
        List<PlanetFaction> planetFactions = displayedObject.planet.planetFactions.Select(entry => entry.Value).ToList();
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
            factionButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() => playerUI.ShowFactionUI(unitSpriteManager.factionUIs[planetFaction.faction])));

            if (planetFaction.faction != null) {
                factionButtonTransorm.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = planetFaction.faction.name.ToString();
                factionButtonTransorm.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text =
                    planetFaction.faction.abbreviatedName.ToString();
            } else {
                factionButtonTransorm.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = "Unclaimed Territory";
                factionButtonTransorm.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = "";
            }

            factionButtonTransorm.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text =
                "Population: " + NumFormatter.ConvertNumber(planetFaction.population);
            factionButtonTransorm.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text =
                "Force: " + NumFormatter.ConvertNumber(planetFaction.force);
            factionButtonTransorm.GetChild(1).GetChild(2).GetComponent<TMP_Text>().text =
                (planetFaction.territory.GetTotalAreas() * 100 / displayedObject.planet.areas.GetTotalAreas()).ToString() + "%";
            //constructionBayButtonTransform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = planetFaction.special;
            factionButtonTransorm.GetChild(0).GetComponent<Image>().color =
                LocalPlayer.Instance.GetColorOfRelationType(LocalPlayer.Instance.GetRelationToFaction(planetFaction.faction));
            i++;
        }

        for (; i < planetFactionsList.childCount; i++) {
            planetFactionsList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void OpenFactionMenu() {
        Faction faction = displayedObject.planet.faction;
        playerUI.ShowFactionUI(unitSpriteManager.factionUIs[faction]);
    }
}
