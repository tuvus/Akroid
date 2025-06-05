using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFactionOverviewUI : PlayerUIMenu<FactionUI> {
    [SerializeField] private TMP_Text factionName;
    [SerializeField] private TMP_Text unitCount;
    [SerializeField] private TMP_Text shipCount;
    [SerializeField] private TMP_Text stationCount;
    [SerializeField] private Toggle autoCommandFleets;

    [SerializeField] private Toggle autoResearch;
    [SerializeField] private TMP_Text nextDiscoveryCost;
    [SerializeField] private TMP_Text totalDiscoveries;
    [SerializeField] private Transform improvementList;
    [SerializeField] private TMP_Text leaderName;
    [SerializeField] private Transform characterPortraitFrame;
    private GameObject characterPortrait;

    private GameObject pastCharacterPortrait;

    protected override bool ShouldShowLeftPanel() {
        return localPlayer.GetFactionUI() == displayedObject;
    }

    protected override void RefreshMiddlePanel() {
        factionName.text = displayedObject.faction.name;
        unitCount.text = "Units: " + displayedObject.faction.units.Count;
        shipCount.text = "Ships: " + displayedObject.faction.ships.Count;
        stationCount.text = "Station: " + displayedObject.faction.stations.Count;
        if (displayedObject == localPlayer.GetFactionUI() &&
            displayedObject.faction.GetFactionAI() is SimulationFactionAI) {
            autoCommandFleets.transform.parent.gameObject.SetActive(true);
        } else {
            autoCommandFleets.transform.parent.gameObject.SetActive(false);
        }

        if (autoCommandFleets.gameObject.activeInHierarchy) {
            autoCommandFleets.SetIsOnWithoutNotify(((SimulationFactionAI)displayedObject.faction.GetFactionAI())
                .autoCommandFleets);
            autoCommandFleets.onValueChanged.RemoveAllListeners();
            autoCommandFleets.onValueChanged.AddListener(autoBuildShips => SetAutoCommandFleets(autoBuildShips));
        }
    }

    protected override void RefreshLeftPanel() {
        autoResearch.SetIsOnWithoutNotify(displayedObject.faction.GetFactionAI().autoResearch);
        nextDiscoveryCost.text = "Next discovery cost: " + displayedObject.faction.researchCost;
        totalDiscoveries.text = "Total discoveries: " + displayedObject.faction.discoveries;
        for (int i = 0; i < displayedObject.faction.improvementModifiers.Length; i++) {
            improvementList.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text =
                displayedObject.faction.improvementDiscoveryCount[i].ToString();
            improvementList.GetChild(i).GetChild(2).GetComponent<TMP_Text>().text =
                ((int)(displayedObject.faction.improvementModifiers[i] * 100) / 100f).ToString();
        }
    }

    protected override void RefreshRightPanel() {
        if (pastCharacterPortrait == null ||
            pastCharacterPortrait != displayedObject.faction.GetFactionCommManager().GetPortrait()) {
            if (characterPortrait != null) {
                DestroyImmediate(characterPortrait);
            }

            leaderName.text = displayedObject.faction.GetFactionCommManager().GetSenderName();
            pastCharacterPortrait = displayedObject.faction.GetFactionCommManager().GetPortrait();
            characterPortrait = Instantiate(displayedObject.faction.GetFactionCommManager().GetPortrait(),
                characterPortraitFrame);
        }
    }

    public void SetAutoCommandFleets(bool autoCommandFleets) {
        ((SimulationFactionAI)displayedObject.faction.GetFactionAI()).autoCommandFleets = autoCommandFleets;
    }

    public void SetAutoResearch() {
        displayedObject.faction.GetFactionAI().autoResearch = autoResearch.isOn;
    }

    public void DiscoverResearchArea(int researchArea) {
        displayedObject.faction.DiscoverResearchArea((Faction.ResearchAreas)researchArea);
    }
}
