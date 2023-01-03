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
    [SerializeField] Text cargoBaysStatus;
    [SerializeField] Text cargoBayCapacity;
    [SerializeField] Transform cargoBayList;
    [SerializeField] GameObject cargoBayButtonPrefab;
    [SerializeField] Text hangerStatus;
    [SerializeField] Transform hangerList;
    [SerializeField] GameObject shipButtonPrefab;
    [SerializeField] List<Ship> shipsInHanger;
    float updateSpeed;
    float updateTime;
    public void SetupPlayerStationUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
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
        stationStatusUI.SetActive(displayedStation.stationType != Station.StationType.None);
        stationConstructionUI.SetActive(displayedStation.stationType == Station.StationType.Shipyard || displayedStation.stationType == Station.StationType.FleetCommand);
        stationHangerUI.SetActive(displayedStation.GetHanger() != null);
        UpdateStationDisplay();
    }

    public void UpdateStationDisplay() {
        Profiler.BeginSample("StationDisplayUpdate");
        if (stationStatusUI.activeSelf) {
            stationName.text = displayedStation.GetUnitName();
            UpdateCargoBayUI(displayedStation.GetCargoBay());
        }
        if (stationConstructionUI.activeSelf) {
            UpdateConstructionUI(displayedStation.GetCargoBay());
        }
        if (stationHangerUI.activeSelf) {
            UpdateHangerUI(displayedStation.GetHanger());
        }
        Profiler.EndSample();
    }

    void UpdateCargoBayUI(CargoBay cargoBay) {
        cargoBaysStatus.text = "Cargo bays in use " + cargoBay.GetUsedCargoBays() + "/" + cargoBay.GetMaxCargoBays();
        cargoBayCapacity.text = "Cargo bay capacity " + cargoBay.GetCargoBayCapacity();
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
    }

    void UpdateConstructionUI(CargoBay cargoBay) {
        return;
        cargoBaysStatus.text = "Cargo bays in use " + cargoBay.GetUsedCargoBays() + "/" + cargoBay.GetMaxCargoBays();
        cargoBayCapacity.text = "Cargo bay capacity " + cargoBay.GetCargoBayCapacity();
        for (int i = 0; i < cargoBay.cargoBays.Count; i++) {
            if (cargoBayList.childCount <= i) {
                Instantiate(cargoBayButtonPrefab, cargoBayList);
            }
            Transform cargoBayButton = cargoBayList.GetChild(i);
            cargoBayButton.gameObject.SetActive(true);
            cargoBayButton.GetChild(0).GetComponent<Text>().text = cargoBay.cargoBayTypes[i].ToString();
            cargoBayButton.GetChild(1).GetComponent<Text>().text = cargoBay.cargoBays[i].ToString();
        }
        for (int i = cargoBay.cargoBays.Count; i < cargoBayList.childCount; i++) {
            cargoBayList.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < cargoBay.cargoBays.Count; i++) {
            if (cargoBayList.childCount <= i) {
                Instantiate(cargoBayButtonPrefab, cargoBayList);
            }
            Transform cargoBayButton = cargoBayList.GetChild(i);
            cargoBayButton.gameObject.SetActive(true);
            cargoBayButton.GetChild(0).GetComponent<Text>().text = cargoBay.cargoBayTypes[i].ToString();
            cargoBayButton.GetChild(1).GetComponent<Text>().text = cargoBay.cargoBays[i].ToString();
        }
        for (int i = cargoBay.cargoBays.Count; i < cargoBayList.childCount; i++) {
            cargoBayList.GetChild(i).gameObject.SetActive(false);
        }
    }

    void UpdateHangerUI (Hanger hanger) {
        shipsInHanger.Clear();
        for (int i = 0; i < hanger.ships.Count; i++) {
            shipsInHanger.Add(hanger.ships[i]);
        }
        hangerStatus.text = "Hanger capacity " + shipsInHanger.Count + "/" + hanger.GetMaxDockSpace();
        for (int i = 0; i < shipsInHanger.Count; i++) {
            if (hangerList.childCount <= i) {
                Instantiate(shipButtonPrefab, hangerList);
            }
            Transform hangerBayButton = hangerList.GetChild(i);
            hangerBayButton.GetComponent<Button>().onClick.RemoveAllListeners();
            int f = i;
            hangerBayButton.GetComponent<Button>().onClick.AddListener(new UnityEngine.Events.UnityAction(() => HangerButtonPressed(f)));
            hangerBayButton.gameObject.SetActive(true);
            hangerBayButton.GetChild(0).GetComponent<Text>().text = shipsInHanger[i].GetUnitName();
            hangerBayButton.GetChild(1).GetComponent<Text>().text = shipsInHanger[i].faction.name;
            hangerBayButton.GetChild(2).GetComponent<Text>().text = ((shipsInHanger[i].GetHealth() * 100) / shipsInHanger[i].GetMaxHealth()).ToString() + "%";
        }
        for (int i = shipsInHanger.Count; i < hangerList.childCount; i++) {
            hangerList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void HangerButtonPressed(int index) {
        shipsInHanger[index].shipAI.AddUnitAICommand(Command.CreateUndockCommand(), Command.CommandAction.AddToBegining);
    }
}