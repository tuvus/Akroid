using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class PlayerShipUI : MonoBehaviour {
    PlayerUI playerUI;
    public Ship displayedShip { get; private set; }
    [SerializeField] GameObject shipStatusUI;
    [SerializeField] Text shipName;
    [SerializeField] Text shipFaction;
    [SerializeField] Text shipClass;
    [SerializeField] Text shipType;
    [SerializeField] Text shipAction;
    [SerializeField] Text shipAI;
    [SerializeField] Text shipFleet;
    [SerializeField] Text shipFleetAI;
    [SerializeField] Text weaponsCount;
    [SerializeField] Text shipTotalDPS;
    [SerializeField] Text maxWeaponRange;
    [SerializeField] Text cargoHeader;
    [SerializeField] Text cargoBaysStatus;
    [SerializeField] Text cargoBayCapacity;
    [SerializeField] Transform cargoBayList;
    [SerializeField] GameObject cargoBayButtonPrefab;
    float updateSpeed;
    float updateTime;

    public void SetupPlayerShipUI(PlayerUI playerUI) {
        this.playerUI = playerUI;
    }

    public void DisplayShip(Ship displayedShip) {
        this.displayedShip = displayedShip;
        if (displayedShip == null)
            return;
        shipStatusUI.SetActive(true);
        UpdateShipDisplay();
    }

    public void UpdateShipUI() {
        updateTime -= Time.deltaTime;
        if (updateTime <= 0) {
            updateTime += updateSpeed;
            UpdateShipDisplay();
        }
    }

    private void UpdateShipDisplay() {
        Profiler.BeginSample("ShipDisplayUpdate");
        if (shipStatusUI.activeSelf) {
            UpdateShipStatusUI();
        }
        Profiler.EndSample();
    }

    void UpdateShipStatusUI() {
        shipName.text = displayedShip.GetUnitName();
        shipFaction.text = "Faction: " + displayedShip.faction.name;
        shipClass.text = "Ship Class: " + displayedShip.GetShipClass();
        shipType.text = "Ship Type: " + displayedShip.GetShipType();
        if (displayedShip.shipAI.commands.Count > 0)
            shipAI.text = "ShipAI: " + displayedShip.shipAI.commands.First().commandType.ToString() + ", " + displayedShip.shipAI.currentCommandState.ToString();
        else shipAI.text = "ShipAI: Idle";

        if (displayedShip.fleet != null) {
            shipFleet.gameObject.SetActive(true);
            shipFleet.text = "Fleet: " + displayedShip.fleet.GetFleetName();
            shipFleetAI.gameObject.SetActive(true);
            if (displayedShip.fleet.FleetAI.commands.Count > 0)
                shipFleetAI.text = "FleetAI: " + displayedShip.fleet.FleetAI.commands.First().commandType.ToString() + ", " + displayedShip.fleet.FleetAI.currentCommandState.ToString();
            else shipFleetAI.text = "FleetAI: Idle";
        } else {
            shipFleet.gameObject.SetActive(false);
            shipFleetAI.gameObject.SetActive(false);
        }
        weaponsCount.text = "Weapons: " + displayedShip.GetWeaponCount();
        if (displayedShip.GetWeaponCount() > 0) {
            shipTotalDPS.text = "Damage Per Second: " + NumFormatter.ConvertNumber(displayedShip.GetUnitDamagePerSecond());
            maxWeaponRange.text = "Weapon Range: " + NumFormatter.ConvertNumber(displayedShip.GetMaxWeaponRange());
            shipTotalDPS.gameObject.SetActive(true);
            maxWeaponRange.gameObject.SetActive(true);
        } else {
            shipTotalDPS.gameObject.SetActive(false);
            maxWeaponRange.gameObject.SetActive(false);
        }
        shipAction.text = "Ship Action: " + displayedShip.shipAction.ToString();
        UpdateCargoBayUI(displayedShip.GetCargoBay(), !LocalPlayer.Instance.GetFaction().IsAtWarWithFaction(displayedShip.faction));
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
}