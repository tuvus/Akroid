using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMenueUI : MonoBehaviour {
    PlayerUI playerUI;
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

    public void SetupMenueUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void ShowMenueUI() {
        menueUIFactionSelect.ClearOptions();
        factions = BattleManager.Instance.factions.ToList();
        List<string> factionNames = new List<string>(factions.Count);
        factionNames.Add("None");
        foreach (var faction in factions) {
            factionNames.Add(faction.name);

        }
        menueUIZoomIndicators.SetIsOnWithoutNotify(playerUI.showUnitZoomIndicators);
        menueUIUnitCombatIndicators.transform.parent.gameObject.SetActive(playerUI.showUnitZoomIndicators);
        menueUIUnitCombatIndicators.SetIsOnWithoutNotify(playerUI.showUnitCombatIndicators);
        menueUIEffects.SetIsOnWithoutNotify(playerUI.effects);
        menueUIParticles.transform.parent.gameObject.SetActive(playerUI.effects);
        menueUIParticles.SetIsOnWithoutNotify(playerUI.particles);
        menueUICommandRenderer.SetIsOnWithoutNotify(playerUI.commandRendererShown);
        menueUIFactionColors.SetIsOnWithoutNotify(playerUI.factionColoring);
        menueUIFactionSelect.AddOptions(factionNames);
        if (LocalPlayer.Instance.GetFaction() == null)
            menueUIFactionSelect.SetValueWithoutNotify(0);
        else
            menueUIFactionSelect.SetValueWithoutNotify(factions.IndexOf(LocalPlayer.Instance.GetFaction()) + 1);
        timeScaleText.text = "Battle Time Scale: " + ((int)(BattleManager.Instance.timeScale * 10) / 10f);
        menueUITimeScale.SetValueWithoutNotify((int)(BattleManager.Instance.timeScale * 10));
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
            LocalPlayer.Instance.SetupFaction(null);
        } else if (LocalPlayer.Instance.GetFaction() == null || menueUIFactionSelect.value - 1 != factions.IndexOf(LocalPlayer.Instance.GetFaction())) {
            LocalPlayer.Instance.SetupFaction(factions[menueUIFactionSelect.value - 1]);
        }
    }

    public void UpdateBattleTimeScale() {
        BattleManager.Instance.SetSimulationTimeScale(menueUITimeScale.value / 10f);
        timeScaleText.text = "Battle Time Scale: " + ((int)(BattleManager.Instance.timeScale * 10) / 10f);
    }

    public void ResetBattleTimeScale() {
        menueUITimeScale.value = 10;
    }
}