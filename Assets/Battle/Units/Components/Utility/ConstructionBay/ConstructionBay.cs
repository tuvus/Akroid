using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static Ship;

public class ConstructionBay : ModuleComponent {
    [SerializeField] public List<ShipConstructionBlueprint> buildQueue;
    private ConstructionBayScriptableObject constructionBayScriptableObject;

    private float constructionTime;

    public ConstructionBay(BattleManager battleManager, IModule module, Unit unit,
        ComponentScriptableObject componentScriptableObject) :
        base(battleManager, module, unit, componentScriptableObject) {
        constructionBayScriptableObject = (ConstructionBayScriptableObject)componentScriptableObject;

        if (unit.IsStation()) {
            ((Station)unit).ReserveCargo(1200 * 12, CargoBay.CargoType.Metal);
            ((Station)unit).ReserveCargo(1200 * 8, CargoBay.CargoType.Gas);
        }
        buildQueue = new List<ShipConstructionBlueprint>(10);
    }

    public override void Upgrade(ComponentScriptableObject componentScriptableObject) {
        base.Upgrade(componentScriptableObject);
        constructionBayScriptableObject = (ConstructionBayScriptableObject)componentScriptableObject;
    }

    public bool AddConstructionToQueue(ShipConstructionBlueprint shipBlueprint) {
        if (shipBlueprint.GetFaction().TransferCredits(shipBlueprint.cost, unit.faction)) {
            unit.faction.UseCredits(shipBlueprint.cost);
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
        unit.faction.AddCredits(shipBlueprint.cost);
        unit.faction.TransferCredits(shipBlueprint.cost, shipBlueprint.GetFaction());
        buildQueue.RemoveAt(index);
    }

    public void UpdateConstructionBay(float deltaTime) {
        constructionTime -= deltaTime;
        if (constructionTime <= 0) {
            int amountMultiplier =
                (int)(Mathf.Abs(constructionTime) / constructionBayScriptableObject.constructionSpeed) + 1;
            constructionTime += constructionBayScriptableObject.constructionSpeed * amountMultiplier;
            UpdateConstruction(amountMultiplier);
        }
    }

    private void UpdateConstruction(int amountMultiplier) {
        int availableConstructionBays = constructionBayScriptableObject.constructionBays;
        long buildAmount = constructionBayScriptableObject.constructionAmount * amountMultiplier;
        if (buildAmount <= 0) return;
        Dictionary<CargoBay.CargoType, long> cargoReserved = new Dictionary<CargoBay.CargoType, long>();

        foreach (ShipConstructionBlueprint shipBlueprint in buildQueue.ToList()) {
            if (availableConstructionBays == 0) return;
            if (shipBlueprint.IsFinished()) continue;
            availableConstructionBays--;

            // We need to copy the ResourceCosts Dictionary so that we can concurrently remove entries
            foreach (KeyValuePair<CargoBay.CargoType, long> resourceCost in shipBlueprint.resourceCosts.ToList()) {
                long availableCargo = math.max(0,
                    unit.GetAllCargoOfType(resourceCost.Key, true) - cargoReserved.GetValueOrDefault(resourceCost.Key, 0));
                long amountToUse = math.min(availableCargo, math.min(buildAmount, resourceCost.Value));
                shipBlueprint.resourceCosts[resourceCost.Key] -= amountToUse;
                unit.UseCargo(amountToUse, resourceCost.Key);

                if (shipBlueprint.resourceCosts[resourceCost.Key] <= 0) {
                    shipBlueprint.resourceCosts.Remove(resourceCost.Key);
                    if (shipBlueprint.IsFinished() && BuildBlueprint(shipBlueprint)) {
                        buildQueue.Remove(shipBlueprint);
                        break;
                    }
                }
            }

        }
    }

    private bool BuildBlueprint(ShipConstructionBlueprint shipBlueprint) {
        if (!unit.IsStation()) return false;
        Ship ship = ((Station)unit).BuildShip(shipBlueprint.faction, shipBlueprint.shipScriptableObject,
            shipBlueprint.cost);
        if (ship == null) return false;
        if (unit.IsStation()) ((Station)unit).stationAI.OnShipBuilt(ship);
        return true;
    }

    public long GetCreditCostOfShip(Faction faction, ShipScriptableObject ship) {
        if (faction == this.faction) {
            return ship.cost;
        }
        if (faction != null) {
            // Other factions need to pay us for the metal
            long resourceCost = 0;
            for (int i = 0; i < ship.resourceTypes.Count; i++) {
                resourceCost += (long)(ship.resourceCosts[i] * battleManager.baseResourcePrice[ship.resourceTypes[i]]);
            }
            return (long)((ship.cost + resourceCost) * 1.3f);
        }
        return ship.cost;
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
