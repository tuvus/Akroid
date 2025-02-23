using System.Linq;
using TMPro;
using UnityEngine;

public class PlayerShipUI : PlayerUIMenu<ShipUI> {
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
        return displayedObject != null && displayedObject.ship.IsSpawned();
    }

    protected override void RefreshMiddlePanel() {
        shipName.text = displayedObject.ship.GetUnitName();
        shipFaction.text = displayedObject.ship.faction.name;
        shipClass.text = "Ship Class: " + displayedObject.ship.GetShipClass();
        shipType.text = "Ship Type: " + displayedObject.ship.GetShipType();
        if (displayedObject.ship.shipAI.commands.Count > 0)
            shipAI.text = "ShipAI: " + displayedObject.ship.shipAI.commands.First().commandType.ToString() + ", " +
                          displayedObject.ship.shipAI.currentCommandState.ToString();
        else shipAI.text = "ShipAI: Idle";

        if (displayedObject.ship.fleet != null) {
            shipFleet.gameObject.SetActive(true);
            shipFleet.text = "Fleet: " + displayedObject.ship.fleet.GetFleetName();
            shipFleetAI.gameObject.SetActive(true);
            if (displayedObject.ship.fleet.fleetAI.commands.Count > 0)
                shipFleetAI.text = "FleetAI: " + displayedObject.ship.fleet.fleetAI.commands.First().commandType.ToString() + ", " +
                                   displayedObject.ship.fleet.fleetAI.currentCommandState.ToString();
            else shipFleetAI.text = "FleetAI: Idle";
        } else {
            shipFleet.gameObject.SetActive(false);
            shipFleetAI.gameObject.SetActive(false);
        }

        weaponsCount.text = "Weapons: " + displayedObject.ship.GetWeaponCount();
        if (displayedObject.ship.GetWeaponCount() > 0) {
            shipTotalDPS.text = "Damage Per Second: " + NumFormatter.ConvertNumber(displayedObject.ship.GetUnitDamagePerSecond());
            maxWeaponRange.text = "Weapon Range: " + NumFormatter.ConvertNumber(displayedObject.ship.GetMaxWeaponRange());
            shipTotalDPS.gameObject.SetActive(true);
            maxWeaponRange.gameObject.SetActive(true);
        } else {
            shipTotalDPS.gameObject.SetActive(false);
            maxWeaponRange.gameObject.SetActive(false);
        }

        shipAction.text = "Ship Action: " + displayedObject.ship.shipAction.ToString();
        UpdateCargoBayUI(displayedObject.ship.moduleSystem.Get<CargoBay>().FirstOrDefault(),
            !LocalPlayer.Instance.GetFaction().IsAtWarWithFaction(displayedObject.ship.faction));
    }

    void UpdateCargoBayUI(CargoBay cargoBay, bool isFriendlyFaction) {
        if (isFriendlyFaction && cargoBay != null) {
            cargoHeader.transform.parent.parent.gameObject.SetActive(true);
            cargoBaysStatus.text = "Cargo bays in use " + cargoBay.GetCargoBaysUsed() + "/" + cargoBay.GetMaxCargoBays();
            cargoBayCapacity.text = "Cargo bay capacity " + NumFormatter.ConvertNumber(cargoBay.GetCargoBayCapacity());

            int cargoBayIndex = 0;
            foreach (var cargoBayType in cargoBay.cargoBays) {
                int numberOfCargoBaysUsed = cargoBay.GetCargoBaysUsedByType(cargoBayType.Key);
                for (int i = 0; i < numberOfCargoBaysUsed; i++) {
                    if (cargoBayList.childCount <= cargoBayIndex + i) {
                        Instantiate(cargoBayButtonPrefab, cargoBayList);
                    }

                    // If we are not the last cargo bay then we are guaranteed to be full.
                    long amount = cargoBay.GetCargoBayCapacity();
                    int percent = 100;
                    // If we are the last cargo bay then we need to calculate how full we are.
                    if (i == numberOfCargoBaysUsed - 1 && cargoBayType.Value / cargoBay.GetCargoBayCapacity() < numberOfCargoBaysUsed) {
                        amount = cargoBayType.Value % cargoBay.GetCargoBayCapacity();
                        percent = (int)(amount * 100 / cargoBay.GetCargoBayCapacity());
                    }

                    Transform cargoBayButton = cargoBayList.GetChild(cargoBayIndex + i);
                    cargoBayButton.gameObject.SetActive(true);
                    cargoBayButton.GetChild(0).GetComponent<TMP_Text>().text = cargoBayType.Key.ToString();
                    cargoBayButton.GetChild(1).GetComponent<TMP_Text>().text = amount.ToString();
                    cargoBayButton.GetChild(2).GetComponent<TMP_Text>().text = percent.ToString() + "%";
                }
                cargoBayIndex+=numberOfCargoBaysUsed;
            }

            for (int i = cargoBayIndex; i < cargoBayList.childCount; i++) {
                cargoBayList.GetChild(i).gameObject.SetActive(false);
            }
        } else {
            cargoHeader.transform.parent.parent.gameObject.SetActive(false);
        }
    }

    public void OpenFactionMenu() {
        Faction faction = displayedObject.ship.faction;
        playerUI.ShowFactionUI(uiBattleManager.factionUIs[faction]);
    }
}
