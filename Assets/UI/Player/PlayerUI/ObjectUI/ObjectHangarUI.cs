using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectHangarUI : PlayerObjectUIMenu {
    [SerializeField] private GameObject shipHangerButtonPrefab;
    [SerializeField] private TMP_Text hangarStatus;
    [SerializeField] private Transform hangarList;
    private Unit unit;
    [SerializeField] private readonly List<Ship> shipsInHangar = new List<Ship>();

    public override void SetDisplayedObject(ObjectUI objectUI) {
        base.SetDisplayedObject(objectUI);
        if (objectUI != null && objectUI.iObject is Unit testUnit && testUnit.moduleSystem.Get<Hangar>().Any())
            unit = testUnit;
        else unit = null;
    }

    public override bool ShouldShowMenu() {
        return unit != null;
    }

    protected override void UpdateMenu() {
        Hangar hangar = unit.moduleSystem.Get<Hangar>().First();
        shipsInHangar.Clear();

        for (int i = 0; i < hangar.ships.Count; i++) {
            shipsInHangar.Add(hangar.ships[i]);
        }

        hangarStatus.text = "Hangar capacity " + shipsInHangar.Count + "/" + hangar.GetMaxDockSpace();
        for (int i = 0; i < shipsInHangar.Count; i++) {
            if (hangarList.childCount <= i) {
                Instantiate(shipHangerButtonPrefab, hangarList);
            }

            Transform hangarBayButtonTransform = hangarList.GetChild(i);
            Button hangarBayButton = hangarBayButtonTransform.GetComponent<Button>();
            hangarBayButton.onClick.RemoveAllListeners();
            hangarBayButtonTransform.GetChild(3).GetComponent<Button>().onClick.RemoveAllListeners();
            Ship ship = shipsInHangar[i];
            ShipUI shipUI = (ShipUI)uiBattleManager.units[ship];
            int f = i;

            hangarBayButton.onClick.AddListener(() => HangarButtonPressed(f));
            hangarBayButtonTransform.gameObject.SetActive(true);
            hangarBayButtonTransform.GetChild(0).GetComponent<TMP_Text>().text = ship.GetUnitName();
            hangarBayButtonTransform.GetChild(1).GetComponent<TMP_Text>().text = ship.faction.abbreviatedName;
            hangarBayButtonTransform.GetChild(2).GetComponent<TMP_Text>().text =
                (ship.GetHealth() * 100 / ship.GetMaxHealth()) + "%";
            hangarBayButtonTransform.GetChild(3).GetComponent<Button>().onClick
                .AddListener(() => HangarInfoButtonPressed(f));
            if (uiManager.GetFactionColoringShown()) {
                hangarBayButton.GetComponent<Image>().color =
                    ship.faction.GetColorBackgroundTint(shipUI.unitIconUI.GetColor().a);
            } else {
                hangarBayButton.GetComponent<Image>().color = shipUI.unitIconUI.GetColor();
            }
        }

        for (int i = shipsInHangar.Count; i < hangarList.childCount; i++) {
            hangarList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void HangarButtonPressed(int index) {
        if (localPlayer.GetLocalPlayerInput() is LocalPlayerSelectionInput) {
            LocalPlayerSelectionInput localPlayerSelection =
                (LocalPlayerSelectionInput)localPlayer.GetLocalPlayerInput();

            if (localPlayerSelection.AdditiveButtonPressed) {
                localPlayerSelection.ToggleSelectedUnit(uiBattleManager.units[shipsInHangar[index]]);
            } else {
                localPlayerSelection.SelectBattleObjects(uiBattleManager.units[shipsInHangar[index]]);
            }

            UpdateMenu();
        }
    }

    public void HangarInfoButtonPressed(int index) {
        localPlayer.GetPlayerUI().CloseAllMenus();
        localPlayer.GetPlayerUI().SetDisplayedObject(uiBattleManager.units[shipsInHangar[index]]);
    }

    public Button GetButtonOfShip(Ship ship) {
        return hangarList.GetChild(shipsInHangar.IndexOf(ship)).GetComponent<Button>();
    }
}
