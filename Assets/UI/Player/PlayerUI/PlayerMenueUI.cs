using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMenueUI : MonoBehaviour {
    [SerializeField] private TMP_Text musicVolumeText;
    [SerializeField] private Slider musicVolumeScale;
    [SerializeField] private TMP_Text soundEffectVolumeText;
    [SerializeField] private Slider soundEffectVolumeScale;
    [SerializeField] private TMP_Text scrollSpeedText;
    [SerializeField] private Slider scrollSpeedScale;
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
    private BattleManager battleManager;
    private List<Faction> factions;
    private LocalPlayer localPlayer;
    private PlayerUI playerUI;

    public void SetupMenuUI(BattleManager battleManager, LocalPlayer localPlayer, PlayerUI playerUI) {
        this.localPlayer = localPlayer;
        this.playerUI = playerUI;
        this.battleManager = battleManager;
    }

    public void ShowMenuUI() {
        menueUIFactionSelect.ClearOptions();
        factions = battleManager.factions.ToList();
        var factionNames = new List<string>(factions.Count);
        factionNames.Add("None");
        foreach (Faction faction in factions) {
            factionNames.Add(faction.name);
        }

        if (!PlayerPrefs.HasKey(PlayerUI.threadingPrefs)) {
            PlayerPrefs.SetInt(PlayerUI.threadingPrefs, 0);
            PlayerPrefs.Save();
        }

        musicVolumeText.text = "Music: " + (int)(playerUI.musicVolume * 100) + "%";
        musicVolumeScale.SetValueWithoutNotify(playerUI.musicVolume);
        soundEffectVolumeText.text = "Sound Effects: " + (int)(playerUI.soundEffectsVolume * 100) + "%";
        soundEffectVolumeScale.SetValueWithoutNotify(playerUI.soundEffectsVolume);
        scrollSpeedText.text = "Scroll Speed: " + ((int)(playerUI.scrollSpeed * 10) / 10f);
        scrollSpeedScale.SetValueWithoutNotify(playerUI.scrollSpeed);
        menueUIMultiThreading.SetIsOnWithoutNotify(PlayerPrefs.GetInt(PlayerUI.threadingPrefs) == 1);
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
        timeScaleText.text = "Battle Time Scale: " + (int)(battleManager.timeScale * 10) / 10f;
        menueUITimeScale.SetValueWithoutNotify((int)(battleManager.timeScale * 10));
    }

    public void UpdateMusicVolume() {
        PlayerPrefs.SetFloat(PlayerUI.musicVolumePrefs, musicVolumeScale.value);
        PlayerPrefs.Save();
        playerUI.musicVolume = musicVolumeScale.value;
        musicVolumeText.text = "Music: " + (int)(playerUI.musicVolume * 100) + "%";
    }

    public void UpdateSoundEffectsVolume() {
        PlayerPrefs.SetFloat(PlayerUI.soundEffectsPrefs, soundEffectVolumeScale.value);
        PlayerPrefs.Save();
        playerUI.soundEffectsVolume = soundEffectVolumeScale.value;
        soundEffectVolumeText.text = "Sound Effects: " + (int)(playerUI.soundEffectsVolume * 100) + "%";
    }

    public void UpdateScrollSpeed() {
        PlayerPrefs.SetFloat(PlayerUI.scrollSpeedPrefs, scrollSpeedScale.value);
        PlayerPrefs.Save();
        playerUI.scrollSpeed = scrollSpeedScale.value;
        scrollSpeedText.text = "Scroll Speed: " + ((int)(playerUI.scrollSpeed * 10) / 10f);
    }

    public void SetMultiThreading() {
        PlayerPrefs.SetInt(PlayerUI.threadingPrefs, menueUIMultiThreading.isOn ? 1 : 0);
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
        } else if (localPlayer.GetFaction() == null ||
            menueUIFactionSelect.value - 1 != factions.IndexOf(localPlayer.GetFaction())) {
            localPlayer.player.SetFaction(factions[menueUIFactionSelect.value - 1]);
        }
    }

    public void UpdateBattleTimeScale() {
        battleManager.SetSimulationTimeScale(menueUITimeScale.value / 10f);
        timeScaleText.text = "Battle Time Scale: " + (int)(battleManager.timeScale * 10) / 10f;
    }

    public void ResetBattleTimeScale() {
        menueUITimeScale.value = 10;
    }
}
