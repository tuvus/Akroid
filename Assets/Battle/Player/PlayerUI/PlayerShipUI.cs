using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerShipUI : PlayerUIMenu<Ship> {
    [SerializeField] TMP_Text shipName;
    [SerializeField] TMP_Text shipFaction;
    [SerializeField] TMP_Text shipClass;
    [SerializeField] TMP_Text shipType;
    [SerializeField] TMP_Text shipAction;
    [SerializeField] TMP_Text shipAI;
    [SerializeField] TMP_Text shipFleet;
    [SerializeField] TMP_Text shipFleetAI;
    [SerializeField] TMP_Text weaponsCount;
    [SerializeField] TMP_Text shipTotalDPS;
    [SerializeField] TMP_Text maxWeaponRange;
    [SerializeField] TMP_Text cargoHeader;
    [SerializeField] TMP_Text cargoBaysStatus;
    [SerializeField] TMP_Text cargoBayCapacity;
    [SerializeField] Transform cargoBayList;
    [SerializeField] GameObject cargoBayButtonPrefab;

    protected override bool IsObjectViable() {
        return displayedObject != null && displayedObject.IsSpawned();
    }

    protected override void RefreshMiddlePanel() {
        shipName.text = displayedObject.GetUnitName();
        shipFaction.text = displayedObject.faction.name;
        shipClass.text = "Ship Class: " + displayedObject.GetShipClass();
        shipType.text = "Ship Type: " + displayedObject.GetShipType();
        if (displayedObject.shipAI.commands.Count > 0)
            shipAI.text = "ShipAI: " + displayedObject.shipAI.commands.First().commandType.ToString() + ", " + displayedObject.shipAI.currentCommandState.ToString();
        else shipAI.text = "ShipAI: Idle";

        if (displayedObject.fleet != null) {
            shipFleet.gameObject.SetActive(true);
            shipFleet.text = "Fleet: " + displayedObject.fleet.GetFleetName();
            shipFleetAI.gameObject.SetActive(true);
            if (displayedObject.fleet.FleetAI.commands.Count > 0)
                shipFleetAI.text = "FleetAI: " + displayedObject.fleet.FleetAI.commands.First().commandType.ToString() + ", " + displayedObject.fleet.FleetAI.currentCommandState.ToString();
            else shipFleetAI.text = "FleetAI: Idle";
        } else {
            shipFleet.gameObject.SetActive(false);
            shipFleetAI.gameObject.SetActive(false);
        }
        weaponsCount.text = "Weapons: " + displayedObject.GetWeaponCount();
        if (displayedObject.GetWeaponCount() > 0) {
            shipTotalDPS.text = "Damage Per Second: " + NumFormatter.ConvertNumber(displayedObject.GetUnitDamagePerSecond());
            maxWeaponRange.text = "Weapon Range: " + NumFormatter.ConvertNumber(displayedObject.GetMaxWeaponRange());
            shipTotalDPS.gameObject.SetActive(true);
            maxWeaponRange.gameObject.SetActive(true);
        } else {
            shipTotalDPS.gameObject.SetActive(false);
            maxWeaponRange.gameObject.SetActive(false);
        }
        shipAction.text = "Ship Action: " + displayedObject.shipAction.ToString();
        UpdateCargoBayUI(displayedObject.moduleSystem.Get<CargoBay>().FirstOrDefault(), !LocalPlayer.Instance.GetFaction().IsAtWarWithFaction(displayedObject.faction));
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
                cargoBayButton.GetChild(1).GetComponent<TMP_Text>().text = cargoBay.cargoBays[i].ToString();
                cargoBayButton.GetChild(2).GetComponent<TMP_Text>().text = ((cargoBay.cargoBays[i] * 100) / cargoBay.GetCargoBayCapacity()).ToString() + "%";
            }
            for (int i = cargoBay.cargoBays.Count; i < cargoBayList.childCount; i++) {
                cargoBayList.GetChild(i).gameObject.SetActive(false);
            }
        } else {
            cargoHeader.transform.parent.parent.gameObject.SetActive(false);
        }
    }

    public void OpenFactionMenu() {
        Faction faction = displayedObject.faction;
        playerUI.ShowFactionUI(faction);
    }
}