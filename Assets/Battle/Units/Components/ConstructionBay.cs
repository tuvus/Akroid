using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Ship;

public class ConstructionBay : ModuleComponent {
    ConstructionBayScriptableObject constructionBayScriptableObject;
    private Shipyard shipyard;

    private float constructionTime;

    [SerializeField]
    public List<Ship.ShipConstructionBlueprint> buildQueue;

    public override void SetupComponent(Module module, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, componentScriptableObject);
        constructionBayScriptableObject = (ConstructionBayScriptableObject)componentScriptableObject;
    }

    public void SetupConstructionBay(Shipyard fleetCommand) {
        this.shipyard = fleetCommand;
        buildQueue = new List<Ship.ShipConstructionBlueprint>(10);
    }

    public bool AddConstructionToQueue(Ship.ShipConstructionBlueprint shipBlueprint) {
        if (shipBlueprint.GetFaction().TransferCredits(shipBlueprint.cost, shipyard.faction)) {
                shipyard.faction.UseCredits(shipBlueprint.cost);
            buildQueue.Add(shipBlueprint);
            return true;
        }
        return false;
    }

    public void AddConstructionToBeginningQueue(Ship.ShipConstructionBlueprint shipBlueprint) {
        buildQueue.Insert(0, shipBlueprint);
    }

    public void RemoveBlueprintFromQueue(int index) {
        ShipConstructionBlueprint shipBlueprint = buildQueue[index];
        shipyard.faction.AddCredits(shipBlueprint.cost);
        shipyard.faction.TransferCredits(shipBlueprint.cost, shipBlueprint.GetFaction());
        buildQueue.RemoveAt(index);
    }

    public void UpdateConstructionBay(float deltaTime) {
        constructionTime -= deltaTime;
        if (constructionTime <= 0) {
            int amountMultiplier = (int)(Mathf.Abs(constructionTime) / constructionBayScriptableObject.constructionSpeed) + 1;
            constructionTime += constructionBayScriptableObject.constructionSpeed * amountMultiplier;
            UpdateConstruction(amountMultiplier);
        }
    }

    void UpdateConstruction(int amountMultiplier) {
        int availableConstructionBays = constructionBayScriptableObject.constructionBays;
        for (int i = 0; i < buildQueue.Count; i++) {
            if (availableConstructionBays == 0)
                return;
            Ship.ShipConstructionBlueprint shipBlueprint = buildQueue[i];
            if (!shipBlueprint.IsFinished()) {
                availableConstructionBays--;
                long buildAmount = constructionBayScriptableObject.constructionAmount * amountMultiplier;
                for (int f = 0; f < shipBlueprint.resourceCosts.Count; f++) {
                    if (buildAmount <= 0)
                        break;
                    long amountToUse = Unity.Mathematics.math.min(shipyard.GetAllCargo(shipBlueprint.resourcesTypes[f]), Unity.Mathematics.math.min(buildAmount, shipBlueprint.resourceCosts[f]));
                    shipBlueprint.resourceCosts[f] -= amountToUse;
                    shipyard.GetCargoBay().UseCargo(amountToUse, shipBlueprint.resourcesTypes[f]);
                    if (shipBlueprint.resourceCosts[f] <= 0) {
                        shipBlueprint.resourceCosts.RemoveAt(f);
                        shipBlueprint.resourcesTypes.RemoveAt(f);
                        f--;
                        if (shipBlueprint.IsFinished() && BuildBlueprint(shipBlueprint)) {
                            buildQueue.Remove(shipBlueprint);
                            i--;
                            break;
                        }
                    }
                }
            }
        }
    }

    bool BuildBlueprint(Ship.ShipConstructionBlueprint shipBlueprint) {
        Ship ship = shipyard.BuildShip(shipBlueprint.factionIndex, shipBlueprint.shipScriptableObject.shipClass, shipBlueprint.cost);
        if (ship == null)
            return false;
        shipyard.stationAI.OnShipBuilt(ship);
        return true;
    }

    public int GetNumberOfShipsOfClass(Ship.ShipClass shipClass) {
        int count = 0;
        for (int i = 0; i < buildQueue.Count; i++) {
            if (buildQueue[i].shipScriptableObject.shipClass == shipClass) {
                count++;
            }
        }
        return count;
    }

    public int GetNumberOfShipsOfClassFaction(Ship.ShipClass shipClass, int factionIndex) {
        int count = 0;
        for (int i = 0; i < buildQueue.Count; i++) {
            if (buildQueue[i].shipScriptableObject.shipClass == shipClass && buildQueue[i].factionIndex == factionIndex) {
                count++;
            }
        }
        return count;
    }

    public bool HasOpenBays() {
        return constructionBayScriptableObject.constructionBays > buildQueue.Count;
    }

    public int GetConstructionBays() {
        return constructionBayScriptableObject.constructionBays;
    }
}