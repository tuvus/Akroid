using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFactionOverviewUI : MonoBehaviour {
    PlayerUI playerUI;
    Faction faction;
    [SerializeField] Text unitCount;
    [SerializeField] Text shipCount;
    [SerializeField] Text stationCount;
    [SerializeField] Toggle autoCommandFleets;

    [SerializeField] Toggle autoResearch;
    [SerializeField] Text nextDiscoveryCost;
    [SerializeField] Text totalDiscoveries;
    [SerializeField] Transform improvementList;
    public void SetupFactionOverviewUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void UpdateFactionOverviewUI(Faction faction) {
        this.faction = faction;
        unitCount.text = "Units: " + faction.units.Count.ToString();
        shipCount.text = "Ships: " + faction.ships.Count.ToString();
        stationCount.text = "Station: " + faction.stations.Count.ToString();
        autoCommandFleets.transform.parent.gameObject.SetActive(faction.GetFactionAI() is SimulationFactionAI);
        if (autoCommandFleets.gameObject.activeInHierarchy) {
            autoCommandFleets.SetIsOnWithoutNotify(((SimulationFactionAI)faction.GetFactionAI()).autoCommandFleets);
            autoCommandFleets.onValueChanged.RemoveAllListeners();
            autoCommandFleets.onValueChanged.AddListener((autoBuildShips) => SetAutoCommandFleets(autoBuildShips));
        }
        UpdateResearchUI();
    }

    void UpdateResearchUI() {
        autoResearch.SetIsOnWithoutNotify(faction.GetFactionAI().autoResearch);
        nextDiscoveryCost.text = "Next discovery cost: " + faction.researchCost;
        totalDiscoveries.text = "Total discoveries: " + faction.Discoveries;
        for (int i = 0; i < faction.improvementModifiers.Length; i++) {
            improvementList.GetChild(i).GetChild(1).GetComponent<Text>().text = faction.improvementDiscoveryCount[i].ToString();
            improvementList.GetChild(i).GetChild(2).GetComponent<Text>().text = ((int)(faction.improvementModifiers[i] * 100) / 100f).ToString();
        }
    }

    public void SetAutoCommandFleets(bool autoCommandFleets) {
        ((SimulationFactionAI)faction.GetFactionAI()).autoCommandFleets = autoCommandFleets;
    }

    public void SetAutoResearch() {
        faction.GetFactionAI().autoResearch = autoResearch.isOn;
    }

    public void DiscoverResearchArea(int researchArea) {
        faction.DiscoverResearchArea((Faction.ResearchAreas)researchArea);
    }
}
