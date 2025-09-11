using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectHangarUI : PlayerObjectUIMenu {
    [SerializeField] private GameObject shipHangerButtonPrefab;
    [SerializeField] private TMP_Text hangarStatus;
    [SerializeField] private Transform hangarList;
    public Unit unit { get; private set; }
    [SerializeField] private readonly List<Ship> shipsInHangars = new List<Ship>();

    public override void SetDisplayedObject(ObjectUI objectUI) {
        base.SetDisplayedObject(objectUI);
        if (objectUI != null && objectUI.iObject is Unit testUnit && testUnit.moduleSystem.Get<Hangar>().Any())
            unit = testUnit;
        else unit = null;
    }

    public override bool ShouldShowMenu() {
        return unit != null && localPlayer.GetRelationToFaction(unit.faction) != LocalPlayer.RelationType.Enemy;
    }

    public override void UpdateMenu() {
        shipsInHangars.Clear();
        unit.moduleSystem.Get<Hangar>().SelectMany(h => h.ships).ToList().ForEach(s => shipsInHangars.Add(s));
        hangarStatus.text = "Hangar capacity " + shipsInHangars.Count + "/" +
            unit.moduleSystem.Get<Hangar>().Sum(h => h.GetMaxDockSpace());

        for (int i = 0; i < shipsInHangars.Count; i++) {
            if (hangarList.childCount <= i) {
                Instantiate(shipHangerButtonPrefab, hangarList);
            }

            Transform hangarBayButtonTransform = hangarList.GetChild(i);
            Button hangarBayButton = hangarBayButtonTransform.GetComponent<Button>();
            hangarBayButton.onClick.RemoveAllListeners();
            hangarBayButtonTransform.GetChild(3).GetComponent<Button>().onClick.RemoveAllListeners();
            Ship ship = shipsInHangars[i];
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

        for (int i = shipsInHangars.Count; i < hangarList.childCount; i++) {
            hangarList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void HangarButtonPressed(int index) {
        if (localPlayer.GetLocalPlayerInput() is LocalPlayerSelectionInput) {
            LocalPlayerSelectionInput localPlayerSelection =
                (LocalPlayerSelectionInput)localPlayer.GetLocalPlayerInput();

            if (localPlayerSelection.AdditiveButtonPressed) {
                localPlayerSelection.ToggleSelectedUnit(uiBattleManager.units[shipsInHangars[index]]);
            } else {
                localPlayerSelection.SelectBattleObjects(uiBattleManager.units[shipsInHangars[index]]);
            }

            UpdateMenu();
        }
    }

    public void HangarInfoButtonPressed(int index) {
        localPlayer.GetPlayerUI().CloseAllMenus();
        localPlayer.GetPlayerUI().SetDisplayedObject(uiBattleManager.units[shipsInHangars[index]]);
    }

    public Button GetButtonOfShip(Ship ship) {
        return hangarList.GetChild(shipsInHangars.IndexOf(ship)).GetComponent<Button>();
    }
}
