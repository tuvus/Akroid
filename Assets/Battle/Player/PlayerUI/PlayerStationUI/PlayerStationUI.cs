using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class PlayerStationUI : MonoBehaviour {
    PlayerUI playerUI;
    [SerializeField] GameObject stationStatusUI;
    [SerializeField] GameObject stationConstructionUI;
    [SerializeField] GameObject stationHangerUI;
    public Station displayedStation { get; private set; }
    [SerializeField] Text stationName;
    [SerializeField] Text stationFaction;
    [SerializeField] Text stationType;
    [SerializeField] Text weaponsCount;
    [SerializeField] Text stationTotalDPS;
    [SerializeField] Text maxWeaponRange;
    [SerializeField] Text cargoBaysStatus;
    [SerializeField] Text cargoBayCapacity;
    [SerializeField] Text cargoHeader;
    [SerializeField] Transform cargoBayList;
    [SerializeField] GameObject cargoBayButtonPrefab;
    [SerializeField] Text hangerStatus;
    [SerializeField] Transform hangerList;
    [SerializeField] GameObject shipButtonPrefab;
    [SerializeField] List<Ship> shipsInHanger;
    float updateSpeed;
    float updateTime;
    [SerializeField] Toggle autoBuildShips;
    [SerializeField] Button shipYardSelection;
    [SerializeField] Button upgradeSelection;
    /// <summary> True for shipyard, false for upgrade </summary>
    bool shipYardOrUpgrade;
    [SerializeField] GameObject shipBlueprintButtonPrefab;
    [SerializeField] Transform blueprintList;
    Unit upgradeDisplayUnit;
    [SerializeField] Text constructionBayStatus;
    [SerializeField] Transform constructionBayList;
    List<Ship.ShipBlueprint> shipBlueprints;

    public void SetupPlayerStationUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
        shipYardOrUpgrade = true;
        ShipYardButtonSelected();
    }

    public void UpdateStationUI() {
        updateTime -= Time.deltaTime;
        if (updateTime <= 0) {
            updateTime += updateSpeed;
            UpdateStationDisplay();
        }
    }

    public void DisplayStation(Station displayedStation) {
        this.displayedStation = displayedStation;
        if (displayedStation == null)
            return;
        bool isEnemy = LocalPlayer.Instance.GetRelationToUnit(displayedStation) == LocalPlayer.RelationType.Enemy;
        stationStatusUI.SetActive(displayedStation.stationType != Station.StationType.None);
        stationConstructionUI.SetActive(!isEnemy && (displayedStation.stationType == Station.StationType.Shipyard || displayedStation.stationType == Station.StationType.FleetCommand));
        stationHangerUI.SetActive(!isEnemy && displayedStation.GetHanger() != null);
        UpdateStationDisplay();
    }

    public void UpdateStationDisplay() {
        Profiler.BeginSample("StationDisplayUpdate");
        if (stationStatusUI.activeSelf) {
            stationName.text = displayedStation.GetUnitName();
            stationFaction.text = "Faction: " + displayedStation.faction.name;
            stationType.text = "Station Type: " + displayedStation.stationType;
            weaponsCount.text = "Weapons: " + displayedStation.GetWeaponCount();
            if (displayedStation.GetWeaponCount() > 0) {
                stationTotalDPS.text = "Damage Per Second: " + NumFormatter.ConvertNumber(displayedStation.GetUnitDamagePerSecond());
                maxWeaponRange.text = "Weapon Range: " + NumFormatter.ConvertNumber(displayedStation.GetMaxWeaponRange());
                stationTotalDPS.gameObject.SetActive(true);
                maxWeaponRange.gameObject.SetActive(true);
            } else {
                stationTotalDPS.gameObject.SetActive(false);
                maxWeaponRange.gameObject.SetActive(false);
            }
            UpdateCargoBayUI(displayedStation.GetCargoBay(), LocalPlayer.Instance.GetRelationToUnit(displayedStation) != LocalPlayer.RelationType.Enemy);
        }

        if (stationConstructionUI.activeSelf) {
            UpdateConstructionUI(((Shipyard)displayedStation).GetConstructionBay());
            if (shipYardOrUpgrade) {
                UpdateShipBlueprintUI();
            } else {
                BattleObject displayedObject = LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject();
                if (displayedObject == displayedStation ||
                    !(displayedObject == upgradeDisplayUnit && upgradeDisplayUnit != null 
                    && (!upgradeDisplayUnit.IsShip() || ((Ship)upgradeDisplayUnit).dockedStation == displayedStation))) {
                    UpdateUpgradeBlueprintUI();
                }
            }
        }
        if (stationHangerUI.activeSelf) {
            UpdateHangerUI(displayedStation.GetHanger());
        }
        Profiler.EndSample();
    }

    void UpdateCargoBayUI(CargoBay cargoBay, bool isFriendlyFaction) {
        if (isFriendlyFaction && cargoBay != null) {
            cargoHeader.gameObject.SetActive(true);
            cargoBaysStatus.text = "Cargo bays in use " + cargoBay.GetUsedCargoBays() + "/" + cargoBay.GetMaxCargoBays();
            cargoBaysStatus.gameObject.SetActive(true);
            cargoBayCapacity.text = "Cargo bay capacity " + NumFormatter.ConvertNumber(cargoBay.GetCargoBayCapacity());
            cargoBayCapacity.gameObject.SetActive(true);
            for (int i = 0; i < cargoBay.cargoBays.Count; i++) {
                if (cargoBayList.childCount <= i) {
                    Instantiate(cargoBayButtonPrefab, cargoBayList);
                }
                Transform cargoBayButton = cargoBayList.GetChild(i);
                cargoBayButton.gameObject.SetActive(true);
                cargoBayButton.GetChild(0).GetComponent<Text>().text = cargoBay.cargoBayTypes[i].ToString();
                cargoBayButton.GetChild(1).GetComponent<Text>().text = NumFormatter.ConvertNumber(cargoBay.cargoBays[i]);
                cargoBayButton.GetChild(2).GetComponent<Text>().text = ((cargoBay.cargoBays[i] * 100) / cargoBay.GetCargoBayCapacity()).ToString() + "%";
            }
            for (int i = cargoBay.cargoBays.Count; i < cargoBayList.childCount; i++) {
                cargoBayList.GetChild(i).gameObject.SetActive(false);
            }
            cargoBayList.transform.parent.parent.gameObject.SetActive(true);
        } else {
            cargoHeader.gameObject.SetActive(false);
            cargoBaysStatus.gameObject.SetActive(false);
            cargoBayCapacity.gameObject.SetActive(false);
            cargoBayList.transform.parent.parent.gameObject.SetActive(false);
        }
    }
    public void SetAutoConstruction(bool autoconstruction) {
        ((SimulationFactionAI)displayedStation.faction.GetFactionAI()).autoConstruction = autoconstruction;
    }

    public void ShipYardButtonSelected() {
        if (!shipYardOrUpgrade) {
            shipYardOrUpgrade = true;
            UpdateShipBlueprintUI();
            shipYardSelection.image.color = new Color(1, 1, 1, 1);
            upgradeSelection.image.color = new Color(.8f, .8f, .8f, 1);
        }
    }

    public void UpgradeButtonSelected() {
        if (shipYardOrUpgrade) {
            shipYardOrUpgrade = false;
            UpdateUpgradeBlueprintUI();
            upgradeSelection.image.color = new Color(1, 1, 1, 1);
            shipYardSelection.image.color = new Color(.8f, .8f, .8f, 1);
        }
    }

    void UpdateShipBlueprintUI() {
        shipBlueprints = BattleManager.Instance.shipBlueprints.ToList();
        for (int i = 0; i < BattleManager.Instance.shipBlueprints.Count; i++) {
            if (blueprintList.childCount <= i) {
                Instantiate(shipBlueprintButtonPrefab, blueprintList);
            }
            Transform cargoBayButton = blueprintList.GetChild(i);
            Ship.ShipBlueprint blueprint = shipBlueprints[i];
            Button button = cargoBayButton.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            int f = i;
            button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => ShipBlueprintButtonPressed(f)));
            cargoBayButton.gameObject.SetActive(true);
            cargoBayButton.GetChild(0).GetComponent<Text>().text = blueprint.name;
            cargoBayButton.GetChild(1).GetComponent<Text>().text = "";
            long cost;
            if (LocalPlayer.Instance.GetFaction() != null) {
                cost = ((Shipyard)displayedStation).GetConstructionBay().GetCreditCostOfShip(LocalPlayer.Instance.faction, blueprint.shipScriptableObject);
                button.interactable = LocalPlayer.Instance.GetFaction().credits >= cost;
            } else {
                cost = blueprint.shipScriptableObject.cost;
                button.interactable = false;
            }
            cargoBayButton.GetChild(2).GetComponent<Text>().text = "Cost: " + NumFormatter.ConvertNumber(cost);
        }
        for (int i = BattleManager.Instance.shipBlueprints.Count; i < blueprintList.childCount; i++) {
            blueprintList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void ShipBlueprintButtonPressed(int index) {
        if (((Shipyard)displayedStation).GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(LocalPlayer.Instance.GetFaction(), shipBlueprints[index]))) {
            UpdateConstructionUI(((Shipyard)displayedStation).GetConstructionBay());
            UpdateShipBlueprintUI();
        }
    }

    void UpdateUpgradeBlueprintUI() {
        if (LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject() != null && LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject().IsShip() && ((Ship)LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject()).dockedStation == displayedStation)
            upgradeDisplayUnit = (Ship)LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject();
        else
            upgradeDisplayUnit = null;
        if (upgradeDisplayUnit == null)
            upgradeDisplayUnit = displayedStation;
        List<ModuleSystem.System> upgradeableSystems = upgradeDisplayUnit.moduleSystem.systems.FindAll(a => a.component != null && a.component.upgrade != null).ToList();
        for (int i = 0; i < upgradeableSystems.Count; i++) {
            if (blueprintList.childCount <= i) {
                Instantiate(shipBlueprintButtonPrefab, blueprintList);
            }
            Transform cargoBayButton = blueprintList.GetChild(i);
            ModuleSystem.System system = upgradeableSystems[i];
            ComponentScriptableObject upgradeComponent = system.component.upgrade;
            cargoBayButton.GetComponent<Button>().onClick.RemoveAllListeners();
            int f = i;
            cargoBayButton.GetComponent<Button>().onClick.AddListener(new UnityEngine.Events.UnityAction(() => UpgradeBlueprintButtonPressed(upgradeDisplayUnit, system)));
            cargoBayButton.gameObject.SetActive(true);
            cargoBayButton.GetChild(0).GetComponent<Text>().text = upgradeComponent.name;
            cargoBayButton.GetChild(1).GetComponent<Text>().text = "";
            cargoBayButton.GetChild(2).GetComponent<Text>().text = "Cost: " + NumFormatter.ConvertNumber((upgradeComponent.cost - system.component.cost) * system.moduleCount);
        }
        for (int i = upgradeableSystems.Count; i < blueprintList.childCount; i++) {
            blueprintList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void UpgradeBlueprintButtonPressed(Unit unit, ModuleSystem.System system) {
        if (unit == null || !unit.IsSpawned()) return;
        unit.moduleSystem.UpgradeSystem(unit.moduleSystem.systems.IndexOf(system), displayedStation);
        UpdateUpgradeBlueprintUI();
    }

    void UpdateConstructionUI(ConstructionBay constructionBay) {
        autoBuildShips.transform.parent.gameObject.SetActive(displayedStation.faction.GetFactionAI() is SimulationFactionAI);
        if (autoBuildShips.gameObject.activeInHierarchy) {
            autoBuildShips.SetIsOnWithoutNotify(((SimulationFactionAI)displayedStation.faction.GetFactionAI()).autoConstruction);
            autoBuildShips.onValueChanged.RemoveAllListeners();
            autoBuildShips.onValueChanged.AddListener((autoConstruction) => SetAutoConstruction(autoConstruction));
        }
        constructionBayStatus.text = "Construction bays in use " + Mathf.Min(constructionBay.buildQueue.Count, constructionBay.GetConstructionBays()) + "/" + constructionBay.GetConstructionBays();
        for (int i = 0; i < constructionBay.buildQueue.Count; i++) {
            if (constructionBayList.childCount <= i) {
                Instantiate(shipButtonPrefab, constructionBayList);
            }
            Transform constructionBayButtonTransform = constructionBayList.GetChild(i);
            Button constructionBayButton = constructionBayButtonTransform.GetComponent<Button>();
            constructionBayButton.onClick.RemoveAllListeners();
            int f = i;
            constructionBayButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() => ConstructionButtonPressed(f)));
            constructionBayButtonTransform.gameObject.SetActive(true);
            Ship.ShipConstructionBlueprint blueprint = constructionBay.buildQueue[i];
            constructionBayButtonTransform.GetChild(0).GetComponent<Text>().text = blueprint.name.ToString();
            constructionBayButtonTransform.GetChild(1).GetComponent<Text>().text = blueprint.faction.abbreviatedName;
            constructionBayButtonTransform.GetChild(2).GetComponent<Text>().text = (100 - (blueprint.GetTotalResourcesPutIn() * 100) / blueprint.totalResourcesRequired).ToString() + "%";
            constructionBayButton.GetComponent<Image>().color = LocalPlayer.Instance.GetColorOfRelationType(LocalPlayer.Instance.GetRelationToFaction(blueprint.GetFaction()));
        }
        for (int i = constructionBay.buildQueue.Count; i < constructionBayList.childCount; i++) {
            constructionBayList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void ConstructionButtonPressed(int index) {
        ConstructionBay constructionBay = ((Shipyard)displayedStation).GetConstructionBay();
        if (LocalPlayer.Instance.GetFaction() != null && constructionBay.buildQueue[index].GetFaction() == LocalPlayer.Instance.GetFaction()) {
            constructionBay.RemoveBlueprintFromQueue(index);
            UpdateConstructionUI(constructionBay);
        }
    }

    void UpdateHangerUI(Hanger hanger) {
        shipsInHanger.Clear();
        LocalPlayerSelectionInput localPlayerSelection = null;
        if (LocalPlayer.Instance.GetLocalPlayerInput() is LocalPlayerSelectionInput) {
            localPlayerSelection = (LocalPlayerSelectionInput)LocalPlayer.Instance.GetLocalPlayerInput();
        }
        for (int i = 0; i < hanger.ships.Count; i++) {
            shipsInHanger.Add(hanger.ships[i]);
        }
        hangerStatus.text = "Hanger capacity " + shipsInHanger.Count + "/" + hanger.GetMaxDockSpace();
        for (int i = 0; i < shipsInHanger.Count; i++) {
            if (hangerList.childCount <= i) {
                Instantiate(shipButtonPrefab, hangerList);
            }
            Transform hangerBayButtonTransform = hangerList.GetChild(i);
            Button hangerBayButton = hangerBayButtonTransform.GetComponent<Button>();
            hangerBayButton.onClick.RemoveAllListeners();
            hangerBayButtonTransform.GetChild(3).GetComponent<Button>().onClick.RemoveAllListeners();
            Ship ship = shipsInHanger[i];
            int f = i;

            hangerBayButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() => HangerButtonPressed(f)));
            hangerBayButtonTransform.gameObject.SetActive(true);
            hangerBayButtonTransform.GetChild(0).GetComponent<Text>().text = ship.GetUnitName();
            hangerBayButtonTransform.GetChild(1).GetComponent<Text>().text = ship.faction.abbreviatedName;
            hangerBayButtonTransform.GetChild(2).GetComponent<Text>().text = ((ship.GetHealth() * 100) / ship.GetMaxHealth()).ToString() + "%";
            hangerBayButtonTransform.GetChild(3).GetComponent<Button>().onClick.AddListener(new UnityEngine.Events.UnityAction(() => HangerInfoButtonPressed(f)));
            hangerBayButton.GetComponent<Image>().color = ship.GetUnitSelection().GetColor();
        }
        for (int i = shipsInHanger.Count; i < hangerList.childCount; i++) {
            hangerList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void HangerButtonPressed(int index) {
        if (LocalPlayer.Instance.GetLocalPlayerInput() is LocalPlayerSelectionInput) {
            LocalPlayerSelectionInput localPlayerSelection = (LocalPlayerSelectionInput)LocalPlayer.Instance.GetLocalPlayerInput();

            if (localPlayerSelection.AdditiveButtonPressed) {
                localPlayerSelection.ToggleSelectedUnit(shipsInHanger[index]);
            } else {
                localPlayerSelection.SelectBattleObjects(shipsInHanger[index]);
            }
            UpdateHangerUI(displayedStation.GetHanger());
        }
        //if (LocalPlayer.Instance.ownedUnits.Contains(displayedStation) || shipsInHanger[index].faction == LocalPlayer.Instance.GetFaction())
        //    shipsInHanger[index].shipAI.AddUnitAICommand(Command.CreateUndockCommand(), Command.CommandAction.AddToBegining);
    }

    public void HangerInfoButtonPressed(int index) {
        LocalPlayer.Instance.GetPlayerUI().CloseAllMenus();
        LocalPlayer.Instance.GetPlayerUI().SetDisplayShip(shipsInHanger[index]);
    }
}