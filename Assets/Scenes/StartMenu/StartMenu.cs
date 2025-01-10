using System.Collections;
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
    [field:SerializeField] public AudioSource buttonSound { get; private set; }
    [SerializeField] private SimulationSetup simulationSetup;
    [SerializeField] private CampaignSetup campaignSetup;
    public void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
        HideAllMenues();
        SetStartMenu();
        buttonSound.Stop();
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
        buttonSound.Play();
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
        buttonSound.Play();
    }

    public void ShowSimulationMenue(bool trueOrFalse) {
        transform.GetChild(1).gameObject.SetActive(trueOrFalse);
    }

    #endregion

    #region CampaingMenu

    public void SetCampaignMenu() {
        HideAllMenues();
        ShowCampaignMenue(true);
        buttonSound.Play();
        state = StartMenuState.CampaingMenue;
    }

    void ShowCampaignMenue(bool trueOrFalse) {
        transform.GetChild(3).gameObject.SetActive(trueOrFalse);
    }

    #endregion

    public void SetSimulationSetup() {
        HideAllMenues();
        buttonSound.Play();
        state = StartMenuState.SimulationSetupMenu;
        simulationSetup.ShowSimulationSetup();
    }

    public void ExitGame() {
        Application.Quit();
    }
}
