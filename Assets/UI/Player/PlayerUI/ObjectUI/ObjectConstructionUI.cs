using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectConstructionUI : PlayerObjectUIMenu {
    [SerializeField] private GameObject shipConstructionButtonPrefab;
    [SerializeField] private Toggle autoBuildShips;
    [SerializeField] private GameObject shipBlueprintButtonPrefab;
    [SerializeField] private Transform blueprintList;
    [SerializeField] private TMP_Text constructionBayStatus;
    [SerializeField] private Transform constructionBayList;
    private List<Ship.ShipBlueprint> shipBlueprints = new List<Ship.ShipBlueprint>();

    private Unit unit;


    public override void SetDisplayedObject(ObjectUI objectUI) {
        if (objectUI != null && objectUI.iObject is Unit testUnit && testUnit.moduleSystem.Get<ConstructionBay>().Any())
            unit = testUnit;
    }

    public override bool ShouldShowMenu() {
        return unit != null;
    }

    protected override void UpdateMenu() {
        UpdateConstructionUI(unit.moduleSystem.Get<ConstructionBay>().First());
        UpdateShipBlueprintUI();
    }

    public void ShipBlueprintButtonPressed(int index) {
        if (unit.moduleSystem.Get<ConstructionBay>().First().AddConstructionToQueue(
            new Ship.ShipConstructionBlueprint(localPlayer.GetFaction(), shipBlueprints[index]))) {
            UpdateConstructionUI(unit.moduleSystem.Get<ConstructionBay>().First());
            UpdateShipBlueprintUI();
        }
    }

    private void UpdateConstructionUI(ConstructionBay constructionBay) {
        autoBuildShips.transform.parent.gameObject.SetActive(
            unit.faction.GetFactionAI() is SimulationFactionAI);
        if (autoBuildShips.gameObject.activeInHierarchy) {
            autoBuildShips.SetIsOnWithoutNotify(((SimulationFactionAI)unit.faction.GetFactionAI())
                .autoConstruction);
            autoBuildShips.onValueChanged.RemoveAllListeners();
            autoBuildShips.onValueChanged.AddListener(autoConstruction => SetAutoConstruction(autoConstruction));
        }

        constructionBayStatus.text = "Construction bays in use " +
            Mathf.Min(constructionBay.buildQueue.Count, constructionBay.GetConstructionBays()) + "/" +
            constructionBay.GetConstructionBays();
        for (int i = 0; i < constructionBay.buildQueue.Count; i++) {
            if (constructionBayList.childCount <= i) {
                Instantiate(shipConstructionButtonPrefab, constructionBayList);
            }

            Transform constructionBayButtonTransform = constructionBayList.GetChild(i);
            Button constructionBayButton = constructionBayButtonTransform.GetComponent<Button>();
            constructionBayButton.onClick.RemoveAllListeners();
            int f = i;
            constructionBayButton.onClick.AddListener(() => ConstructionButtonPressed(f));
            constructionBayButtonTransform.gameObject.SetActive(true);
            Ship.ShipConstructionBlueprint blueprint = constructionBay.buildQueue[i];
            constructionBayButtonTransform.GetChild(0).GetComponent<TMP_Text>().text = blueprint.name;
            constructionBayButtonTransform.GetChild(1).GetComponent<TMP_Text>().text =
                blueprint.faction.abbreviatedName;
            constructionBayButtonTransform.GetChild(2).GetComponent<TMP_Text>().text =
                (100 - blueprint.GetTotalResourcesLeftToUse() * 100 / blueprint.totalResourcesRequired) + "%";
            if (uiManager.GetFactionColoringShown()) {
                constructionBayButton.GetComponent<Image>().color = blueprint.faction.GetColorTint();
            } else {
                constructionBayButton.GetComponent<Image>().color =
                    localPlayer.GetColorOfRelationType(localPlayer.GetRelationToFaction(blueprint.GetFaction()));
            }
        }

        for (int i = constructionBay.buildQueue.Count; i < constructionBayList.childCount; i++) {
            constructionBayList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void SetAutoConstruction(bool autoconstruction) {
        ((SimulationFactionAI)unit.faction.GetFactionAI()).autoConstruction = autoconstruction;
    }

    public void ConstructionButtonPressed(int index) {
        ConstructionBay constructionBay = unit.moduleSystem.Get<ConstructionBay>().First();
        if (localPlayer.GetFaction() != null &&
            constructionBay.buildQueue[index].GetFaction() == localPlayer.GetFaction()) {
            constructionBay.RemoveBlueprintFromQueue(index);
            UpdateConstructionUI(constructionBay);
            UpdateShipBlueprintUI();
        }
    }

    private void UpdateShipBlueprintUI() {
        ConstructionBay constructionBay = unit.moduleSystem.Get<ConstructionBay>().First();
        shipBlueprints = uiBattleManager.battleManager.shipBlueprints.Where(b => constructionBay.CanBuildBlueprint(b))
            .ToList();
        for (int i = 0; i < shipBlueprints.Count; i++) {
            if (blueprintList.childCount <= i) {
                Instantiate(shipBlueprintButtonPrefab, blueprintList);
            }

            Transform cargoBayButton = blueprintList.GetChild(i);
            Ship.ShipBlueprint blueprint = shipBlueprints[i];
            Button button = cargoBayButton.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            int f = i;
            button.onClick.AddListener(() => ShipBlueprintButtonPressed(f));
            cargoBayButton.gameObject.SetActive(true);
            cargoBayButton.GetChild(0).GetComponent<TMP_Text>().text = blueprint.name;
            long cost;
            if (localPlayer.GetFaction() != null) {
                cost = constructionBay.GetCreditCostOfShip(localPlayer.player.faction, blueprint.shipScriptableObject);
                button.interactable = localPlayer.GetFaction().credits >= cost;
            } else {
                cost = blueprint.shipScriptableObject.cost;
                button.interactable = false;
            }

            cargoBayButton.GetChild(1).GetComponent<TMP_Text>().text = "Cost: " + NumFormatter.ConvertNumber(cost);
        }

        for (int i = shipBlueprints.Count; i < blueprintList.childCount; i++) {
            blueprintList.GetChild(i).gameObject.SetActive(false);
        }
    }
}
