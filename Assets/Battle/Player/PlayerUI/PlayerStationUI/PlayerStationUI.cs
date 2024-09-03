using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStationUI : PlayerUIMenu<Station> {
    [SerializeField] TMP_Text stationName;
    [SerializeField] TMP_Text stationFaction;
    [SerializeField] TMP_Text stationType;
    [SerializeField] TMP_Text weaponsCount;
    [SerializeField] TMP_Text stationTotalDPS;
    [SerializeField] TMP_Text maxWeaponRange;
    [SerializeField] TMP_Text cargoBaysStatus;
    [SerializeField] TMP_Text cargoBayCapacity;
    [SerializeField] TMP_Text cargoHeader;
    [SerializeField] Transform cargoBayList;
    [SerializeField] GameObject cargoBayButtonPrefab;
    [SerializeField] TMP_Text hangarStatus;
    [SerializeField] Transform hangarList;
    [SerializeField] GameObject shipButtonPrefab;
    [SerializeField] List<Ship> shipsInHangar;
    [SerializeField] Toggle autoBuildShips;
    [SerializeField] Button shipYardSelection;
    [SerializeField] Button upgradeSelection;
    /// <summary> True for shipyard, false for upgrade </summary>
    bool shipYardOrUpgrade = true;
    [SerializeField] GameObject shipBlueprintButtonPrefab;
    [SerializeField] Transform blueprintList;
    Unit upgradeDisplayUnit;
    [SerializeField] TMP_Text constructionBayStatus;
    [SerializeField] Transform constructionBayList;
    List<Ship.ShipBlueprint> shipBlueprints;

    protected override bool IsObjectViable() {
        return displayedObject != null && displayedObject.IsSpawned();
    }

    protected override bool ShouldShowMiddlePanel() {
        return displayedObject.stationType != Station.StationType.None;
    }

    protected override bool ShouldShowLeftPanel() {
        bool isEnemy = LocalPlayer.Instance.GetRelationToUnit(displayedObject) == LocalPlayer.RelationType.Enemy;
        return !isEnemy && (displayedObject.stationType == Station.StationType.Shipyard || displayedObject.stationType == Station.StationType.FleetCommand);
    }

    protected override bool ShouldShowRightPanel() {
        bool isEnemy = LocalPlayer.Instance.GetRelationToUnit(displayedObject) == LocalPlayer.RelationType.Enemy;
        return !isEnemy && displayedObject.moduleSystem.Get<Hangar>().Any();
    }

    protected override void RefreshMiddlePanel() {
        stationName.text = displayedObject.GetUnitName();
        stationFaction.text = displayedObject.faction.name;
        stationType.text = "Station Type: " + displayedObject.stationType;
        weaponsCount.text = "Weapons: " + displayedObject.GetWeaponCount();
        if (displayedObject.GetWeaponCount() > 0) {
            stationTotalDPS.text = "Damage Per Second: " + NumFormatter.ConvertNumber(displayedObject.GetUnitDamagePerSecond());
            maxWeaponRange.text = "Weapon Range: " + NumFormatter.ConvertNumber(displayedObject.GetMaxWeaponRange());
            stationTotalDPS.gameObject.SetActive(true);
            maxWeaponRange.gameObject.SetActive(true);
        } else {
            stationTotalDPS.gameObject.SetActive(false);
            maxWeaponRange.gameObject.SetActive(false);
        }
        UpdateCargoBayUI(displayedObject.moduleSystem.Get<CargoBay>().FirstOrDefault(), LocalPlayer.Instance.GetRelationToUnit(displayedObject) != LocalPlayer.RelationType.Enemy);
    }

    void UpdateCargoBayUI(CargoBay cargoBay, bool isFriendlyFaction) {
        if (isFriendlyFaction && cargoBay != null) {
            cargoHeader.transform.parent.parent.gameObject.SetActive(true);
            cargoBaysStatus.text = "Cargo bays in use " + cargoBay.GetUsedCargoBays() + "/" + cargoBay.GetMaxCargoBays();
            cargoBayCapacity.text = "Cargo bay capacity " + NumFormatter.ConvertNumber(cargoBay.GetCargoBayCapacity());
            for (int i = 0; i < cargoBay.cargoBays.Count; i++) {
                if (cargoBayList.childCount <= i) {
                    Instantiate(cargoBayButtonPrefab, cargoBayList);
                }
                Transform cargoBayButton = cargoBayList.GetChild(i);
                cargoBayButton.gameObject.SetActive(true);
                cargoBayButton.GetChild(0).GetComponent<TMP_Text>().text = cargoBay.cargoBayTypes[i].ToString();
                cargoBayButton.GetChild(1).GetComponent<TMP_Text>().text = NumFormatter.ConvertNumber(cargoBay.cargoBays[i]);
                cargoBayButton.GetChild(2).GetComponent<TMP_Text>().text = ((cargoBay.cargoBays[i] * 100) / cargoBay.GetCargoBayCapacity()).ToString() + "%";
            }
            for (int i = cargoBay.cargoBays.Count; i < cargoBayList.childCount; i++) {
                cargoBayList.GetChild(i).gameObject.SetActive(false);
            }
            cargoBayList.transform.parent.parent.gameObject.SetActive(true);
        } else {
            cargoHeader.transform.parent.parent.gameObject.SetActive(false);
        }
    }

    protected override void RefreshLeftPanel() {
        UpdateConstructionUI(((Shipyard)displayedObject).GetConstructionBay());
        if (shipYardOrUpgrade) {
            UpdateShipBlueprintUI();
        } else {
            BattleObject displayedObject = LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject();
            if (displayedObject == base.displayedObject ||
                !(displayedObject == upgradeDisplayUnit && upgradeDisplayUnit != null
                && (!upgradeDisplayUnit.IsShip() || ((Ship)upgradeDisplayUnit).dockedStation == base.displayedObject))) {
                UpdateUpgradeBlueprintUI();
            }
        }
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
            cargoBayButton.GetChild(0).GetComponent<TMP_Text>().text = blueprint.name;
            long cost;
            if (LocalPlayer.Instance.GetFaction() != null) {
                cost = ((Shipyard)displayedObject).GetConstructionBay().GetCreditCostOfShip(LocalPlayer.Instance.faction, blueprint.shipScriptableObject);
                button.interactable = LocalPlayer.Instance.GetFaction().credits >= cost;
            } else {
                cost = blueprint.shipScriptableObject.cost;
                button.interactable = false;
            }
            cargoBayButton.GetChild(1).GetComponent<TMP_Text>().text = "Cost: " + NumFormatter.ConvertNumber(cost);
        }
        for (int i = BattleManager.Instance.shipBlueprints.Count; i < blueprintList.childCount; i++) {
            blueprintList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void ShipBlueprintButtonPressed(int index) {
        if (((Shipyard)displayedObject).GetConstructionBay().AddConstructionToQueue(new Ship.ShipConstructionBlueprint(LocalPlayer.Instance.GetFaction(), shipBlueprints[index]))) {
            UpdateConstructionUI(((Shipyard)displayedObject).GetConstructionBay());
            UpdateShipBlueprintUI();
        }
    }

    void UpdateUpgradeBlueprintUI() {
        if (LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject() != null && LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject().IsShip() && ((Ship)LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject()).dockedStation == displayedObject)
            upgradeDisplayUnit = (Ship)LocalPlayer.Instance.GetLocalPlayerInput().GetDisplayedBattleObject();
        else
            upgradeDisplayUnit = null;
        if (upgradeDisplayUnit == null)
            upgradeDisplayUnit = displayedObject;
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
            cargoBayButton.GetChild(0).GetComponent<TMP_Text>().text = upgradeComponent.name;
            cargoBayButton.GetChild(1).GetComponent<TMP_Text>().text = "Cost: " + NumFormatter.ConvertNumber((upgradeComponent.cost - system.component.cost) * system.moduleCount);
        }
        for (int i = upgradeableSystems.Count; i < blueprintList.childCount; i++) {
            blueprintList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void UpgradeBlueprintButtonPressed(Unit unit, ModuleSystem.System system) {
        if (unit == null || !unit.IsSpawned()) return;
        unit.moduleSystem.UpgradeSystem(unit.moduleSystem.systems.IndexOf(system), displayedObject);
        UpdateUpgradeBlueprintUI();
    }

    void UpdateConstructionUI(ConstructionBay constructionBay) {
        autoBuildShips.transform.parent.gameObject.SetActive(displayedObject.faction.GetFactionAI() is SimulationFactionAI);
        if (autoBuildShips.gameObject.activeInHierarchy) {
            autoBuildShips.SetIsOnWithoutNotify(((SimulationFactionAI)displayedObject.faction.GetFactionAI()).autoConstruction);
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
            constructionBayButtonTransform.GetChild(0).GetComponent<TMP_Text>().text = blueprint.name.ToString();
            constructionBayButtonTransform.GetChild(1).GetComponent<TMP_Text>().text = blueprint.faction.abbreviatedName;
            constructionBayButtonTransform.GetChild(2).GetComponent<TMP_Text>().text = (100 - (blueprint.GetTotalResourcesLeftToUse() * 100) / blueprint.totalResourcesRequired).ToString() + "%";
            constructionBayButton.GetComponent<Image>().color = LocalPlayer.Instance.GetColorOfRelationType(LocalPlayer.Instance.GetRelationToFaction(blueprint.GetFaction()));
        }
        for (int i = constructionBay.buildQueue.Count; i < constructionBayList.childCount; i++) {
            constructionBayList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void SetAutoConstruction(bool autoconstruction) {
        ((SimulationFactionAI)displayedObject.faction.GetFactionAI()).autoConstruction = autoconstruction;
    }

    public void ConstructionButtonPressed(int index) {
        ConstructionBay constructionBay = ((Shipyard)displayedObject).GetConstructionBay();
        if (LocalPlayer.Instance.GetFaction() != null && constructionBay.buildQueue[index].GetFaction() == LocalPlayer.Instance.GetFaction()) {
            constructionBay.RemoveBlueprintFromQueue(index);
            UpdateConstructionUI(constructionBay);
        }
    }

    protected override void RefreshRightPanel() {
        Hangar hangar = displayedObject.moduleSystem.Get<Hangar>().First();
        shipsInHangar.Clear();
        LocalPlayerSelectionInput localPlayerSelection = null;
        if (LocalPlayer.Instance.GetLocalPlayerInput() is LocalPlayerSelectionInput) {
            localPlayerSelection = (LocalPlayerSelectionInput)LocalPlayer.Instance.GetLocalPlayerInput();
        }
        for (int i = 0; i < hangar.ships.Count; i++) {
            shipsInHangar.Add(hangar.ships[i]);
        }
        hangarStatus.text = "Hangar capacity " + shipsInHangar.Count + "/" + hangar.GetMaxDockSpace();
        for (int i = 0; i < shipsInHangar.Count; i++) {
            if (hangarList.childCount <= i) {
                Instantiate(shipButtonPrefab, hangarList);
            }
            Transform hangarBayButtonTransform = hangarList.GetChild(i);
            Button hangarBayButton = hangarBayButtonTransform.GetComponent<Button>();
            hangarBayButton.onClick.RemoveAllListeners();
            hangarBayButtonTransform.GetChild(3).GetComponent<Button>().onClick.RemoveAllListeners();
            Ship ship = shipsInHangar[i];
            int f = i;

            hangarBayButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() => HangarButtonPressed(f)));
            hangarBayButtonTransform.gameObject.SetActive(true);
            hangarBayButtonTransform.GetChild(0).GetComponent<TMP_Text>().text = ship.GetUnitName();
            hangarBayButtonTransform.GetChild(1).GetComponent<TMP_Text>().text = ship.faction.abbreviatedName;
            hangarBayButtonTransform.GetChild(2).GetComponent<TMP_Text>().text = ((ship.GetHealth() * 100) / ship.GetMaxHealth()).ToString() + "%";
            hangarBayButtonTransform.GetChild(3).GetComponent<Button>().onClick.AddListener(new UnityEngine.Events.UnityAction(() => HangarInfoButtonPressed(f)));
            hangarBayButton.GetComponent<Image>().color = ship.GetUnitSelection().GetColor();
        }
        for (int i = shipsInHangar.Count; i < hangarList.childCount; i++) {
            hangarList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void HangarButtonPressed(int index) {
        if (LocalPlayer.Instance.GetLocalPlayerInput() is LocalPlayerSelectionInput) {
            LocalPlayerSelectionInput localPlayerSelection = (LocalPlayerSelectionInput)LocalPlayer.Instance.GetLocalPlayerInput();

            if (localPlayerSelection.AdditiveButtonPressed) {
                localPlayerSelection.ToggleSelectedUnit(shipsInHangar[index]);
            } else {
                localPlayerSelection.SelectBattleObjects(shipsInHangar[index]);
            }
            RefreshRightPanel();
        }
        //if (LocalPlayer.Instance.ownedUnits.Contains(displayedStation) || shipsInHangar[index].faction == LocalPlayer.Instance.GetFaction())
        //    shipsInHangar[index].shipAI.AddUnitAICommand(Command.CreateUndockCommand(), Command.CommandAction.AddToBegining);
    }

    public void HangarInfoButtonPressed(int index) {
        LocalPlayer.Instance.GetPlayerUI().CloseAllMenus();
        LocalPlayer.Instance.GetPlayerUI().SetDisplayedObject(shipsInHangar[index]);
    }

    public void OpenFactionMenu() {
        Faction faction = displayedObject.faction;
        playerUI.ShowFactionUI(faction);
    }
}