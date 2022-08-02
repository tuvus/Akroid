using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour {
    private LocalPlayerInput localPlayerInput;

    [SerializeField] private PlayerShipStatusUI shipStatusUI;
    [SerializeField] private PlayerShipFuelCellsUI shipFuelCellsUI;
    [SerializeField] private GameObject factionUI;
    [SerializeField] private GameObject optionsBarUI;
    [SerializeField] private GameObject commandUI;
    [SerializeField] private GameObject controlsListUI;
    [SerializeField] private GameObject menuUI;
    [SerializeField] private Toggle menueUIZoomIndicators;
    [SerializeField] private Toggle menueUIUnitCombatIndicators;
    [SerializeField] private Dropdown menueUIFactionSelect;
    [SerializeField] private Text factionName;
    [SerializeField] private Text factionCredits;
    [SerializeField] private Text factionScience;
    [SerializeField] private Text command;
    [SerializeField] private CommandClick commandClick;

    bool showUnitZoomIndicators;
    bool updateUnitZoomIndicators;
    bool showUnitCombatIndicators;

    public void SetUpUI(LocalPlayerInput localPlayerInput) {
        this.localPlayerInput = localPlayerInput;
        controlsListUI.SetActive(false);
        menuUI.SetActive(false);
        commandClick.SetupCommandClick(localPlayerInput.GetCamera());
        showUnitZoomIndicators = true;
        updateUnitZoomIndicators = true;
        showUnitCombatIndicators = true;
    }

    public void UpdatePlayerUI() {
        Profiler.BeginSample("PlayerUI");
        command.text = localPlayerInput.GetActionType().ToString();
        if (GetLocalPlayer().faction != null) {
            factionUI.SetActive(true);
            factionName.text = GetLocalPlayer().faction.name;
            factionCredits.text = "Credits: " + GetLocalPlayer().faction.credits.ToString();
            factionScience.text = "Science: " + GetLocalPlayer().faction.science.ToString() + " (" + GetLocalPlayer().faction.Discoveries + ")";
            int unitCount = 0;
            if (LocalPlayer.Instance.GetLocalPlayerInput() is LocalPlayerSelectionInput) {
                unitCount = ((LocalPlayerSelectionInput)LocalPlayer.Instance.GetLocalPlayerInput()).GetSelectedUnits().GetUnitCount();
            }
            UpdateDisplayedUnitUI(GetLocalPlayer().GetLocalPlayerInput().GetDisplayedUnit(), unitCount);
        } else {
            factionUI.SetActive(false);
        }
        commandClick.UpdateCommandClick();
        if (UpdateUnitZoomIndicators()) {
            for (int i = 0; i < BattleManager.Instance.GetAllUnits().Count; i++) {
                BattleManager.Instance.GetAllUnits()[i].UpdateUnitUI(showUnitZoomIndicators);
            }
        }
        Profiler.EndSample();
    }

    public void UpdateDisplayedUnitUI(Unit unit, int unitCount) {
        if (unit == null || !unit.IsSpawned()) {
            shipStatusUI.DeselectPlayerShipStatusUI();
            //shipFuelCellsUI.DeleteFuelCellUI();
        } else {
            shipStatusUI.RefreshPlayerShipStatusUI(unit, unitCount);
        }
    }

    public void ToggleControlsList() {
        controlsListUI.SetActive(!controlsListUI.activeSelf);
    }

    public void ToggleMenueUI() {
        ShowMenueUI(!menuUI.activeSelf);
        if (menuUI.activeSelf) {
            menueUIFactionSelect.ClearOptions();
            List<string> factionNames = new List<string>(BattleManager.Instance.GetAllFactions().Count);
            factionNames.Add("None");
            for (int i = 0; i < BattleManager.Instance.GetAllFactions().Count; i++) {
                factionNames.Add(BattleManager.Instance.GetAllFactions()[i].name);
            }
            menueUIFactionSelect.AddOptions(factionNames);
            if (LocalPlayer.Instance.GetFaction() == null)
                menueUIFactionSelect.SetValueWithoutNotify(0);
            else
                menueUIFactionSelect.SetValueWithoutNotify(LocalPlayer.Instance.GetFaction().factionIndex + 1);
        }
    }

    public void ChangeFaction() {
        if (menueUIFactionSelect.value == 0) {
            LocalPlayer.Instance.SetupFaction(null);
        } else if (LocalPlayer.Instance.GetFaction() == null || menueUIFactionSelect.value - 1 != LocalPlayer.Instance.GetFaction().factionIndex) {
            LocalPlayer.Instance.SetupFaction(BattleManager.Instance.GetAllFactions()[menueUIFactionSelect.value - 1]);
        }
    }

    public void ShowMenueUI(bool shown) {
        menuUI.SetActive(shown);
    }

    public LocalPlayer GetLocalPlayer() {
        return LocalPlayer.Instance;
    }

    public CommandClick GetCommandClick() {
        return commandClick;
    }

    public bool FreezeZoom() {
        return controlsListUI.activeSelf;
    }

    public void ToggleUnitZoomIndicators() {
        showUnitZoomIndicators = !showUnitZoomIndicators;
        updateUnitZoomIndicators = true;
        menueUIZoomIndicators.SetIsOnWithoutNotify(showUnitZoomIndicators);
        menueUIUnitCombatIndicators.transform.parent.gameObject.SetActive(showUnitZoomIndicators);
    }


    public void ToggleUnitCombatIndicators() {
        showUnitCombatIndicators = !showUnitCombatIndicators;
        updateUnitZoomIndicators = true;
        menueUIUnitCombatIndicators.SetIsOnWithoutNotify(showUnitCombatIndicators);
    }

    public bool GetShowUnitZoomIndicators() {
        return showUnitZoomIndicators;
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
        return controlsListUI.activeSelf || menuUI.activeSelf;
    }

    public void QuitSimulation() {
        SceneManager.LoadScene("Start");

    }
}
