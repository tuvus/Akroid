using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMenueUI : MonoBehaviour {
    PlayerUI playerUI;
    [SerializeField] private Toggle menueUIZoomIndicators;
    [SerializeField] private Toggle menueUIUnitCombatIndicators;
    [SerializeField] private Toggle menueUIParticles;
    [SerializeField] private Dropdown menueUIFactionSelect;
    [SerializeField] private Text timeScaleText;
    [SerializeField] private Slider menueUITimeScale;

    public void SetupMenueUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void ShowMenueUI() {
        menueUIFactionSelect.ClearOptions();
        List<string> factionNames = new List<string>(BattleManager.Instance.GetAllFactions().Count);
        factionNames.Add("None");
        for (int i = 0; i < BattleManager.Instance.GetAllFactions().Count; i++) {
            factionNames.Add(BattleManager.Instance.GetAllFactions()[i].name);
        }
        menueUIZoomIndicators.SetIsOnWithoutNotify(playerUI.showUnitZoomIndicators);
        menueUIUnitCombatIndicators.SetIsOnWithoutNotify(playerUI.showUnitCombatIndicators);
        menueUIParticles.SetIsOnWithoutNotify(playerUI.particles);
        menueUIFactionSelect.AddOptions(factionNames);
        if (LocalPlayer.Instance.GetFaction() == null)
            menueUIFactionSelect.SetValueWithoutNotify(0);
        else
            menueUIFactionSelect.SetValueWithoutNotify(LocalPlayer.Instance.GetFaction().factionIndex + 1);
        timeScaleText.text = "Battle Time Scale: " + ((int)(BattleManager.Instance.timeScale * 10) / 10f);
        menueUITimeScale.SetValueWithoutNotify((int)(BattleManager.Instance.timeScale * 10));
    }

    public void UpdateUnitZoomIndicators(bool newSetting) {
        menueUIZoomIndicators.SetIsOnWithoutNotify(newSetting);
        menueUIUnitCombatIndicators.transform.parent.gameObject.SetActive(newSetting);
    }

    public void UpdateUnitCombatIndicators(bool newSetting) {
        menueUIUnitCombatIndicators.SetIsOnWithoutNotify(newSetting);
    }

    public void SetParticles() {
        playerUI.SetParticles(menueUIParticles.isOn);
    }

    public void ChangeFaction() {
        if (menueUIFactionSelect.value == 0) {
            LocalPlayer.Instance.SetupFaction(null);
        } else if (LocalPlayer.Instance.GetFaction() == null || menueUIFactionSelect.value - 1 != LocalPlayer.Instance.GetFaction().factionIndex) {
            LocalPlayer.Instance.SetupFaction(BattleManager.Instance.GetAllFactions()[menueUIFactionSelect.value - 1]);
        }
    }

    public void UpdateBattleTimeScale() {
        BattleManager.Instance.SetSimulationTimeScale(menueUITimeScale.value / 10f);
        timeScaleText.text = "Battle Time Scale: " + ((int)(BattleManager.Instance.timeScale * 10) / 10f);
    }

    public void QuitSimulation() {
        SceneManager.LoadScene("Start");
    }
}