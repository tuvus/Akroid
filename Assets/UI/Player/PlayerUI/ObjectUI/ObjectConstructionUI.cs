using System;
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
    private List<Tuple<ConstructionBay, int>> blueprintToConstructionBay = new List<Tuple<ConstructionBay, int>>();

    private Unit unit;

    public override void SetDisplayedObject(ObjectUI objectUI) {
        base.SetDisplayedObject(objectUI);
        if (objectUI != null && objectUI.iObject is Unit testUnit && testUnit.moduleSystem.Get<ConstructionBay>().Any())
            unit = testUnit;
        else unit = null;
    }

    public override bool ShouldShowMenu() {
        return unit != null;
    }

    protected override void UpdateMenu() {
        UpdateConstructionUI(unit.moduleSystem.Get<ConstructionBay>().ToList());
        UpdateShipBlueprintUI();
    }

    public void ShipBlueprintButtonPressed(int index) {
        var blueprint = new Ship.ShipConstructionBlueprint(localPlayer.GetFaction(), shipBlueprints[index]);
        var constructor = unit.moduleSystem.Get<ConstructionBay>().FirstOrDefault(c => c.CanBuildBlueprint(blueprint));
        if (constructor != null) {
            constructor.AddConstructionToQueue(blueprint);
            UpdateConstructionUI(unit.moduleSystem.Get<ConstructionBay>().ToList());
            UpdateShipBlueprintUI();
        }
    }

    private void UpdateConstructionUI(List<ConstructionBay> constructionBays) {
        autoBuildShips.transform.parent.gameObject.SetActive(unit.faction.GetFactionAI() is SimulationFactionAI);
        if (autoBuildShips.gameObject.activeInHierarchy) {
            autoBuildShips.SetIsOnWithoutNotify(((SimulationFactionAI)unit.faction.GetFactionAI()).autoConstruction);
            autoBuildShips.onValueChanged.RemoveAllListeners();
            autoBuildShips.onValueChanged.AddListener(autoConstruction => SetAutoConstruction(autoConstruction));
        }

        constructionBayStatus.text = "Construction bays in use " +
            Mathf.Min(constructionBays.Sum(cb => cb.buildQueue.Count),
                constructionBays.Sum(cb => cb.GetConstructionBays())) + "/" +
            constructionBays.Sum(cb => cb.GetConstructionBays());

        blueprintToConstructionBay.Clear();
        int index = 0;
        foreach (ConstructionBay constructionBay in constructionBays) {
            for (int i = 0; i < constructionBay.buildQueue.Count; i++, index++) {
                if (constructionBayList.childCount <= index) {
                    Instantiate(shipConstructionButtonPrefab, constructionBayList);
                }

                Transform constructionBayButtonTransform = constructionBayList.GetChild(index);
                Button constructionBayButton = constructionBayButtonTransform.GetComponent<Button>();
                constructionBayButton.onClick.RemoveAllListeners();
                int f = index;
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
                blueprintToConstructionBay.Add(new(constructionBay, i));
            }
        }

        for (int i = index; i < constructionBayList.childCount; i++) {
            constructionBayList.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void SetAutoConstruction(bool autoconstruction) {
        ((SimulationFactionAI)unit.faction.GetFactionAI()).autoConstruction = autoconstruction;
    }

    public void ConstructionButtonPressed(int index) {
        ConstructionBay constructionBay = blueprintToConstructionBay[index].Item1;
        index = blueprintToConstructionBay[index].Item2;
        if (localPlayer.GetFaction() != null &&
            constructionBay.buildQueue[index].GetFaction() == localPlayer.GetFaction()) {
            constructionBay.RemoveBlueprintFromQueue(index);
            UpdateConstructionUI(unit.moduleSystem.Get<ConstructionBay>().ToList());
            UpdateShipBlueprintUI();
        }
    }

    private void UpdateShipBlueprintUI() {
        shipBlueprints = uiBattleManager.battleManager.shipBlueprints
            .Where(b => unit.moduleSystem.Get<ConstructionBay>().Any(cb => cb.CanBuildBlueprint(b))).ToList();
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
                cost = unit.moduleSystem.Get<ConstructionBay>().First()
                    .GetCreditCostOfShip(localPlayer.player.faction, blueprint.shipScriptableObject);
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
