using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMenueUI : MonoBehaviour {
    private BattleManager battleManager;
    private LocalPlayer localPlayer;
    private PlayerUI playerUI;
    [SerializeField] private Toggle menueUIMultiThreading;
    [SerializeField] private Toggle menueUIZoomIndicators;
    [SerializeField] private Toggle menueUIUnitCombatIndicators;
    [SerializeField] private Toggle menueUIEffects;
    [SerializeField] private Toggle menueUIParticles;
    [SerializeField] private Toggle menueUICommandRenderer;
    [SerializeField] private Toggle menueUIFactionColors;
    [SerializeField] private TMP_Dropdown menueUIFactionSelect;
    [SerializeField] private TMP_Text timeScaleText;
    [SerializeField] private Slider menueUITimeScale;
    private List<Faction> factions;

    public void SetupMenueUI(BattleManager battleManager, LocalPlayer localPlayer, PlayerUI playerUI) {
        this.localPlayer = localPlayer;
        this.playerUI = playerUI;
        this.battleManager = battleManager;
    }

    public void ShowMenueUI() {
        menueUIFactionSelect.ClearOptions();
        factions = battleManager.factions.ToList();
        List<string> factionNames = new List<string>(factions.Count);
        factionNames.Add("None");
        foreach (var faction in factions) {
            factionNames.Add(faction.name);
        }

        if (!PlayerPrefs.HasKey("Threading")) {
            PlayerPrefs.SetInt("Threading", 0);
            PlayerPrefs.Save();
        }

        menueUIMultiThreading.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Threading") == 1);
        menueUIZoomIndicators.SetIsOnWithoutNotify(playerUI.showUnitZoomIndicators);
        menueUIUnitCombatIndicators.transform.parent.gameObject.SetActive(playerUI.showUnitZoomIndicators);
        menueUIUnitCombatIndicators.SetIsOnWithoutNotify(playerUI.showUnitCombatIndicators);
        menueUIEffects.SetIsOnWithoutNotify(playerUI.effects);
        menueUIParticles.transform.parent.gameObject.SetActive(playerUI.effects);
        menueUIParticles.SetIsOnWithoutNotify(playerUI.particles);
        menueUICommandRenderer.SetIsOnWithoutNotify(playerUI.commandRendererShown);
        menueUIFactionColors.SetIsOnWithoutNotify(playerUI.factionColoring);
        menueUIFactionSelect.AddOptions(factionNames);
        if (localPlayer.GetFaction() == null)
            menueUIFactionSelect.SetValueWithoutNotify(0);
        else
            menueUIFactionSelect.SetValueWithoutNotify(factions.IndexOf(localPlayer.GetFaction()) + 1);
        timeScaleText.text = "Battle Time Scale: " + ((int)(battleManager.timeScale * 10) / 10f);
        menueUITimeScale.SetValueWithoutNotify((int)(battleManager.timeScale * 10));
    }

    public void SetMultiThreading() {
        PlayerPrefs.SetInt("Threading", menueUIMultiThreading.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetUnitZoomIndicators() {
        playerUI.ToggleUnitZoomIndicators();
        menueUIUnitCombatIndicators.transform.parent.gameObject.SetActive(playerUI.GetShowUnitZoomIndicators());
    }

    public void SetUnitCombatIndicators() {
        playerUI.ToggleUnitCombatIndicators();
    }

    public void SetEffects() {
        playerUI.SetEffects(menueUIEffects.isOn);
        menueUIParticles.transform.parent.gameObject.SetActive(menueUIEffects.isOn);
    }

    public void SetParticles() {
        playerUI.SetParticles(menueUIParticles.isOn);
    }

    public void SetCommandRenderer() {
        playerUI.SetCommandRenderer(menueUICommandRenderer.isOn);
    }

    public void SetFactionColor() {
        playerUI.SetFactionColor(menueUIFactionColors.isOn);
    }

    public void ChangeFaction() {
        if (menueUIFactionSelect.value == 0) {
            localPlayer.player.SetFaction(null);
            localPlayer.SetupFaction(null);
        } else if (localPlayer.GetFaction() == null || menueUIFactionSelect.value - 1 != factions.IndexOf(localPlayer.GetFaction())) {
            localPlayer.player.SetFaction(factions[menueUIFactionSelect.value - 1]);
        }
    }

    public void UpdateBattleTimeScale() {
        battleManager.SetSimulationTimeScale(menueUITimeScale.value / 10f);
        timeScaleText.text = "Battle Time Scale: " + ((int)(battleManager.timeScale * 10) / 10f);
    }

    public void ResetBattleTimeScale() {
        menueUITimeScale.value = 10;
    }
}
