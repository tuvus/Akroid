using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class PlanetMenu : PlayerUIMenu<PlanetUI> {
    [SerializeField] private Transform displayedImageTransform;
    [SerializeField] private TMP_Text planetType;
    [SerializeField] private TMP_Text planetFactionName;
    [SerializeField] private TMP_Text planetPopulation;
    [SerializeField] private TMP_Text highQualityPercentLand;
    [SerializeField] private TMP_Text mediumQualityPercentLand;
    [SerializeField] private TMP_Text lowQualityPercentLand;
    [SerializeField] private TMP_Text planetAreas;

    [SerializeField] private Transform planetFactionsList;
    [SerializeField] private GameObject planetFactionButton;

    [SerializeField] private TMP_Text districtName;
    [SerializeField] private TMP_Text districtType;
    [SerializeField] private TMP_Text terrainType;
    [SerializeField] private TMP_Text area;

    [SerializeField] private GameObject districtPrefab;

    [Serializable]
    struct TerrainImageInput {
        public District.TerrainType terrain;
        public Sprite sprite;
    }

    [SerializeField] private List<TerrainImageInput> terrainImageInput;
    private Dictionary<District.TerrainType, Sprite> terrainImages;

    private List<GameObject> districtUIs;
    public District selectedDistrict { get; private set; }


    public override void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        base.SetupPlayerUIMenu(playerUI, localPlayer, uiManager);
        districtUIs = new List<GameObject>();
        terrainImages = new Dictionary<District.TerrainType, Sprite>();
        foreach (TerrainImageInput imageInput in terrainImageInput) {
            terrainImages.Add(imageInput.terrain, imageInput.sprite);
        }
    }

    public override void SetDisplayedObject(ObjectUI objectUI) {
        base.SetDisplayedObject(objectUI);
        if (objectUI == null) {
            foreach (GameObject districtUI in districtUIs) {
                districtUI.SetActive(false);
            }
        }
        selectedDistrict = null;
    }

    protected override bool ShouldShowStatusPanel() {
        return displayedObject != null;
    }

    protected override bool ShouldShowLeftPanel() {
        return displayedObject != null && displayedObject.planet.areas.GetTotalAreas() != 0;
    }

    protected override bool ShouldShowRightPanel() {
        return selectedDistrict != null;
    }

    public override void RefreshMenu() {
        base.RefreshMenu();
        RefreshDistricts();
    }

    protected void RefreshDistricts() {
        var planetMap = displayedObject.planet.planetMap;
        float planetRadius = displayedObject.planet.size *
            displayedImageTransform.GetComponent<RectTransform>().rect.size.y /
            playerUI.playerObjectUI.objectViewCamera.orthographicSize / 2;
        float districtScale = planetRadius / (planetMap.radius - .5f) / math.sqrt(3);

        for (int i = 0; i < planetMap.districts.Count; i++) {
            if (districtUIs.Count <= i) {
                var newDistrictUI = Instantiate(districtPrefab, displayedImageTransform);
                districtUIs.Add(newDistrictUI);
                int districtIndex = i;
                newDistrictUI.GetComponent<Button>().onClick.AddListener(() => OnDistrictPress(districtIndex));
            }

            var district = planetMap.districts[i];
            GameObject districtUI = districtUIs[i];
            districtUI.SetActive(true);
            districtUI.transform.localPosition = PlanetMap.GetPositionOfDistrict(district) * districtScale;
            districtUI.GetComponent<RectTransform>().sizeDelta =
                new Vector3(districtScale * math.sqrt(3), districtScale * 2);
            districtUI.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite =
                terrainImages[district.terrainType];

            if (selectedDistrict == district) {
                districtUI.GetComponent<Image>().color = Color.white;
            } else {
                districtUI.GetComponent<Image>().color = Color.grey;
            }
        }
        for (int i = planetMap.districts.Count; i < districtUIs.Count; i++) {
            districtUIs[i].SetActive(false);
        }
    }

    public void OnDistrictPress(int district) {
        if (selectedDistrict == displayedObject.planet.planetMap.districts[district]) {
            selectedDistrict = null;
        } else {
            selectedDistrict = displayedObject.planet.planetMap.districts[district];
        }
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
            // factionButtonTransorm.GetChild(1).GetChild(2).GetComponent<TMP_Text>().text =
                // planetFaction.territory.GetTotalAreas() * 100 / displayedObject.planet.areas.GetTotalAreas() + "%";
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

    protected override void RefreshRightPanel() {
        terrainType.text = "Terrain: " + selectedDistrict.terrainType.ToString();
    }

    public void OpenFactionMenu() {
        Faction faction = displayedObject.planet.faction;
        playerUI.ShowFactionUI(uiBattleManager.factionUIs[faction]);
    }
}
