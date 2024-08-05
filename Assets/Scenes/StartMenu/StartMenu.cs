using System.Collections;
using UnityEngine;

public class StartMenu : MonoBehaviour {
    public static StartMenu Instance {get; private set;}

    private enum StartMenuState {
        None,
        StartMenue,
        SimulationMenue,
        CampaingMenue,
        OptionMenue,
    }

    [SerializeField]
    private StartMenuState state;

    public void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
        HideAllMenues();
        SetStartMenu();
    }

    public void HideAllMenues() {
        state = StartMenuState.None;
        ShowStartMenue(false);
        ShowSimulationMenue(false);
        ShowCampaignMenue(false);
    }

    #region StartMenue
    public void SetStartMenu() {
        HideAllMenues();
        ShowStartMenue(true);
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
    }

    public void ShowSimulationMenue(bool trueOrFalse) {
        transform.GetChild(1).gameObject.SetActive(trueOrFalse);
    }
    #endregion

    #region CampaingMenu

    public void SetCampaignMenu() {
        HideAllMenues();
        ShowCampaignMenue(true);
        state = StartMenuState.CampaingMenue;
    }

    void ShowCampaignMenue(bool trueOrFalse) {
        transform.GetChild(3).gameObject.SetActive(trueOrFalse);
    }
    #endregion

    public void ExitGame() {
        Application.Quit();
    }
}

