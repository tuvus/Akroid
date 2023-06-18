using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResearchUI : MonoBehaviour {
    PlayerUI playerUI;
    Faction faction;
    [SerializeField] Toggle autoResearch;
    [SerializeField] Text nextDiscoveryCost;
    [SerializeField] Text totalDiscoveries;
    [SerializeField] Transform improvementList;
    public void SetupResearchUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void UpdateResearchUI(Faction faction) {
        this.faction = faction;
        autoResearch.SetIsOnWithoutNotify(faction.GetFactionAI().autoResearch);
        nextDiscoveryCost.text = "Next discovery cost: " + faction.researchCost;
        totalDiscoveries.text = "Total discoveries: " + faction.Discoveries;
        for (int i = 0; i < faction.improvementModifiers.Length; i++) {
            improvementList.GetChild(i).GetChild(1).GetComponent<Text>().text = faction.improvementDiscoveryCount[i].ToString();
            improvementList.GetChild(i).GetChild(2).GetComponent<Text>().text = ((int)(faction.improvementModifiers[i] * 100) / 100f).ToString();
        }
    }

    public void SetAutoResearch() {
        faction.GetFactionAI().autoResearch = autoResearch.isOn;
    }

    public void DiscoverResearchArea(int researchArea) {
        faction.DiscoverResearchArea((Faction.ResearchAreas)researchArea);
    }
}
