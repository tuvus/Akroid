using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFactionOverviewUI : MonoBehaviour {
    PlayerUI playerUI;
    public Faction displayedFaction { get; private set; }
    [SerializeField] TMP_Text factionName;
    [SerializeField] TMP_Text unitCount;
    [SerializeField] TMP_Text shipCount;
    [SerializeField] TMP_Text stationCount;
    [SerializeField] Toggle autoCommandFleets;

    [SerializeField] GameObject factionResearchUI;
    [SerializeField] Toggle autoResearch;
    [SerializeField] TMP_Text nextDiscoveryCost;
    [SerializeField] TMP_Text totalDiscoveries;
    [SerializeField] Transform improvementList;

    GameObject pastCharacterPortrait;
    GameObject characterPortrait;
    [SerializeField] TMP_Text leaderName;
    [SerializeField] Transform characterPortraitFrame;

    public void SetupFactionOverviewUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void DisplayFaction(Faction displayedFaction) {
        this.displayedFaction = displayedFaction;
        if (displayedFaction == LocalPlayer.Instance.faction) {
            factionResearchUI.SetActive(true);
        } else {
            factionResearchUI.SetActive(false);
        }
        UpdateFactionOverviewUI();
    }

    public void UpdateFactionOverviewUI() {
        UpdateFactionStatus();
        if (factionResearchUI.activeSelf) {
            UpdateResearchUI();
        }
        UpdateFactionLeader();
    }

    void UpdateFactionStatus() {
        factionName.text = displayedFaction.name;
        unitCount.text = "Units: " + displayedFaction.units.Count.ToString();
        shipCount.text = "Ships: " + displayedFaction.ships.Count.ToString();
        stationCount.text = "Station: " + displayedFaction.stations.Count.ToString();
        if (displayedFaction == LocalPlayer.Instance.faction && displayedFaction.GetFactionAI() is SimulationFactionAI) {
            autoCommandFleets.transform.parent.gameObject.SetActive(true);
        } else {
            autoCommandFleets.transform.parent.gameObject.SetActive(false);
        }
        if (autoCommandFleets.gameObject.activeInHierarchy) {
            autoCommandFleets.SetIsOnWithoutNotify(((SimulationFactionAI)displayedFaction.GetFactionAI()).autoCommandFleets);
            autoCommandFleets.onValueChanged.RemoveAllListeners();
            autoCommandFleets.onValueChanged.AddListener((autoBuildShips) => SetAutoCommandFleets(autoBuildShips));
        }
    }

    void UpdateResearchUI() {
        autoResearch.SetIsOnWithoutNotify(displayedFaction.GetFactionAI().autoResearch);
        nextDiscoveryCost.text = "Next discovery cost: " + displayedFaction.researchCost;
        totalDiscoveries.text = "Total discoveries: " + displayedFaction.Discoveries;
        for (int i = 0; i < displayedFaction.improvementModifiers.Length; i++) {
            improvementList.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text = displayedFaction.improvementDiscoveryCount[i].ToString();
            improvementList.GetChild(i).GetChild(2).GetComponent<TMP_Text>().text = ((int)(displayedFaction.improvementModifiers[i] * 100) / 100f).ToString();
        }
    }

    void UpdateFactionLeader() {
        if (pastCharacterPortrait == null || pastCharacterPortrait != displayedFaction.GetFactionCommManager().GetPortrait()) {
            if (characterPortrait != null) {
                DestroyImmediate(characterPortrait);
            }
            leaderName.text = displayedFaction.GetFactionCommManager().GetSenderName();
            pastCharacterPortrait = displayedFaction.GetFactionCommManager().GetPortrait();
            characterPortrait = Instantiate(displayedFaction.GetFactionCommManager().GetPortrait(), characterPortraitFrame);
        }
    }

    public void SetAutoCommandFleets(bool autoCommandFleets) {
        ((SimulationFactionAI)displayedFaction.GetFactionAI()).autoCommandFleets = autoCommandFleets;
    }

    public void SetAutoResearch() {
        displayedFaction.GetFactionAI().autoResearch = autoResearch.isOn;
    }

    public void DiscoverResearchArea(int researchArea) {
        displayedFaction.DiscoverResearchArea((Faction.ResearchAreas)researchArea);
    }
}