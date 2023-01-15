using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Collections.Specialized.BitVector32;

public class PlayerUI : MonoBehaviour {
    public static PlayerUI Instance { get; protected set; }
    private LocalPlayerInput localPlayerInput;

    [SerializeField] private PlayerUnitStatusUI shipStatusUI;
    [SerializeField] private PlayerShipFuelCellsUI shipFuelCellsUI;
    [SerializeField] private PlayerCommsManager playerCommsManager;
    [SerializeField] private PlayerMenueUI playerMenueUI;
    [SerializeField] private PlayerStationUI playerStationUI;
    [SerializeField] private PlayerShipUI playerShipUI;
    [SerializeField] private PlayerResearchUI playerResearchUI;
    [SerializeField] private GameObject factionUI;
    [SerializeField] private GameObject optionsBarUI;
    [SerializeField] private GameObject commandUI;
    [SerializeField] private GameObject controlsListUI;
    [SerializeField] private GameObject menuUI;
    [SerializeField] private Text factionName;
    [SerializeField] private Text factionCredits;
    [SerializeField] private Text factionScience;
    [SerializeField] private Text command;
    [SerializeField] private CommandClick commandClick;
    [SerializeField] private GameObject victoryUI;
    [SerializeField] private Text victoryTitle;
    [SerializeField] private Text victoryElapsedTime;
    [SerializeField] private Text victoryRealTime;
    [SerializeField] private GameObject stationUI;
    [SerializeField] private GameObject shipUI;
    [SerializeField] private GameObject researchUI;


    public bool showUnitZoomIndicators;
    public bool showUnitCombatIndicators;
    public bool updateUnitZoomIndicators;
    public bool effects;
    public bool particles;

    public void SetUpUI(LocalPlayerInput localPlayerInput) {
        Instance = this;
        this.localPlayerInput = localPlayerInput;
        CloseAllMenues();
        commandClick.SetupCommandClick(localPlayerInput.GetCamera());
        showUnitZoomIndicators = true;
        updateUnitZoomIndicators = true;
        showUnitCombatIndicators = true;
        effects = true;
        particles = true;
        playerCommsManager.SetupPlayerCommsManager(this);
        playerMenueUI.SetupMenueUI(this);
        playerStationUI.SetupPlayerStationUI(this);
        playerShipUI.SetupPlayerShipUI(this);
        playerResearchUI.SetupResearchUI(this);
    }

    public void UpdatePlayerUI() {
        Profiler.BeginSample("PlayerUI");
        command.text = localPlayerInput.GetActionType().ToString();
        if (GetLocalPlayer().faction != null) {
            factionUI.SetActive(true);
            factionName.text = GetLocalPlayer().faction.name;
            factionCredits.text = "Credits: " + GetLocalPlayer().faction.credits.ToString();
            factionScience.text = "Science: " + GetLocalPlayer().faction.science.ToString() + " (" + GetLocalPlayer().faction.Discoveries + ")";
        } else {
            factionUI.SetActive(false);
        }
        int unitCount = 0;
        if (LocalPlayer.Instance.GetLocalPlayerInput() is LocalPlayerSelectionInput) {
            unitCount = ((LocalPlayerSelectionInput)LocalPlayer.Instance.GetLocalPlayerInput()).GetSelectedUnits().GetUnitCount();
        }
        UpdateDisplayedUnitUI(GetLocalPlayer().GetLocalPlayerInput().GetDisplayedFleet(), GetLocalPlayer().GetLocalPlayerInput().GetDisplayedUnit(), unitCount);
        commandClick.UpdateCommandClick();
        if (UpdateUnitZoomIndicators()) {
            for (int i = 0; i < BattleManager.Instance.GetAllUnits().Count; i++) {
                BattleManager.Instance.GetAllUnits()[i].UpdateUnitUI(showUnitZoomIndicators);
            }
        }
        if (stationUI.activeSelf) {
            playerStationUI.UpdateStationUI();
        }
        if (shipUI.activeSelf) {
            playerShipUI.UpdateShipUI();
        }
        if (researchUI.activeSelf) {
            playerResearchUI.UpdateResearchUI(GetLocalPlayer().GetFaction());
        }
        Profiler.EndSample();
    }

    public void UpdateDisplayedUnitUI(Fleet fleet, Unit unit, int unitCount) {
        if (unit == null || !unit.IsSpawned()) {
            shipStatusUI.DeselectPlayerUnitStatusUI();
            //shipFuelCellsUI.DeleteFuelCellUI();
        } else {
            if (fleet != null)
                shipStatusUI.RefreshPlayerUnitStatusUI(fleet, unit, unitCount);
            else
                shipStatusUI.RefreshPlayerUnitStatusUI(unit, unitCount);
        }
    }

