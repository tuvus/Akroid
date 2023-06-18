using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] GameObject shipBlueprintButtonPrefab;
    [SerializeField] Transform buildShipsList;
    [SerializeField] Text constructionBayStatus;
    [SerializeField] Transform constructionBayList;
    public void SetupPlayerStationUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
        UpdateShipBlueprintUI();
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
        stationStatusUI.SetActive(displayedStation.stationType != Station.StationType.None);
        stationConstructionUI.SetActive(!LocalPlayer.Instance.GetFaction().IsAtWarWithFaction(displayedStation.faction) && (displayedStation.stationType == Station.StationType.Shipyard || displayedStation.stationType == Station.StationType.FleetCommand));
        stationHangerUI.SetActive(!LocalPlayer.Instance.GetFaction().IsAtWarWithFaction(displayedStation.faction) && displayedStation.GetHanger() != null);
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
                stationTotalDPS.text = "Damage Per Second: " + ((int)(displayedStation.GetUnitDamagePerSecond() * 10) / 10f);
                maxWeaponRange.text = "Weapon Range: " + displayedStation.GetMaxWeaponRange();
                stationTotalDPS.gameObject.SetActive(true);
                maxWeaponRange.gameObject.SetActive(true);
            } else {
                stationTotalDPS.gameObject.SetActive(false);
                maxWeaponRange.gameObject.SetActive(false);
            }
            UpdateCargoBayUI(displayedStation.GetCargoBay(), !LocalPlayer.Instance.GetFaction().IsAtWarWithFaction(displayedStation.faction));
        }
        if (stationConstructionUI.activeSelf) {
            UpdateConstructionUI(((Shipyard)displayedStation).GetConstructionBay());
        }
        if (stationHangerUI.activeSelf) {
            UpdateHangerUI(displayedStation.GetHanger());
        }
        Profiler.EndSample();
    }

    void UpdateCargoBayUI(CargoBay cargoBay, bool isFreindlyFaction) {
        if (isFreindlyFaction && cargoBay != null) {
            cargoHeader.gameObject.SetActive(true);
            cargoBaysStatus.text = "Cargo bays in use " + cargoBay.GetUsedCargoBays() + "/" + cargoBay.GetMaxCargoBays();
            cargoBaysStatus.gameObject.SetActive(true);
            cargoBayCapacity.text = "Cargo bay capacity " + cargoBay.GetCargoBayCapacity();
            cargoBayCapacity.gameObject.SetActive(true);
            for (int i = 0; i < cargoBay.cargoBays.Count; i++) {
                if (cargoBayList.childCount <= i) {
                    Instantiate(cargoBayButtonPrefab, cargoBayList);
                }
                Transform cargoBayButton = cargoBayList.GetChild(i);
                cargoBayButton.gameObject.SetActive(true);
                cargoBayButton.GetChild(0).GetComponent<Text>().text = cargoBay.cargoBayTypes[i].ToString();
                cargoBayButton.GetChild(1).GetComponent<Text>().text = cargoBay.cargoBays[i].ToString();
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

    void UpdateShipBlueprintUI() {
        for (int i = 0; i < BattleManager.Instance.shipBlueprints.Count; i++) {
            if (buildShipsList.childCount <= i) {
                Instantiate(shipBlueprintButtonPrefab, buildShipsList);
            }
            Transform cargoBayButton = buildShipsList.GetChild(i);
            Ship.ShipBlueprint blueprint = BattleManager.Instance.shipBlueprints[i];
            cargoBayButton.GetComponent<Button>().onClick.RemoveAllListeners();
            int f = i;
            cargoBayButton.GetComponent<Button>().onClick.AddListener(new UnityEngine.Events.UnityAction(() => ShipBlueprintButtonPressed(f)));
            cargoBayButton.gameObject.SetActive(true);
            cargoBayButton.GetChild(0).GetComponent<Text>().text = blueprint.shipName;
            cargoBayButton.GetChild(1).GetComponent<Text>().text = "Cost: " + blueprint.shipCost.ToString();
        }
        for (int i = BattleManager.Instance.shipBlueprints.Count; i < buildShipsList.childCount; i++) {
            buildShipsList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void ShipBlueprintButtonPressed(int index) {
        ((Shipyard)displayedStation).GetConstructionBay().AddConstructionToQueue(BattleManager.Instance.shipBlueprints[index].CreateShipBlueprint(LocalPlayer.Instance.GetFaction().factionIndex));
        UpdateConstructionUI(((Shipyard)displayedStation).GetConstructionBay());
    }

    void UpdateConstructionUI(ConstructionBay constructionBay) {
        autoBuildShips.transform.parent.gameObject.SetActive(displayedStation.faction.GetFactionAI() is SimulationFactionAI);
        if (autoBuildShips.gameObject.activeInHierarchy) {
            autoBuildShips.SetIsOnWithoutNotify(((SimulationFactionAI)displayedStation.faction.GetFactionAI()).autoBuildShips);
            autoBuildShips.onValueChanged.RemoveAllListeners();
            autoBuildShips.onValueChanged.AddListener((autoBuildShips) => SetAutoBuildShips(autoBuildShips));
        }
        constructionBayStatus.text = "Construction bays in use " + Mathf.Min(constructionBay.buildQueue.Count, constructionBay.constructionBays) + "/" + constructionBay.constructionBays;
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
            Ship.ShipBlueprint blueprint = constructionBay.buildQueue[i];
            constructionBayButtonTransform.GetChild(0).GetComponent<Text>().text = blueprint.shipName.ToString();
            constructionBayButtonTransform.GetChild(1).GetComponent<Text>().text = BattleManager.Instance.factions[blueprint.factionIndex].name;
            constructionBayButtonTransform.GetChild(2).GetComponent<Text>().text = (100 - (blueprint.GetTotalResourcesPutIn() * 100) / blueprint.totalResourcesRequired).ToString() + "%";

        }
        for (int i = constructionBay.buildQueue.Count; i < constructionBayList.childCount; i++) {
            constructionBayList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void SetAutoBuildShips(bool autoBuildShips) {
        ((SimulationFactionAI)displayedStation.faction.GetFactionAI()).autoBuildShips = autoBuildShips;
    }

    public void ConstructionButtonPressed(int index) {
        ConstructionBay constructionBay = ((Shipyard)displayedStation).GetConstructionBay();
        if (constructionBay.buildQueue[index].factionIndex == LocalPlayer.Instance.GetFaction().factionIndex) {
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
            int f = i;
            hangerBayButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() => HangerButtonPressed(f)));
            hangerBayButtonTransform.gameObject.SetActive(true);
            hangerBayButtonTransform.GetChild(0).GetComponent<Text>().text = shipsInHanger[i].GetUnitName();
            hangerBayButtonTransform.GetChild(1).GetComponent<Text>().text = shipsInHanger[i].faction.name;
            hangerBayButtonTransform.GetChild(2).GetComponent<Text>().text = ((shipsInHanger[i].GetHealth() * 100) / shipsInHanger[i].GetMaxHealth()).ToString() + "%";
            if (localPlayerSelection != null && localPlayerSelection.GetSelectedUnits().ContainsUnit(shipsInHanger[i])) {
                hangerBayButton.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            } else {
                hangerBayButton.GetComponent<Image>().color = new Color(.5f, .5f, 5f, 1);
            }
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
                localPlayerSelection.SelectUnits(shipsInHanger[index]);
            }
            UpdateHangerUI(displayedStation.GetHanger());
        }
        //if (LocalPlayer.Instance.ownedUnits.Contains(displayedStation) || shipsInHanger[index].faction == LocalPlayer.Instance.GetFaction())
        //    shipsInHanger[index].shipAI.AddUnitAICommand(Command.CreateUndockCommand(), Command.CommandAction.AddToBegining);
    }
}