using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UnitMenu : PlayerUIMenu<UnitUI> {
    [SerializeField] private GameObject moduleUIPrefab;
    [SerializeField] private Transform displayedImageTransform;
    [SerializeField] private ObjectConstructionUI constructionUI;
    [field:SerializeField] public ObjectHangarUI hangarUI { get; private set; }
    [SerializeField] private ObjectSystemUI systemUI;

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

    private List<GameObject> moduleUIs;
    private ModuleSystem.System selectedSystem;

    public override void SetupPlayerUIMenu(PlayerUI playerUI, LocalPlayer localPlayer, UIManager uiManager) {
        base.SetupPlayerUIMenu(playerUI, localPlayer, uiManager);
        moduleUIs = new List<GameObject>();
        constructionUI.SetupPlayerObjectUIMenu(playerUI, localPlayer, uiManager);
        hangarUI.SetupPlayerObjectUIMenu(playerUI, localPlayer, uiManager);
        systemUI.SetupPlayerObjectUIMenu(playerUI, localPlayer, uiManager);
        systemUI.gameObject.SetActive(false);
    }

    public override void SetDisplayedObject(ObjectUI objectUI) {
        base.SetDisplayedObject(objectUI);
        constructionUI.SetDisplayedObject(displayedObject);
        hangarUI.SetDisplayedObject(displayedObject);
        systemUI.SetDisplayedObject(displayedObject);
        shipPanel.SetActive(displayedObject.unit.IsShip());
        DeselectSystem();
    }

    protected override bool ShouldShowLeftPanel() {
        return constructionUI.ShouldShowMenu();
    }

    protected override bool ShouldShowRightPanel() {
        return selectedSystem == null ? hangarUI.ShouldShowMenu() : systemUI.ShouldShowMenu();
    }

    protected override void RefreshLeftPanel() {
        constructionUI.UpdateMenu();
    }

    protected override void RefreshRightPanel() {
        if (selectedSystem == null)
            hangarUI.UpdateMenu();
        else systemUI.UpdateMenu();
    }

    public override void RefreshMenu() {
        base.RefreshMenu();
        UpdateModules(displayedObject.unit);
    }

    private void UpdateModules(Unit unit) {
        ModuleSystem moduleSystem = unit.moduleSystem;
        for (int i = 0; i < moduleSystem.modules.Count; i++) {
            ModuleComponent module = moduleSystem.modules[i];
            if (moduleUIs.Count <= i) {
                moduleUIs.Add(Instantiate(moduleUIPrefab, displayedImageTransform));
                int moduleIndex = i;
                moduleUIs[i].GetComponent<Button>().onClick.AddListener(() => OnModuleButtonPress(moduleIndex));
            }
            moduleUIs[i].SetActive(true);
            moduleUIs[i].GetComponent<RectTransform>().anchoredPosition = module.GetPosition() *
                displayedImageTransform.GetComponent<RectTransform>().rect.size * unit.scale * 42 /
                (100 * unit.GetSpriteSize());
            moduleUIs[i].transform.GetChild(0).eulerAngles = new Vector3(0, 0, module.rotation);
            if (module.componentScriptableObject.sprite != null) {
                moduleUIs[i].transform.GetChild(0).GetComponent<Image>().sprite =
                    module.componentScriptableObject.sprite;
                moduleUIs[i].transform.GetChild(0).gameObject.SetActive(true);
            } else moduleUIs[i].transform.GetChild(0).gameObject.SetActive(false);

            if (selectedSystem != null && selectedSystem == moduleSystem.moduleToSystem[moduleSystem.modules[i]]) {
                moduleUIs[i].GetComponent<Image>().color = Color.white;
            } else {
                moduleUIs[i].GetComponent<Image>().color = Color.grey;
            }
        }
        for (int i = moduleSystem.modules.Count; i < moduleUIs.Count; i++) {
            moduleUIs[i].SetActive(false);
        }
    }

    private void OnModuleButtonPress(int moduleIndex) {
        ModuleSystem moduleSystem = ((Unit)displayedObject.iObject).moduleSystem;
        if (selectedSystem == moduleSystem.moduleToSystem[moduleSystem.modules[moduleIndex]]) {
            DeselectSystem();
        } else {
            selectedSystem = moduleSystem.moduleToSystem[moduleSystem.modules[moduleIndex]];
            rightPanel = systemUI.gameObject;
            systemUI.SelectSystem(selectedSystem);
            systemUI.UpdateMenu();
            hangarUI.gameObject.SetActive(false);
        }
    }

    protected override void RefreshStatusPanel() {
        Unit unit = displayedObject.unit;
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
        Unit unit = displayedObject.unit;
        if (!unit.moduleSystem.Get<CargoBay>().Any()) {
            cargoBayObject.SetActive(false);
            return;
        }
        cargoBayObject.SetActive(true);

        List<CargoBay> cargoBays = unit.moduleSystem.Get<CargoBay>().ToList();
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
        Unit unit = displayedObject.unit;
        if (!unit.moduleSystem.Get<HabitationArea>().Any()) {
            populationObject.SetActive(false);
            return;
        }
        populationObject.SetActive(true);

        List<HabitationArea> habitationAreas = unit.moduleSystem.Get<HabitationArea>().ToList();

        populationCapacity.text = NumFormatter.ConvertNumber(habitationAreas.Sum(h => h.population.TotalPopulation())) +
            "/" + NumFormatter.ConvertNumber(habitationAreas.Sum(h => h.GetCapacity()));

        int populationIndex = 0;

        foreach (Occupation occupation in HabitationArea.allOccupations) {
            long pop = habitationAreas.Sum(h => h.population.Get(occupation));
            if (pop == 0) continue;

            if (populationList.childCount <= populationIndex) {
                Instantiate(populationButtonPrefab, populationList);
            }

            Transform populationButton = populationList.GetChild(populationIndex);
            populationButton.gameObject.SetActive(true);
            populationButton.GetChild(0).GetComponent<TMP_Text>().text = occupation + "s";
            populationButton.GetChild(1).GetComponent<TMP_Text>().text = NumFormatter.ConvertNumber(pop);
            populationIndex++;

        }

        for (int i = populationIndex; i < populationList.childCount; i++) {
            populationList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void DeselectSystem() {
        selectedSystem = null;
        rightPanel = hangarUI.gameObject;
        systemUI.gameObject.SetActive(false);
    }
}
