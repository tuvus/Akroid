using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using static Ship;
using Random = Unity.Mathematics.Random;

public class Station : Unit, IPositionConfirmer {
    public enum StationType {
        None = 0,
        FleetCommand = 1,
        DefenceStation = 2,
        MiningStation = 3,
        Shipyard = 4,
        TradeStation = 5,
        ResearchStation = 6
    }

    protected bool built;
    private Random random;
    private readonly float rotationSpeed;

    public StationScriptableObject stationScriptableObject { get; }

    public StationAI stationAI { get; protected set; }
    public float repairTime { get; protected set; }

    public Dictionary<CargoBay.CargoTypes, (long wanted, long has)> reservedCargo;
    public Dictionary<CargoBay.CargoTypes, (long wanted, long has)> contractedCargo;
    public Dictionary<CargoBay.CargoTypes, (long wanted, long has)> freeCargo;
    public HashSet<FactionTrade.Contract> contractShipsDocked;

    [Serializable]
    public class StationBlueprint {
        public string name;
        public StationScriptableObject stationScriptableObject;
        public long stationCost;
        public List<CargoBay.CargoTypes> resourcesTypes;
        public List<long> resources;
        public long totalResourcesRequired;

        private StationBlueprint(StationScriptableObject stationScriptableObject, string name) {
            this.stationScriptableObject = stationScriptableObject;
            this.name = name;
            stationCost = stationScriptableObject.cost;
            resourcesTypes = new List<CargoBay.CargoTypes>(stationScriptableObject.resourceTypes);
            resources = new List<long>(stationScriptableObject.resourceCosts);
            for (int i = 0; i < resources.Count; i++) {
                totalResourcesRequired += resources[i];
            }
        }

        public StationBlueprint CreateStationBlueprint(string name = null) {
            if (name == null)
                name = this.name;
            return new StationBlueprint(stationScriptableObject, name);
        }
    }

    public Station(BattleObjectData battleObjectData, BattleManager battleManager,
        StationScriptableObject stationScriptableObject, bool built)
        : base(battleObjectData, battleManager, stationScriptableObject) {
        this.stationScriptableObject = stationScriptableObject;
        reservedCargo = new Dictionary<CargoBay.CargoTypes, (long wanted, long has)>();
        contractedCargo = new Dictionary<CargoBay.CargoTypes, (long wanted, long has)>();
        freeCargo = new Dictionary<CargoBay.CargoTypes, (long wanted, long has)>();
        switch (stationScriptableObject.stationType) {
            case StationType.MiningStation:
                stationAI = new MiningStationAI(this);
                break;
            case StationType.Shipyard:
            case StationType.FleetCommand:
            case StationType.TradeStation:
                stationAI = new ShipyardAI(this);
                break;
            default:
                stationAI = new StationAI(this);
                break;
        }

        this.built = built;
        if (!built) {
            faction.AddStationBlueprint(this);
            health = 0;
            moduleSystem.Get<Turret>().ForEach(t => t.ShowTurret(false));
        } else {
            faction.AddStation(this);
            Spawn();
            faction.GetFactionAI().OnStationBuilt(this);
        }
        random = new Random((uint)battleManager.battleObjects.Count + 1);

        rotationSpeed = stationScriptableObject.rotationSpeed * random.NextFloat(.5f, 1.5f);
        if (random.NextBool()) {
            rotationSpeed *= -1;
        }

        visible = true;
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (IPositionConfirmer blockingObject in battleManager.GetPositionBlockingObjects()) {
            if (blockingObject is Station) {
                Station station = (Station)blockingObject;
                float enemyBonus = 0;
                if (faction.IsAtWarWithFaction(station.faction))
                    enemyBonus = GetMaxWeaponRange() * 2;
                if (Vector2.Distance(position, station.GetPosition()) <=
                    minDistanceFromObject + enemyBonus + station.GetSize() + GetSize()) {
                    return false;
                }
            } else if (Vector2.Distance(position, blockingObject.GetPosition()) <=
                minDistanceFromObject + GetSize() + blockingObject.GetSize()) {
                return false;
            }
        }

        foreach (Station stationBlueprint in battleManager.stationsInProgress) {
            float enemyBonus = 0;
            if (faction.IsAtWarWithFaction(stationBlueprint.faction))
                enemyBonus = GetMaxWeaponRange() * 2;
            if (Vector2.Distance(position, stationBlueprint.GetPosition()) <=
                minDistanceFromObject + enemyBonus + stationBlueprint.GetSize() + GetSize()) {
                return false;
            }
        }

        return true;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = battleManager.FindFreeLocationIncrement(positionGiver, this);
        return targetPosition.HasValue ? targetPosition.Value : positionGiver.position;
    }

