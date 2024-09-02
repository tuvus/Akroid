using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFactionOverviewUI : PlayerUIMenu<Faction> {
    [SerializeField] TMP_Text factionName;
    [SerializeField] TMP_Text unitCount;
    [SerializeField] TMP_Text shipCount;
    [SerializeField] TMP_Text stationCount;
    [SerializeField] Toggle autoCommandFleets;

    [SerializeField] Toggle autoResearch;
    [SerializeField] TMP_Text nextDiscoveryCost;
    [SerializeField] TMP_Text totalDiscoveries;
    [SerializeField] Transform improvementList;

    GameObject pastCharacterPortrait;
    GameObject characterPortrait;
    [SerializeField] TMP_Text leaderName;
    [SerializeField] Transform characterPortraitFrame;

    protected override bool ShouldShowLeftPanel() {
        return LocalPlayer.Instance.GetFaction() == displayedObject;
    }

    protected override void RefreshMiddlePanel() {
        factionName.text = displayedObject.name;
        unitCount.text = "Units: " + displayedObject.units.Count.ToString();
        shipCount.text = "Ships: " + displayedObject.ships.Count.ToString();
        stationCount.text = "Station: " + displayedObject.stations.Count.ToString();
        if (displayedObject == LocalPlayer.Instance.faction && displayedObject.GetFactionAI() is SimulationFactionAI) {
            autoCommandFleets.transform.parent.gameObject.SetActive(true);
        } else {
            autoCommandFleets.transform.parent.gameObject.SetActive(false);
        }
        if (autoCommandFleets.gameObject.activeInHierarchy) {
            autoCommandFleets.SetIsOnWithoutNotify(((SimulationFactionAI)displayedObject.GetFactionAI()).autoCommandFleets);
            autoCommandFleets.onValueChanged.RemoveAllListeners();
            autoCommandFleets.onValueChanged.AddListener((autoBuildShips) => SetAutoCommandFleets(autoBuildShips));
        }
    }

    protected override void RefreshLeftPanel() {
        autoResearch.SetIsOnWithoutNotify(displayedObject.GetFactionAI().autoResearch);
        nextDiscoveryCost.text = "Next discovery cost: " + displayedObject.researchCost;
        totalDiscoveries.text = "Total discoveries: " + displayedObject.Discoveries;
        for (int i = 0; i < displayedObject.improvementModifiers.Length; i++) {
            improvementList.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text = displayedObject.improvementDiscoveryCount[i].ToString();
            improvementList.GetChild(i).GetChild(2).GetComponent<TMP_Text>().text = ((int)(displayedObject.improvementModifiers[i] * 100) / 100f).ToString();
        }
    }

    protected override void RefreshRightPanel() {
        if (pastCharacterPortrait == null || pastCharacterPortrait != displayedObject.GetFactionCommManager().GetPortrait()) {
            if (characterPortrait != null) {
                DestroyImmediate(characterPortrait);
            }
            leaderName.text = displayedObject.GetFactionCommManager().GetSenderName();
            pastCharacterPortrait = displayedObject.GetFactionCommManager().GetPortrait();
            characterPortrait = Instantiate(displayedObject.GetFactionCommManager().GetPortrait(), characterPortraitFrame);
        }
    }

    public void SetAutoCommandFleets(bool autoCommandFleets) {
        ((SimulationFactionAI)displayedObject.GetFactionAI()).autoCommandFleets = autoCommandFleets;
    }

    public void SetAutoResearch() {
        displayedObject.GetFactionAI().autoResearch = autoResearch.isOn;
    }

    public void DiscoverResearchArea(int researchArea) {
        displayedObject.DiscoverResearchArea((Faction.ResearchAreas)researchArea);
    }
}