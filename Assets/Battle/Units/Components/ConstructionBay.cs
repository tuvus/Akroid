using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static Ship;

public class ConstructionBay : ModuleComponent {
    ConstructionBayScriptableObject constructionBayScriptableObject;
    private Shipyard shipyard;

    private float constructionTime;

    [SerializeField]
    public List<ShipConstructionBlueprint> buildQueue;

    public override void SetupComponent(Module module, Faction faction, ComponentScriptableObject componentScriptableObject) {
        base.SetupComponent(module, faction, componentScriptableObject);
        constructionBayScriptableObject = (ConstructionBayScriptableObject)componentScriptableObject;
    }

    public void SetupConstructionBay(Shipyard fleetCommand) {
        this.shipyard = fleetCommand;
        buildQueue = new List<ShipConstructionBlueprint>(10);
    }

    public bool AddConstructionToQueue(ShipConstructionBlueprint shipBlueprint) {
        if (shipBlueprint.GetFaction().TransferCredits(shipBlueprint.cost, shipyard.faction)) {
            shipyard.faction.UseCredits(shipBlueprint.cost);
            buildQueue.Add(shipBlueprint);
            return true;
        }
        return false;
    }

    public void AddConstructionToBeginningQueue(ShipConstructionBlueprint shipBlueprint) {
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
        long buildAmount = constructionBayScriptableObject.constructionAmount * amountMultiplier;
        if (buildAmount <= 0) return;
        Dictionary<CargoBay.CargoTypes, long> cargoReserved = new();

        foreach (var shipBlueprint in buildQueue.ToList()) {
            if (availableConstructionBays == 0) return;
            if (shipBlueprint.IsFinished()) continue;
            availableConstructionBays--;

            // We need to copy the ResourceCosts Dictionary so that we can concurrently remove entries
            foreach (var resourceCost in shipBlueprint.resourceCosts.ToList()) {
                long availableCargo = math.max(0, shipyard.GetAllCargoOfType(resourceCost.Key) - cargoReserved.GetValueOrDefault(resourceCost.Key, 0));
                long amountToUse = math.min(availableCargo, math.min(buildAmount, resourceCost.Value));
                shipBlueprint.resourceCosts[resourceCost.Key] -= amountToUse;
                shipyard.GetCargoBay().UseCargo(amountToUse, resourceCost.Key);

                if (shipBlueprint.resourceCosts[resourceCost.Key] <= 0) {
                    shipBlueprint.resourceCosts.Remove(resourceCost.Key);
                    if (shipBlueprint.IsFinished() && BuildBlueprint(shipBlueprint)) {
                        buildQueue.Remove(shipBlueprint);
                        break;
                    }
                }
            }
            AddReservedResources(shipBlueprint, cargoReserved);
        }
    }

    bool BuildBlueprint(ShipConstructionBlueprint shipBlueprint) {
        Ship ship = shipyard.BuildShip(shipBlueprint.faction, shipBlueprint.shipScriptableObject, shipBlueprint.cost);
        if (ship == null)
            return false;
        shipyard.stationAI.OnShipBuilt(ship);
        return true;
    }

    public long GetCreditCostOfShip(Faction faction, ShipScriptableObject ship) {
        if (faction == this.faction) {
            return ship.cost;
        } else if (faction != null) {
            // Other factions need to pay us for the metal
            return ship.cost + (long)(ship.resourceCosts[ship.resourceTypes.IndexOf(CargoBay.CargoTypes.Metal)] * faction.GetFactionAI().GetSellCostOfMetal());
        } else {
            return ship.cost;
        }
    }

    public Dictionary<CargoBay.CargoTypes, long> GetReservedResources() {
        Dictionary<CargoBay.CargoTypes, long> reservedResources = new();
        buildQueue.ForEach((blueprint) => AddReservedResources(blueprint, reservedResources));
        return reservedResources;
    }

    /// <summary> Adds the resources to reserve from constructionBlueprint to the reservedResources Dictionary passed in. </summary>
    private void AddReservedResources(ShipConstructionBlueprint constructionBlueprint, Dictionary<CargoBay.CargoTypes, long> reservedResources) {
        foreach (var cost in constructionBlueprint.resourceCosts) {
            if (reservedResources.ContainsKey(cost.Key)) {
                reservedResources[cost.Key] = reservedResources[cost.Key] + cost.Value;
            } else {
                reservedResources.Add(cost.Key, cost.Value);
            }
        }
    }

    public int GetNumberOfShipsOfClass(ShipClass shipClass) {
        return buildQueue.Count(q => q.shipScriptableObject.shipClass == shipClass);
    }

    public int GetNumberOfShipsOfClassFaction(ShipClass shipClass, Faction faction) {
        return buildQueue.Count(q => q.shipScriptableObject.shipClass == shipClass && q.faction == faction);
    }

    public int GetNumberOfShipsOfType(ShipType shipType) {
        return buildQueue.Count(q => q.shipScriptableObject.shipType == shipType);
    }

    public int GetNumberOfShipsOfTypeFaction(ShipType shipType, Faction faction) {
        return buildQueue.Count(q => q.shipScriptableObject.shipType == shipType && q.faction == faction);
    }

    public bool HasOpenBays() {
        return constructionBayScriptableObject.constructionBays > buildQueue.Count;
    }

    public int GetConstructionBays() {
        return constructionBayScriptableObject.constructionBays;
    }
}