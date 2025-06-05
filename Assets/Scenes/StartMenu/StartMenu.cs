using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StartMenu : MonoBehaviour {
    public static StartMenu Instance { get; private set; }

    private enum StartMenuState {
        None,
        StartMenue,
        SimulationMenue,
        CampaingMenue,
        SimulationSetupMenu,
        CampaingSetup,
    }

    [SerializeField] private StartMenuState state;
    [SerializeField] TMP_Text versionText;
    [SerializeField] private AudioSource buttonClickSound;
    [SerializeField] private AudioSource buttonHoverSound;
    [SerializeField] private SimulationSetup simulationSetup;
    [SerializeField] private CampaignSetup campaignSetup;

    public List<GameObject> tmpCharacters;
    private float lastButtonSoundTime;

    public void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
        // Very hacky, had to get this done quickly, please fix
        if (Character.characterPrefabs == null) {
            Character.characterPrefabs = new();
            tmpCharacters.ForEach(c => Character.characterPrefabs.Add(c.name, c));
        }
        HideAllMenues();
        SetStartMenu();
        buttonClickSound.Stop();
        versionText.text = "Version: " + Application.version;
    }

    public void HideAllMenues() {
        state = StartMenuState.None;
        ShowStartMenue(false);
        ShowSimulationMenue(false);
        ShowCampaignMenue(false);
        simulationSetup.gameObject.SetActive(false);
        campaignSetup.gameObject.SetActive(false);
        simulationSetup.SetStartMenu(this);
        campaignSetup.SetStartMenu(this);
    }

    #region StartMenue

    public void SetStartMenu() {
        HideAllMenues();
        ShowStartMenue(true);
        buttonClickSound.Play();
        state = StartMenuState.StartMenue;
    }

    void ShowStartMenue(bool trueOrFalse) {
        transform.GetChild(0).gameObject.SetActive(trueOrFalse);
    }

    #endregion

    #region SimulationMenue

    public void SetSimulationMenu() {
        HideAllMenues();
        ShowSimulationMenue(true);
        state = StartMenuState.SimulationMenue;
        buttonClickSound.Play();
    }

    public void ShowSimulationMenue(bool trueOrFalse) {
        transform.GetChild(1).gameObject.SetActive(trueOrFalse);
    }

    #endregion

    #region CampaingMenu

    public void SetCampaignMenu() {
        HideAllMenues();
        ShowCampaignMenue(true);
        buttonClickSound.Play();
        state = StartMenuState.CampaingMenue;
    }

    void ShowCampaignMenue(bool trueOrFalse) {
        transform.GetChild(3).gameObject.SetActive(trueOrFalse);
    }

    #endregion

    public void PlayButtonClickSound() {
        buttonClickSound.Play();
        lastButtonSoundTime = Time.time;
    }

    public void PlayButtonHoverSound() {
        if (lastButtonSoundTime <= Time.time - .2f) {
            buttonHoverSound.Play();
            lastButtonSoundTime = Time.time;
        }
    }

    public void SetSimulationSetup() {
        HideAllMenues();
        buttonClickSound.Play();
        state = StartMenuState.SimulationSetupMenu;
        simulationSetup.ShowSimulationSetup();
    }

    public void ExitGame() {
        buttonClickSound.Play();
        Application.Quit();
    }
}