    public override void UpdateUnit(float deltaTime) {
        if (built && IsSpawned()) {
            base.UpdateUnit(deltaTime);
            if (enemyUnitsInRange.Count == 0)
                repairTime -= deltaTime;
            SetRotation(rotation + rotationSpeed * deltaTime);
            stationAI.UpdateAI(deltaTime);
            if (repairTime <= 0) {
                repairTime += stationScriptableObject.repairSpeed;
            }
        }
    }

    #region StationControls

    public virtual Ship BuildShip(ShipClass shipClass, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipClass, cost, undock);
    }

    public virtual Ship BuildShip(ShipScriptableObject shipScriptableObject, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipScriptableObject, cost, undock);
    }

    public virtual Ship BuildShip(ShipType shipType, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipType, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipClass shipClass, long cost = 0, bool? undock = false) {
        ShipScriptableObject shipScriptableObject = battleManager.GetShipBlueprint(shipClass).shipScriptableObject;
        return BuildShip(faction, battleManager.GetShipBlueprint(shipClass).shipScriptableObject,
            shipScriptableObject.unitName, cost,
            undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipClass shipClass, string shipName, long cost = 0,
        bool? undock = false) {
        return BuildShip(faction, battleManager.GetShipBlueprint(shipClass).shipScriptableObject, shipName, cost,
            undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipScriptableObject shipScriptableObject, long cost = 0,
        bool? undock = false) {
        return BuildShip(faction, shipScriptableObject, shipScriptableObject.unitName, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipType shipType, long cost = 0, bool? undock = false) {
        ShipScriptableObject shipScriptableObject = battleManager.GetShipBlueprint(shipType).shipScriptableObject;
        return BuildShip(faction, shipScriptableObject, shipScriptableObject.unitName, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipType shipType, string shipName, long cost = 0,
        bool? undock = false) {
        ShipScriptableObject shipScriptableObject = battleManager.GetShipBlueprint(shipType).shipScriptableObject;
        return BuildShip(faction, shipScriptableObject, shipName, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipScriptableObject shipScriptableObject, string shipName,
        long cost = 0, bool? undock = false) {
        return BuildShip(new BattleObjectData(shipName, position, random.NextFloat(0, 360), faction),
            shipScriptableObject, cost, undock);
    }

    /// <summary>
    ///     Builds a ship from this station and adds it to the faction at factionIndex.
    ///     If Undock is true, docks then undocks the ship
    ///     If undock is false, docks the ship
    ///     If undock is null, it doesn't dock the ship at all.
    /// </summary>
    /// <returns>The newly built ship</returns>
    public virtual Ship BuildShip(BattleObjectData battleObjectData, ShipScriptableObject shipScriptableObject,
        long cost = 0, bool? undock = false) {
        Ship newShip = battleManager.CreateNewShip(battleObjectData, shipScriptableObject);
        if (undock == null) {
            // The ship will be built at this station, however it's position is somewhere else in the system
        } else if ((bool)undock) {
            newShip.DockShip(this);
            newShip.UndockShip();
        } else {
            newShip.DockShip(this);
        }

        return newShip;
    }

    public override void Explode() {
        if (!built) Spawn();
        base.Explode();
    }

    public override void DestroyUnit() {
        base.DestroyUnit();
        battleManager.DestroyStation(this);
    }

    /// <summary> Docks a ship to the staiton, should only be called from the ship. /// </summary>
    public bool DockShip(Ship ship) {
        Hangar openHangar = moduleSystem.Get<Hangar>().FirstOrDefault(h => h.CanDockShip());
        if (IsSpawned() && IsBuilt() && openHangar != null && openHangar.DockShip(ship)) {
            return true;
        }

        return false;
    }

    public void UndockShip(Ship ship) {
        moduleSystem.Get<Hangar>().First(h => h.ships.Contains(ship)).RemoveShip(ship);
    }

    public int RepairUnit(Unit unit, int amount) {
        int leftOver = unit.Repair(amount);
        repairTime += stationScriptableObject.repairSpeed * (amount - leftOver) / stationScriptableObject.repairAmount;
        return leftOver;
    }

    public virtual bool BuildStation() {
        if (!built) {
            battleManager.BuildStationBlueprint(this);
            faction.RemoveStationBlueprint(this);
            faction.AddStation(this);
            built = true;
            health = GetMaxHealth();
            moduleSystem.Get<Turret>().ForEach(t => t.ShowTurret(true));
            Spawn();
            faction.GetFactionAI().OnStationBuilt(this);
            return true;
        }

        return false;
    }

    #endregion

    #region Cargo

    /// <summary>
    /// Always uses reserved cargo last
    /// </summary>
    public override long UseCargo(long amount, CargoBay.CargoTypes cargoType) {
        // Use up free cargo first
        long cargoUsed = math.min(amount, freeCargo.GetValueOrDefault(cargoType, (0, 0)).has);
        if (cargoUsed != 0)
            freeCargo[cargoType] = (freeCargo[cargoType].wanted, freeCargo[cargoType].has - cargoUsed);

        // Use up reserved cargo next
        long reservedCargoUsed = math.min(amount - cargoUsed, reservedCargo.GetValueOrDefault(cargoType, (0, 0)).has);
        if (reservedCargoUsed != 0)
            reservedCargo[cargoType] =
                (reservedCargo[cargoType].wanted, reservedCargo[cargoType].has - reservedCargoUsed);

        // Now update the actual cargo
        return base.UseCargo(cargoUsed + reservedCargoUsed, cargoType);
    }

    public override long LoadCargo(long amount, CargoBay.CargoTypes cargoType) {
        long leftover = base.LoadCargo(amount, cargoType);
        long toStore = amount - leftover;
        long toAdd = 0;
        if (reservedCargo.ContainsKey(cargoType)) {
            // Add to reserved cargo first
            toAdd = reservedCargo[cargoType].wanted - reservedCargo[cargoType].has;
            if (toAdd > 0) {
                reservedCargo[cargoType] = (reservedCargo[cargoType].wanted,
                    reservedCargo[cargoType].has + math.min(toStore, toAdd));
            }
            toStore -= toAdd;
            if (toStore <= 0) return leftover;
        }

        if (contractedCargo.ContainsKey(cargoType)) {
            // Then add to contracted cargo next
            toAdd = contractedCargo[cargoType].wanted - contractedCargo[cargoType].has;
            if (toAdd > 0)
                contractedCargo[cargoType] = (contractedCargo[cargoType].wanted,
                    contractedCargo[cargoType].has + math.min(toStore, toAdd));
            toStore -= toAdd;
            if (toStore <= 0) return leftover;
        }

        if (toStore > 0) {
            // Finally, all the rest should be added to free cargo
            if (freeCargo.ContainsKey(cargoType))
                freeCargo[cargoType] = (freeCargo[cargoType].wanted, freeCargo[cargoType].has + toStore);
            else freeCargo[cargoType] = (0, toStore);
        }
        return leftover;
    }

    /// <summary>
    /// Unloads the cargo from the ship to the station based on the contract.
    /// </summary>
    /// <returns>True if the contract is finished, false otherwise</returns>
    public bool UnloadContractFromShip(FactionTrade.Contract contract) {
        Assert.AreEqual(contract.receiver, this);
        Assert.IsTrue(contract.provider.IsShip());
        Assert.AreEqual(((Ship)contract.provider).dockedStation, this);
        long cargoToMove = 400;

        foreach (var offer in contract.cargo.Values.ToList()) {
            long toMove = math.min(cargoToMove, offer.amount);
            toMove -= LoadCargoFromUnit(toMove, offer.cargoType, contract.provider);
            cargoToMove -= toMove;
            contract.cargo[offer.cargoType] =
                new FactionTrade.Offer(offer.cargoType, offer.amount - toMove, offer.price);
            faction.TransferCredits((long)(toMove * offer.price), contract.provider.faction);
            if (contract.cargo[offer.cargoType].amount == 0) contract.cargo.Remove(offer.cargoType);
            if (cargoToMove == 0) return false;
        }
        return !contract.cargo.Any();
    }

    public bool LoadContractToShip(long amount, FactionTrade.Contract contract) {
        Assert.AreEqual(contract.provider, this);
        Assert.IsTrue(contract.receiver.IsShip());
        Assert.AreEqual(((Ship)contract.receiver).dockedStation, this);
        long cargoToMove = amount;

        foreach (var offer in contract.cargo.Values.ToList()) {
            long toMove = math.min(cargoToMove, offer.amount);
            toMove -= contract.receiver.LoadCargoFromUnit(toMove, offer.cargoType, contract.provider);
            cargoToMove -= toMove;
            contract.cargo[offer.cargoType] =
                new FactionTrade.Offer(offer.cargoType, offer.amount - toMove, offer.price);
            contractedCargo[offer.cargoType] = (contractedCargo[offer.cargoType].wanted - toMove,
                contractedCargo[offer.cargoType].has - toMove);
            contract.receiver.faction.TransferCredits((long)(toMove * offer.price), faction);
            if (contract.cargo[offer.cargoType].amount == 0) contract.cargo.Remove(offer.cargoType);
            if (cargoToMove == 0) return false;
        }
        return !contract.cargo.Any();
    }

    public override long GetAllCargoOfType(CargoBay.CargoTypes cargoType, bool includeReserved = false) {
        long cargo = 0;
        if (includeReserved) {
            if (cargoType == CargoBay.CargoTypes.All)
                cargo = reservedCargo.Sum(c => c.Value.has);
            else cargo = !reservedCargo.TryGetValue(cargoType, out (long wanted, long has) value) ? 0 : value.has;
        }
        if (cargoType == CargoBay.CargoTypes.All)
            return cargo + freeCargo.Sum(c => c.Value.has);
        return !freeCargo.TryGetValue(cargoType, out (long wanted, long has) value2) ? cargo : cargo + value2.has;
    }

    public long GetAllCargoOfType(CargoBay.CargoTypes cargoType, bool includeReserved, bool includeContract) {
        long cargo = 0;
        if (includeContract) {
            if (cargoType == CargoBay.CargoTypes.All)
                cargo += contractedCargo.Sum(c => c.Value.has);
            else cargo += !contractedCargo.TryGetValue(cargoType, out (long wanted, long has) value) ? 0 : value.has;
        }
        return cargo + GetAllCargoOfType(cargoType, includeReserved);
    }

    public void AddReservedCargo(long amount, CargoBay.CargoTypes cargoType) {
        long initialAmount = 0;
        if (freeCargo.ContainsKey(cargoType)) {
            initialAmount = math.min(amount, freeCargo[cargoType].has);
            freeCargo[cargoType] = (freeCargo[cargoType].wanted, freeCargo[cargoType].has - initialAmount);
        }
        if (reservedCargo.ContainsKey(cargoType)) {
            reservedCargo[cargoType] = (reservedCargo[cargoType].wanted + amount,
                reservedCargo[cargoType].has + initialAmount);
        } else {
            reservedCargo[cargoType] = (amount, initialAmount);
        }
    }

    public void RemoveReservedCargo(long amount, CargoBay.CargoTypes cargoType) {
        long extra = math.max(reservedCargo[cargoType].has - reservedCargo[cargoType].wanted + amount, 0);
        if (extra > 0) {
            if (freeCargo.ContainsKey(cargoType))
                freeCargo[cargoType] = (freeCargo[cargoType].wanted, freeCargo[cargoType].has + extra);
            else freeCargo[cargoType] = (0, extra);
        }
        if (reservedCargo[cargoType].wanted > amount) {
            reservedCargo[cargoType] = (reservedCargo[cargoType].wanted - amount, reservedCargo[cargoType].has - extra);
        } else {
            reservedCargo.Remove(cargoType);
        }
    }

    #endregion

    #region GetMethods

    public bool IsBuilt() {
        return built;
    }

    public int GetRepairAmount() {
        return (int)(stationScriptableObject.repairAmount *
            faction.GetImprovementModifier(Faction.ImprovementAreas.HullStrength));
    }

    public StationType GetStationType() {
        return stationScriptableObject.stationType;
    }

    #endregion
}
