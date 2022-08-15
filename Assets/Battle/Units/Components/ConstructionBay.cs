using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionBay : MonoBehaviour {
    private Shipyard shipyard;

    public float constructionSpeed;
    public float constructionAmmount;
    public int constructionBays;

    private float constructionTime;

    [SerializeField]
    public List<Ship.ShipBlueprint> buildQueue;

    public void SetupConstructionBay(Shipyard fleetCommand) {
        this.shipyard = fleetCommand;
        buildQueue = new List<Ship.ShipBlueprint>(10);
    }

    public void AddConstructionToQueue(Ship.ShipBlueprint shipBlueprint) {
        buildQueue.Add(shipBlueprint);
    }

    public void AddConstructionToBeginningQueue(Ship.ShipBlueprint shipBlueprint) {
        buildQueue.Insert(0, shipBlueprint);
    }

    public void UpdateConstructionBay(float deltaTime) {
        constructionTime -= deltaTime;
        if (constructionTime <= 0) {
            int ammountMultiplier = (int)(Mathf.Abs(constructionTime) / constructionSpeed) + 1;
            constructionTime += constructionSpeed * ammountMultiplier;
            UpdateConstruction(ammountMultiplier);
        }
    }

    void UpdateConstruction(int ammountMultiplier) {
        int availableConstructionBays = constructionBays;
        for (int i = 0; i < buildQueue.Count; i++) {
            if (availableConstructionBays == 0)
                return;
            Ship.ShipBlueprint shipBlueprint = buildQueue[i];
            if (!shipBlueprint.IsFinished()) {
                availableConstructionBays--;
                float buildAmmount = constructionAmmount * ammountMultiplier;
                for (int f = 0; f < shipBlueprint.resources.Count; f++) {
                    if (buildAmmount <= 0)
                        break;
                    float ammountToUse = Mathf.Min(shipyard.GetAllCargo(shipBlueprint.resourcesTypes[f]), Mathf.Min(buildAmmount, shipBlueprint.resources[f]));
                    shipBlueprint.resources[f] -= ammountToUse;
                    shipyard.UseCargo(ammountToUse, shipBlueprint.resourcesTypes[f]);
                    if (shipBlueprint.resources[f] <= 0) {
                        shipBlueprint.resources.RemoveAt(f);
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

    bool BuildBlueprint(Ship.ShipBlueprint shipBlueprint) {
        Ship ship = shipyard.BuildShip(shipBlueprint.shipClass, shipBlueprint.shipCost);
        if (ship == null)
            return false;
        shipyard.stationAI.OnShipBuilt(ship);
        return true;
    }

    public int GetNumberOfShipsOfClass(Ship.ShipClass shipClass) {
        int count = 0;
        for (int i = 0; i < buildQueue.Count; i++) {
            if (buildQueue[i].shipClass == shipClass) {
                count++;
            }
        }
        return count;
    }

    public bool HasOpenBays() {
        return constructionBays > buildQueue.Count;
    }
}