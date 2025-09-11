using System.IO.IsolatedStorage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectSystemUI : PlayerObjectUIMenu {
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text componentName;
    [SerializeField] private TMP_Text systemType;
    [SerializeField] private TMP_Text moduleCount;
    [SerializeField] private TMP_Text maxComponentSize;
    private Unit unit;
    private ModuleSystem.System displayedSystem;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeName;
    [SerializeField] private TMP_Text upgradeCost;
    [SerializeField] private Transform componentListTransform;
    [SerializeField] private GameObject componentButtonPrefab;

    public override void SetDisplayedObject(ObjectUI objectUI) {
        base.SetDisplayedObject(objectUI);
        if (objectUI.iObject is Unit newUnit) unit = newUnit;
    }

    public void SelectSystem(ModuleSystem.System displayedSystem) {
        this.displayedSystem = displayedSystem;
    }

    public override bool ShouldShowMenu() {
        return unit != null && displayedSystem != null && localPlayer.GetRelationToFaction(unit.faction) != LocalPlayer.RelationType.Enemy;
    }

    public override void UpdateMenu() {
        bool isOwned = localPlayer.GetRelationToUnit(unit) == LocalPlayer.RelationType.Owned;
        title.text = displayedSystem.name;
        componentName.text = displayedSystem.component.name;
        systemType.text = "System type: " + displayedSystem.type;
        moduleCount.text = "Module count: " + displayedSystem.moduleCount;
        maxComponentSize.text = "Max component size: " + displayedSystem.moduleSize;

        if (displayedSystem.component.upgrade != null && isOwned) {
            upgradeButton.transform.parent.gameObject.SetActive(true);
            upgradeName.text = displayedSystem.component.upgrade.name;
            upgradeCost.text = "Cost: " + NumFormatter.ConvertNumber((displayedSystem.component.upgrade.cost -
                displayedSystem.component.cost) * displayedSystem.moduleCount);
            upgradeButton.interactable =
                unit.moduleSystem.CanUpgradeSystem(displayedSystem, unit);
            if (!upgradeButton.interactable && unit is Ship ship && ship.dockedStation != null) {
                upgradeButton.interactable = unit.moduleSystem.CanUpgradeSystem(displayedSystem, ship.dockedStation);
            }
        } else {
            upgradeButton.transform.parent.gameObject.SetActive(false);
        }

        int index = 0;
        if (isOwned) {
            foreach (var componentScriptableObject in uiBattleManager.battleManager.components) {
                if (!unit.moduleSystem.IsComponentCompatibleOnSystem(displayedSystem, componentScriptableObject)
                    || componentScriptableObject == displayedSystem.component)
                    continue;

                if (index == componentListTransform.childCount)
                    Instantiate(componentButtonPrefab, componentListTransform);

                Button component = componentListTransform.GetChild(index).GetComponent<Button>();
                component.transform.GetChild(0).GetComponent<TMP_Text>().text =
                    componentScriptableObject.name == "Empty" ? "Remove" : componentScriptableObject.name;
                component.transform.GetChild(1).GetComponent<TMP_Text>().text =
                    "Cost: " + NumFormatter.ConvertNumber(componentScriptableObject.cost * displayedSystem.moduleCount);
                component.gameObject.SetActive(true);
                component.onClick.RemoveAllListeners();
                component.onClick.AddListener(() => {
                    unit.moduleSystem.ReplaceSystem(displayedSystem, componentScriptableObject,
                        unit.moduleSystem.CanReplaceSystem(displayedSystem, componentScriptableObject, unit)
                            ? unit : ((Ship)unit).dockedStation);
                    UpdateMenu();
                });

                component.interactable =
                    unit.moduleSystem.CanReplaceSystem(displayedSystem, componentScriptableObject, unit);
                if (!component.interactable && unit is Ship ship && ship.dockedStation != null)
                    component.interactable =
                        unit.moduleSystem.CanReplaceSystem(displayedSystem, componentScriptableObject,
                            ship.dockedStation);

                index++;
            }
        }

        for (int i = index; i < componentListTransform.childCount; i++) {
            componentListTransform.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void UpgradeComponent() {
        if (!upgradeButton.gameObject.activeSelf)
            Debug.LogError("Trying to upgrade a system that doesn't have an upgrade!");
        if (unit.moduleSystem.CanUpgradeSystem(unit.moduleSystem.systems.IndexOf(displayedSystem), unit)) {
            unit.moduleSystem.UpgradeSystem(unit.moduleSystem.systems.IndexOf(displayedSystem), unit);
        } else if (unit is Ship ship && ship.dockedStation != null) {
            unit.moduleSystem.UpgradeSystem(unit.moduleSystem.systems.IndexOf(displayedSystem), ship.dockedStation);
        } else {
            Debug.LogError("Trying to upgrade a system that isn't allowed to be upgraded!");
        }
        UpdateMenu();
    }
}
