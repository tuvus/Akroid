using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerFactionOverviewUI : MonoBehaviour {
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
    public FactionUI factionUI;
    private GameObject characterPortrait;
    private LocalPlayer localPlayer;

    private GameObject pastCharacterPortrait;
    private PlayerUI playerUI;
    private UIBattleManager uiBattleManager;
    private UIManager uiManager;

    public void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        this.playerUI = playerUI;
        this.localPlayer = localPlayer;
        this.uiManager = uiManager;
        uiBattleManager = uiManager.uiBattleManager;
    }

    public void SetDisplayedFaction(FactionUI faction) {
        factionUI = faction;
    }

    public void RefreshMenu() {
        RefreshMiddlePanel();
        if (localPlayer.GetFactionUI() == factionUI) {
            RefreshLeftPanel();
        }
        RefreshRightPanel();
    }

    private void RefreshMiddlePanel() {
        factionName.text = factionUI.faction.name;
        unitCount.text = "Units: " + factionUI.faction.units.Count;
        shipCount.text = "Ships: " + factionUI.faction.ships.Count;
        stationCount.text = "Station: " + factionUI.faction.stations.Count;
        if (factionUI == localPlayer.GetFactionUI() &&
            factionUI.faction.GetFactionAI() is SimulationFactionAI) {
            autoCommandFleets.transform.parent.gameObject.SetActive(true);
        } else {
            autoCommandFleets.transform.parent.gameObject.SetActive(false);
        }

        if (autoCommandFleets.gameObject.activeInHierarchy) {
            autoCommandFleets.SetIsOnWithoutNotify(((SimulationFactionAI)factionUI.faction.GetFactionAI())
                .autoCommandFleets);
            autoCommandFleets.onValueChanged.RemoveAllListeners();
            autoCommandFleets.onValueChanged.AddListener(autoBuildShips => SetAutoCommandFleets(autoBuildShips));
        }
    }

    private void RefreshLeftPanel() {
        autoResearch.SetIsOnWithoutNotify(factionUI.faction.GetFactionAI().autoResearch);
        nextDiscoveryCost.text = "Next discovery cost: " + factionUI.faction.researchCost;
        totalDiscoveries.text = "Total discoveries: " + factionUI.faction.discoveries;
        for (int i = 0; i < factionUI.faction.improvementModifiers.Length; i++) {
            improvementList.GetChild(i).GetChild(1).GetComponent<TMP_Text>().text =
                factionUI.faction.improvementDiscoveryCount[i].ToString();
            improvementList.GetChild(i).GetChild(2).GetComponent<TMP_Text>().text =
                ((int)(factionUI.faction.improvementModifiers[i] * 100) / 100f).ToString();
        }
    }

    private void RefreshRightPanel() {
        if (pastCharacterPortrait == null ||
            pastCharacterPortrait != factionUI.faction.GetFactionCommManager().GetPortrait()) {
            if (characterPortrait != null) {
                DestroyImmediate(characterPortrait);
            }

            leaderName.text = factionUI.faction.GetFactionCommManager().GetSenderName();
            pastCharacterPortrait = factionUI.faction.GetFactionCommManager().GetPortrait();
            characterPortrait = Instantiate(factionUI.faction.GetFactionCommManager().GetPortrait(),
                characterPortraitFrame);
        }
    }

    public void SetAutoCommandFleets(bool autoCommandFleets) {
        ((SimulationFactionAI)factionUI.faction.GetFactionAI()).autoCommandFleets = autoCommandFleets;
    }

    public void SetAutoResearch() {
        factionUI.faction.GetFactionAI().autoResearch = autoResearch.isOn;
    }

    public void DiscoverResearchArea(int researchArea) {
        factionUI.faction.DiscoverResearchArea((Faction.ResearchAreas)researchArea);
    }
}
