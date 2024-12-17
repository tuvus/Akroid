using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

public class PlayerUI : MonoBehaviour {
    public static PlayerUI Instance { get; protected set; }
    private LocalPlayer localPlayer;
    private LocalPlayerInput localPlayerInput;
    private UnitSpriteManager unitSpriteManager;

    [SerializeField] private PlayerObjectStatusUI objectStatusUI;
    [SerializeField] private PlayerShipFuelCellsUI shipFuelCellsUI;
    [field: SerializeField] public PlayerCommsManager playerCommsManager { get; private set; }
    [SerializeField] private PlayerMenueUI playerMenueUI;

    [field: SerializeField] public PlayerFactionOverviewUI playerFactionOverviewUI { get; private set; }
    [field: SerializeField] public PlayerEventUI playerEventUI { get; protected set; }
    [SerializeField] private GameObject factionUI;
    [SerializeField] private GameObject optionsBarUI;
    [SerializeField] private GameObject commandUI;
    [SerializeField] private GameObject controlsListUI;
    [SerializeField] private GameObject menuUI;
    [SerializeField] private TMP_Text factionName;
    [SerializeField] private TMP_Text factionCredits;
    [SerializeField] private TMP_Text factionScience;
    [SerializeField] private TMP_Text command;
    [SerializeField] private TMP_Text timeSpeed;
    [SerializeField] private CommandClick commandClick;
    [SerializeField] private GameObject victoryUI;
    [SerializeField] private TMP_Text victoryTitle;
    [SerializeField] private TMP_Text victoryFaction;
    [SerializeField] private TMP_Text victoryElapsedTime;
    [SerializeField] private TMP_Text victoryRealTime;
    [SerializeField] private GameObject stationUI;
    [SerializeField] private GameObject shipUI;
    [SerializeField] private GameObject planetUI;
    [SerializeField] private GameObject factionOverviewUI;
    [SerializeField] private LineRenderer commandRenderer;
    [SerializeField] private GameObject starUI;
    [SerializeField] private GameObject asteroidUI;
    [SerializeField] private GameObject gasCloudUI;

    [SerializeField] private List<IPlayerUIMenu> uIMenusInput;
    public Dictionary<Type, IPlayerUIMenu> uIMenus;

    public bool showUnitZoomIndicators;
    public bool showUnitCombatIndicators;
    public bool updateUnitZoomIndicators;
    public bool effects;
    public bool particles;
    public bool commandRendererShown;
    public bool factionColoring;

    public void SetUpUI(LocalPlayerInput localPlayerInput, LocalPlayer localPlayer, UnitSpriteManager unitSpriteManager) {
        this.localPlayer = localPlayer;
        Instance = this;
        this.localPlayerInput = localPlayerInput;
        this.unitSpriteManager = unitSpriteManager;
        CloseAllMenus();
        commandClick.SetupCommandClick(localPlayerInput.GetCamera());
        showUnitZoomIndicators = true;
        updateUnitZoomIndicators = true;
        showUnitCombatIndicators = true;
        effects = true;
        particles = true;
        commandRendererShown = true;
        factionColoring = false;
        playerCommsManager.SetupPlayerCommsManager(this);
        uIMenusInput.ForEach(m => m.SetupPlayerUIMenu(this, localPlayer, unitSpriteManager, .2f));
        playerMenueUI.SetupMenueUI(this);
        uIMenus = new Dictionary<Type, IPlayerUIMenu>();
        foreach (var menu in uIMenusInput) {
            uIMenus.Add(menu.GetMenuType(), menu);
        }
    }