    public void SetDisplayStation(Station station) {
        if (!stationUI.activeSelf) {
            ShowStationUI(true);
            playerStationUI.DisplayStation(station);
            return;
        }
        if (playerStationUI.displayedStation == station) {
            playerStationUI.UpdateStationUI();
        } else {
            playerStationUI.DisplayStation(station);
        }
    }

    public void SetDisplayShip(Ship ship) {
        if (!stationUI.activeSelf) {
            ShowShipUI(true);
            playerShipUI.DisplayShip(ship);
            return;
        }
        if (playerShipUI.displayedShip == ship) {
            playerShipUI.UpdateShipUI();
        } else {
            playerShipUI.DisplayShip(ship);
        }
    }

    public void ToggleControlsList() {
        ShowControlList(!controlsListUI.activeSelf);
    }

    public void ToggleMenueUI() {
        ShowMenueUI(!menuUI.activeSelf);
        if (menuUI.activeSelf) {
            playerMenueUI.ShowMenueUI();
        }
    }

    public void SetEffects(bool shown) {
        if (shown != effects) {
            effects = shown;
            BattleManager.Instance.ShowEffects(shown);
        }
    }

    public void SetParticles(bool shown) {
        if (shown != particles) {
            particles = shown;
            BattleManager.Instance.ShowParticles(shown);
        }
    }

    public void FactionWon(string factionName, double realTime, double timeElapsed) {
        victoryTitle.text = "Victory \n " + factionName;
        victoryRealTime.text = "Real time: " + (int)(realTime / 60) + " minutes";
        victoryElapsedTime.text = "Time elapsed: " + (int)(timeElapsed / 60) + " minutes";
        ShowVictoryUI(true);
    }

    public void ShowControlList(bool shown) {
        CloseAllMenues();
        controlsListUI.SetActive(shown);
    }

    public void ShowMenueUI(bool shown) {
        CloseAllMenues();
        menuUI.SetActive(shown);
    }

    public void ShowVictoryUI(bool shown) {
        CloseAllMenues();
        victoryUI.SetActive(shown);
    }

    public void ShowStationUI(bool shown) {
        stationUI.SetActive(false);
        CloseAllMenues();
        stationUI.SetActive(shown);
        if (!shown)
            playerStationUI.DisplayStation(null);
    }

    public void ShowShipUI(bool shown) {
        shipUI.SetActive(false);
        CloseAllMenues();
        shipUI.SetActive(shown);
        if (!shown)
            playerShipUI.DisplayShip(null);
    }

    public void ShowResearchUI(bool shown) {
        CloseAllMenues();
        researchUI.SetActive(shown);
    }

    public void CloseAllMenues() {
        menuUI.SetActive(false);
        controlsListUI.SetActive(false);
        victoryUI.SetActive(false);
        if (stationUI.activeSelf) {
            stationUI.SetActive(false);
        }
        if (shipUI.activeSelf) {
            shipUI.SetActive(false);
        }
        if (researchUI.activeSelf) {
            researchUI.SetActive(false);
        }
    }

    public LocalPlayer GetLocalPlayer() {
        return LocalPlayer.Instance;
    }

    public CommandClick GetCommandClick() {
        return commandClick;
    }

    public bool FreezeZoom() {
        return controlsListUI.activeSelf || playerCommsManager.FreezeScrolling() || stationUI.activeSelf || shipUI.activeSelf || researchUI.activeSelf;
    }

    public bool GetShowUnitZoomIndicators() {
        return showUnitZoomIndicators;
    }

    public void ToggleUnitZoomIndicators() {
        showUnitZoomIndicators = !showUnitZoomIndicators;
        updateUnitZoomIndicators = true;
        playerMenueUI.UpdateUnitZoomIndicators(showUnitZoomIndicators);
    }

    public void ToggleUnitCombatIndicators() {
        showUnitCombatIndicators = !showUnitCombatIndicators;
        updateUnitZoomIndicators = true;
        playerMenueUI.UpdateUnitCombatIndicators(showUnitCombatIndicators);
    }

    bool UpdateUnitZoomIndicators() {
        if (updateUnitZoomIndicators && !showUnitZoomIndicators) {
            updateUnitZoomIndicators = false;
            return true;
        }
        return updateUnitZoomIndicators;
    }

    public bool ShowUnitCombatIndicators() {
        return showUnitCombatIndicators;
    }

    public bool IsControlsListShown() {
        return controlsListUI.activeSelf;
    }

    public bool IsAMenueShown() {
        return controlsListUI.activeSelf || menuUI.activeSelf || victoryUI.activeSelf || stationUI.activeSelf || shipUI.activeSelf || researchUI.activeSelf;
    }

    public PlayerCommsManager GetPlayerCommsManager() {
        return playerCommsManager;
    }

    public LocalPlayerInput GetLocalPlayerInput() {
        return localPlayerInput;
    }

    public void QuitSimulation() {
        SceneManager.LoadScene("Start");
    }
}