using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UnitUIMenu : MonoBehaviour {
    private UnitUI unitUI;

    protected LocalPlayer localPlayer;
    protected PlayerUI playerUI;
    protected UIBattleManager uiBattleManager;

    [SerializeField] private TMP_Text factionText;
    [SerializeField] private TMP_Text unitClassText;
    [SerializeField] private TMP_Text strengthText;
    [SerializeField] private TMP_Text weaponsText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private GameObject shipPanel;
    [SerializeField] private TMP_Text fleetText;
    [SerializeField] private TMP_Text currentCommandText;
    [SerializeField] private TMP_Text currentActionText;
    [SerializeField] private TMP_Text speedText;

    [SerializeField] private GameObject cargoBayObject;
    [SerializeField] private TMP_Text cargoBaysStatus;
    [SerializeField] private TMP_Text cargoBayCapacity;
    [SerializeField] private Transform cargoBayList;
    [SerializeField] private GameObject cargoBayButtonPrefab;

    [SerializeField] private GameObject populationObject;
    [SerializeField] private TMP_Text populationCapacity;
    [SerializeField] private Transform populationList;
    [SerializeField] private GameObject populationButtonPrefab;


    public void SetupUnitUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        this.playerUI = playerUI;
        this.localPlayer = localPlayer;
        this.uiBattleManager = uiManager.uiBattleManager;
    }

    public void SetUnit(UnitUI unitUI) {
        this.unitUI = unitUI;
        shipPanel.SetActive(unitUI.unit.IsShip());
    }

    public void UpdateMenu() {
        Unit unit = unitUI.unit;
        factionText.text = unit.faction.name;
        if (unit is Station station) {
            unitClassText.text = station.GetStationType().ToString();
        } else if (unit is Ship ships) {
            unitClassText.text = ships.GetShipClass().ToString();
        }
        strengthText.text = "Damage: " + NumFormatter.ConvertNumber(unit.GetUnitDamagePerSecond());
        weaponsText.text = "Weapons: " + unit.GetWeaponCount();
        if (unit.GetWeaponCount() > 0) {
            rangeText.gameObject.SetActive(true);
            rangeText.text = "Range: " + NumFormatter.ConvertNumber(unit.GetMaxWeaponRange());
        } else {
            rangeText.gameObject.SetActive(false);
        }

        if (unit is Ship ship) {
            fleetText.text = ship.fleet != null ? ship.fleet.GetFleetName() : "No fleet";
            currentCommandText.text = ship.shipAI.commands.Count > 0 ?
                ship.shipAI.commands.First().commandType.ToString() : "No command";
            currentActionText.text = ship.shipAction.ToString();
            speedText.text = "Speed: " + ship.speed;
        }
        if (localPlayer.GetRelationToUnit(unit) != LocalPlayer.RelationType.Enemy) {
            UpdateCargoBayUI();
            UpdatePopulationUI();
        }
    }

    private void UpdateCargoBayUI() {
        Unit unit = unitUI.unit;
        if (!unit.moduleSystem.Get<CargoBay>().Any()) {
            cargoBayObject.SetActive(false);
            return;
        } else {
            cargoBayObject.SetActive(true);
        }

        var cargoBays = unit.moduleSystem.Get<CargoBay>().ToList();
        long totalCapacity = cargoBays.Sum(cb => cb.GetCargoBayCapacity() * cb.GetMaxCargoBays());
        cargoBaysStatus.text =
            "Cargo bays " + cargoBays.Sum(cb => cb.GetCargoBaysUsed()) + "/" +
            cargoBays.Sum(cb => cb.GetMaxCargoBays());
        cargoBayCapacity.text = "Total capacity " + NumFormatter.ConvertNumber(totalCapacity);

        int cargoBayIndex = 0;

        foreach (CargoBay.CargoType cargoType in CargoBay.allCargoTypes) {
            long totalCargo = cargoBays.Sum(cb => cb.GetAllCargo(cargoType));
            if (totalCargo == 0) continue;

            if (cargoBayList.childCount <= cargoBayIndex) {
                Instantiate(cargoBayButtonPrefab, cargoBayList);
            }

            Transform cargoBayButton = cargoBayList.GetChild(cargoBayIndex);
            cargoBayButton.gameObject.SetActive(true);
            cargoBayButton.GetChild(0).GetComponent<TMP_Text>().text = cargoType.ToString();
            cargoBayButton.GetChild(1).GetComponent<TMP_Text>().text = NumFormatter.ConvertNumber(totalCargo);
            cargoBayIndex++;

        }

        for (int i = cargoBayIndex; i < cargoBayList.childCount; i++) {
            cargoBayList.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void UpdatePopulationUI() {
        Unit unit = unitUI.unit;
        if (!unit.moduleSystem.Get<HabitationArea>().Any()) {
            populationObject.SetActive(false);
            return;
        } else {
            populationObject.SetActive(true);
        }

        var habitationAreas = unit.moduleSystem.Get<HabitationArea>().ToList();

        populationCapacity.text = NumFormatter.ConvertNumber(habitationAreas.Sum(h => h.population.TotalPopulation())) +
            "/" + NumFormatter.ConvertNumber(habitationAreas.Sum(h => h.GetCapacity()));

        int populationIndex = 0;

        foreach (var occupation in HabitationArea.allOccupations) {
            long pop = habitationAreas.Sum(h => h.population.Get(occupation));
            if (pop == 0) continue;

            if (populationList.childCount <= populationIndex) {
                Instantiate(populationButtonPrefab, populationList);
            }

            Transform populationButton = populationList.GetChild(populationIndex);
            populationButton.gameObject.SetActive(true);
            populationButton.GetChild(0).GetComponent<TMP_Text>().text = occupation.ToString();
            populationButton.GetChild(1).GetComponent<TMP_Text>().text = NumFormatter.ConvertNumber(pop);
            populationIndex++;

        }

        for (int i = populationIndex; i < populationList.childCount; i++) {
            populationList.GetChild(i).gameObject.SetActive(false);
        }
    }
}