    public void UpdatePlayerUI() {
        Profiler.BeginSample("PlayerUI");
        command.text = localPlayerInput.GetActionType().ToString();
        if (localPlayer.player.faction != null) {
            factionUI.SetActive(true);
            factionName.text = localPlayer.player.faction.name;
            factionCredits.text = "Credits: " + NumFormatter.ConvertNumber(localPlayer.player.faction.credits);
            factionScience.text = "Science: " + NumFormatter.ConvertNumber(localPlayer.player.faction.science) + " (" +
                                  localPlayer.player.faction.Discoveries + ")";
        } else {
            factionUI.SetActive(false);
        }

        int unitCount = 0;
        if (LocalPlayer.Instance.GetLocalPlayerInput() is LocalPlayerSelectionInput) {
            unitCount = ((LocalPlayerSelectionInput)LocalPlayer.Instance.GetLocalPlayerInput()).GetSelectedUnits().GetUnitCount();
        }

        UpdateDisplayedObjectUI(GetLocalPlayerInput().GetDisplayedFleet(), GetLocalPlayerInput().GetDisplayedBattleObject(), unitCount);
        commandClick.UpdateCommandClick();

        uIMenusInput.ForEach(m => {
            if (m.IsShown()) m.UpdateUI();
        });
        timeSpeed.text = "Time: " + Time.timeScale;
        playerEventUI.UpdateEventUI();
        playerCommsManager.UpdateCommsManager();
        Profiler.EndSample();
    }

    public void UpdateDisplayedObjectUI(FleetUI fleetUI, BattleObjectUI battleObjectUI, int unitCount) {
        commandRenderer.enabled = false;
        if (battleObjectUI == null || !battleObjectUI.battleObject.IsSpawned()) {
            objectStatusUI.DeselectPlayerObjectStatusUI();
        } else if (battleObjectUI.battleObject.IsUnit()) {
            if (fleetUI != null) {
                objectStatusUI.RefreshPlayerObjectStatusUI(fleetUI, (UnitUI)battleObjectUI, unitCount);
            } else {
                objectStatusUI.RefreshPlayerObjectStatusUI((UnitUI)battleObjectUI, unitCount);
            }

            if (commandRendererShown && battleObjectUI.battleObject.IsShip()) {
                Ship ship = (Ship)battleObjectUI.battleObject;
                List<Vector3> positions;
                if (fleetUI != null) {
                    if (fleetUI.fleet.FleetAI.commands.Count == 0) return;
                    positions = fleetUI.fleet.FleetAI.GetMovementPositionPlan();
                } else {
                    if (ship.shipAI.commands.Count == 0) return;
                    positions = ship.shipAI.GetMovementPositionPlan();
                }

                int targetCount = commandRenderer.positionCount;
                if (targetCount > positions.Count) {
                    while (positions.Count < targetCount) {
                        positions.Add(positions.Last());
                    }
                } else if (positions.Count > targetCount) {
                    positions = positions.GetRange(0, targetCount);
                }

                commandRenderer.widthMultiplier = localPlayerInput.GetCamera().orthographicSize / 100;
                commandRenderer.SetPositions(positions.ToArray());
                commandRenderer.enabled = true;
            }
        } else if (battleObjectUI.battleObject.IsPlanet()) {
            objectStatusUI.RefreshPlayerObjectStatusUI(battleObjectUI, unitCount);
        } else if (battleObjectUI.battleObject.IsStar() || battleObjectUI.battleObject.IsAsteroid() || battleObjectUI.battleObject.IsGasCloud()) {
            objectStatusUI.RefreshPlayerObjectStatusUI(battleObjectUI, unitCount);
        }
    }

    #region MenueUIs

    public void ShowControlList(bool shown) {
        CloseAllMenus();
        controlsListUI.SetActive(shown);
    }

    public void ToggleControlsList() {
        ShowControlList(!controlsListUI.activeSelf);
    }

    public void ShowMenueUI(bool shown) {
        CloseAllMenus();
        menuUI.SetActive(shown);
    }

    public void ToggleMenueUI() {
        ShowMenueUI(!menuUI.activeSelf);
        if (menuUI.activeSelf) {
            playerMenueUI.ShowMenueUI();
        }
    }

    public void ShowVictoryUI(bool shown) {
        CloseAllMenus();
        victoryUI.SetActive(shown);
    }

