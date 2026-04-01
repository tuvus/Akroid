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
    [SerializeField] private TMP_Text terrainType;
    [SerializeField] private TMP_Text districtType;
    [SerializeField] private TMP_Text districtPopulation;
    [SerializeField] private TMP_Text districtArea;
    [SerializeField] private TMP_Text districtOwner;

    [SerializeField] private GameObject districtPrefab;
    [SerializeField] private GameObject districtInteractionPrefab;
    [SerializeField] private GameObject planetImageButton;

    [Serializable]
    struct TerrainImageInput {
        public District.TerrainType terrain;
        public Sprite sprite;
    }

    [SerializeField] private List<TerrainImageInput> terrainImageInput;
    private Dictionary<District.TerrainType, Sprite> terrainImages;

    // The offset of the planet map, the district with the location of the offset will be at the center
    // The other districts will be wrapped around.
    private Vector2Int offset;
    private List<GameObject> districtUIs;
    private List<GameObject> interactionUIs;
    public bool showCoordinates;
    public District selectedDistrict { get; private set; }


    public override void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        base.SetupPlayerUIMenu(playerUI, localPlayer, uiManager);
        districtUIs = new List<GameObject>();
        interactionUIs = new List<GameObject>();
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
        offset = Vector2Int.zero;
        selectedDistrict = null;
    }

    protected override bool ShouldShowStatusPanel() {
        return displayedObject != null;
    }

    protected override bool ShouldShowLeftPanel() {
        return displayedObject != null && displayedObject.planet.planetFactions.Count != 0;
    }

    protected override bool ShouldShowRightPanel() {
        return selectedDistrict != null;
    }

    public override void RefreshMenu() {
        base.RefreshMenu();
        RefreshDistricts();
        planetImageButton.SetActive(selectedDistrict != null);
    }

    protected void RefreshDistricts() {
        var planetMap = displayedObject.planet.planetMap;
        float planetRadius = displayedObject.planet.size *
            displayedImageTransform.GetComponent<RectTransform>().rect.size.y /
            playerUI.playerObjectUI.objectViewCamera.orthographicSize / 2;
        float districtScale = planetRadius / (planetMap.radius - .5f) / math.sqrt(3);

        int interactionArrowIndex = 0;

        RectTransform GetDistrictInteractionArrow(int index) {
            if (interactionUIs.Count <= index) {
                interactionUIs.Add(Instantiate(districtInteractionPrefab, displayedImageTransform));
            }
            return interactionUIs[index].GetComponent<RectTransform>();
        }

        // Visulaize Districts
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
            districtUI.transform.localPosition =
                PlanetMap.GetPositionFromLocation(planetMap.WrapLocation(district.location - offset)) * districtScale;
            districtUI.GetComponent<RectTransform>().sizeDelta =
                new Vector3(districtScale * math.sqrt(3) * 1.01f, districtScale * 2);
            districtUI.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite =
                terrainImages[district.terrainType];

            Color baseColor = Color.white;
            if (district.owner != null) baseColor = district.owner.faction.color;
            if (selectedDistrict == district) {
                districtUI.transform.GetChild(0).GetComponent<Image>().color = baseColor;
            } else {
                districtUI.transform.GetChild(0).GetComponent<Image>().color =
                    new Color(baseColor.r * .7f, baseColor.g * .7f, baseColor.b * .7f);
            }

            if (showCoordinates) {
                districtUI.transform.GetChild(1).gameObject.SetActive(true);
                districtUI.transform.GetChild(1).GetComponent<TMP_Text>().text = district.location.ToString();
            } else {
                districtUI.transform.GetChild(1).gameObject.SetActive(false);
            }
        }

        // Visualize district interactions
        // needs to be done after district positions are set
        for (int i = 0; i < planetMap.districts.Count; i++) {
            var district = planetMap.districts[i];
            if (district.owner != null &&
                (district.GetDistrictOwner().districtAction == DistrictFaction.DistrictAction.Attack
                    || district.GetDistrictOwner().districtAction == DistrictFaction.DistrictAction.Expand)) {
                var targetDistrict = district.GetDistrictOwner().targetDistrict;
                var interactionTransform = GetDistrictInteractionArrow(interactionArrowIndex);
                var locationDelta = PlanetMap.GetPositionFromLocation(
                    planetMap.WrapLocation(targetDistrict.location - district.location)) * districtScale / 2;
                var middlePosition = districtUIs[i].GetComponent<RectTransform>().anchoredPosition + locationDelta;
                interactionTransform.anchoredPosition = middlePosition;
                interactionTransform.localEulerAngles =
                    new Vector3(0, 0, Calculator.GetAngleOutOfPosition(locationDelta) + 90);
                interactionTransform.GetComponent<Image>().color = Color.softYellow;
                if (district.GetDistrictOwner().districtAction == DistrictFaction.DistrictAction.Attack)
                    interactionTransform.GetComponent<Image>().color = Color.softRed;
                interactionTransform.gameObject.SetActive(true);
                interactionArrowIndex++;
            }
        }

        for (int i = planetMap.districts.Count; i < districtUIs.Count; i++) {
            districtUIs[i].SetActive(false);
        }

        for (int j = interactionArrowIndex; j < interactionUIs.Count; j++) {
            GetDistrictInteractionArrow(j).gameObject.SetActive(false);
        }
    }

    public void OnDistrictPress(int district) {
        if (selectedDistrict == displayedObject.planet.planetMap.districts[district]) {
            selectedDistrict = null;
        } else {
            selectedDistrict = displayedObject.planet.planetMap.districts[district];
            offset = selectedDistrict.location;
        }
    }

    protected override void RefreshStatusPanel() {
        if (displayedObject.planet.faction != null) {
            planetFactionName.text = displayedObject.planet.faction.name;
        } else {
            planetFactionName.text = "Faction" + "Unowned";
        }

        planetPopulation.text = "Population: " +
            NumFormatter.ConvertNumber(displayedObject.planet.GetPopulationWithoutMarines());
        planetAreas.text = "Districts: " + NumFormatter.ConvertNumber(displayedObject.planet.totalArea);
    }

    protected override void RefreshLeftPanel() {
        if (selectedDistrict == null)
            RefreshLeftPanelForPlanet();
        else RefreshLeftPanelForDistrict();
    }

    private void RefreshLeftPanelForPlanet() {
        List<PlanetFaction> planetFactions =
            displayedObject.planet.planetFactions.Select(entry => entry.Value).ToList();
        int i = 0;
        foreach (PlanetFaction planetFaction in planetFactions) {
            if (planetFactionsList.childCount <= i) {
                Instantiate(planetFactionButton, planetFactionsList);
            }

            Transform factionButtonTransform = planetFactionsList.GetChild(i);
            factionButtonTransform.gameObject.SetActive(true);
            Button factionButton = factionButtonTransform.GetChild(0).GetComponent<Button>();
            factionButton.onClick.RemoveAllListeners();

            factionButton.onClick.AddListener(() =>
                playerUI.ShowFactionUI(uiBattleManager.factionUIs[planetFaction.faction]));
            factionButtonTransform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text =
                planetFaction.faction.name;
            factionButtonTransform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text =
                planetFaction.faction.abbreviatedName;

            var pop = planetFaction.GetTotalPopulation();
            factionButtonTransform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text =
                "Population: " + NumFormatter.ConvertNumber(pop.TotalPopulation());
            factionButtonTransform.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text =
                "Force: " + NumFormatter.ConvertNumber(pop.marines);
            factionButtonTransform.GetChild(1).GetChild(2).GetComponent<TMP_Text>().text =
                (int)(planetFaction.GetTotalControl() * 100) / 100 + "%";
            //constructionBayButtonTransform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = planetFaction.special;
            factionButtonTransform.GetChild(0).GetComponent<Image>().color =
                LocalPlayer.Instance.GetColorOfRelationType(
                    LocalPlayer.Instance.GetRelationToFaction(planetFaction.faction));
            i++;
        }

        for (; i < planetFactionsList.childCount; i++) {
            planetFactionsList.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void RefreshLeftPanelForDistrict() {
        int i = 0;
        foreach ((PlanetFaction planetFaction, DistrictFaction districtFaction) in selectedDistrict
            .districtFactions) {
            if (planetFactionsList.childCount <= i) {
                Instantiate(planetFactionButton, planetFactionsList);
            }

            Transform factionButtonTransform = planetFactionsList.GetChild(i);
            factionButtonTransform.gameObject.SetActive(true);
            Button factionButton = factionButtonTransform.GetChild(0).GetComponent<Button>();
            factionButton.onClick.RemoveAllListeners();

            if (planetFaction.faction != null) {
                factionButton.onClick.AddListener(() =>
                    playerUI.ShowFactionUI(uiBattleManager.factionUIs[planetFaction.faction]));
                factionButtonTransform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text =
                    planetFaction.faction.name;
                factionButtonTransform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text =
                    planetFaction.faction.abbreviatedName;
            } else {
                factionButtonTransform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = "Unclaimed Territory";
                factionButtonTransform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = "";
            }

            factionButtonTransform.GetChild(1).GetChild(0).GetComponent<TMP_Text>().text =
                "Population: " + NumFormatter.ConvertNumber(districtFaction.pop.TotalPopulation());
            factionButtonTransform.GetChild(1).GetChild(1).GetComponent<TMP_Text>().text =
                "Force: " + NumFormatter.ConvertNumber(districtFaction.pop.marines);
            factionButtonTransform.GetChild(1).GetChild(2).GetComponent<TMP_Text>().text =
                (int)(districtFaction.control * 100) + "%";
            //constructionBayButtonTransform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = planetFaction.special;
            factionButtonTransform.GetChild(0).GetComponent<Image>().color =
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
        districtType.text = "Type: " + selectedDistrict.districtType.ToString();
        districtArea.text = "Area: " + NumFormatter.ConvertNumber(selectedDistrict.area);
        if (selectedDistrict.owner == null)
            districtOwner.text = "Owner: Unclaimed";
        else districtOwner.text = "Owner: " + selectedDistrict.owner.faction.name;
        districtPopulation.text = "Pop: " + NumFormatter.ConvertNumber(selectedDistrict.GetTotalPopulation());
    }

    public void DeselectDistrict() {
        selectedDistrict = null;
    }

    public void OpenFactionMenu() {
        Faction faction = displayedObject.planet.faction;
        playerUI.ShowFactionUI(uiBattleManager.factionUIs[faction]);
    }
}