    public void FactionWon(Faction faction, double realTime, double timeElapsed) {
        if (faction == LocalPlayer.Instance.player.faction) {
            victoryTitle.text = "Victory!";
        } else {
            victoryTitle.text = "Defeat!";
        }

        victoryFaction.text = faction.name;
        victoryRealTime.text = "Real time: " + (int)(realTime / 60) + " minutes";
        victoryElapsedTime.text = "Time elapsed: " + (int)(timeElapsed / 60) + " minutes";
        ShowVictoryUI(true);
    }


    public void SetDisplayedObject(ObjectUI iObjectUI) {
        Type currentType = iObjectUI.GetType();
        while (currentType != null) {
            if (uIMenus.ContainsKey(currentType)) {
                CloseAllMenus();
                uIMenus[currentType].SetDisplayedObject(iObjectUI);
                return;
            }

            currentType = currentType.BaseType;
        }
    }

    public void ShowFactionUI(FactionUI faction) {
        SetDisplayedObject(faction);
    }

    public void ShowLocalFaction() {
        ShowFactionUI(unitSpriteManager.factionUIs[localPlayer.GetFaction()]);
    }

    public void CloseAllMenus() {
        menuUI.SetActive(false);
        controlsListUI.SetActive(false);
        victoryUI.SetActive(false);
        if (stationUI.activeSelf) stationUI.SetActive(false);
        if (shipUI.activeSelf) shipUI.SetActive(false);
        if (planetUI.activeSelf) planetUI.SetActive(false);
        if (factionOverviewUI.activeSelf) factionOverviewUI.SetActive(false);
        if (starUI.activeSelf) starUI.SetActive(false);
        if (asteroidUI.activeSelf) asteroidUI.SetActive(false);
        if (gasCloudUI.activeSelf) gasCloudUI.SetActive(false);
    }

    public void ToggleUnitZoomIndicators() {
        showUnitZoomIndicators = !showUnitZoomIndicators;
        updateUnitZoomIndicators = true;
    }

    public void ToggleUnitCombatIndicators() {
        showUnitCombatIndicators = !showUnitCombatIndicators;
        updateUnitZoomIndicators = true;
    }

    public void SetEffects(bool shown) {
        if (shown != effects) {
            effects = shown;
            // BattleManager.Instance.ShowEffects(shown);
        }
    }

    public void SetParticles(bool shown) {
        if (shown != particles) {
            particles = shown;
            // BattleManager.Instance.ShowParticles(shown);
        }
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

    public void SetCommandRenderer(bool shown) {
        commandRendererShown = shown;
        if (!shown) {
            commandRenderer.enabled = false;
        }
    }

    public void SetFactionColor(bool shown) {
        if (factionColoring != shown) {
            factionColoring = shown;
            // BattleManager.Instance.ShowFactionColoring(shown);
        }
    }

    #endregion

    #region HelperMethods

    public bool FreezeZoom() {
        return playerCommsManager.FreezeScrolling() || IsAMenueShown();
    }

    public bool GetShowUnitZoomIndicators() {
        return showUnitZoomIndicators;
    }

    public bool IsControlsListShown() {
        return controlsListUI.activeSelf;
    }

    public bool IsAMenueShown() {
        return controlsListUI.activeSelf || menuUI.activeSelf || victoryUI.activeSelf || stationUI.activeSelf
               || shipUI.activeSelf || planetUI.activeSelf || factionOverviewUI.activeSelf || starUI.activeSelf
               || asteroidUI.activeSelf || gasCloudUI.activeSelf;
    }

    public bool IsAnObjectMenuShown() {
        return stationUI.activeSelf || shipUI.activeSelf || planetUI.activeSelf;
    }

    public CommandClick GetCommandClick() {
        return commandClick;
    }

    public LocalPlayerInput GetLocalPlayerInput() {
        return localPlayerInput;
    }

    public void QuitSimulation() {
        localPlayerInput.ResetTimeScale();
        SceneManager.LoadScene("Start");
    }

    #endregion
}
