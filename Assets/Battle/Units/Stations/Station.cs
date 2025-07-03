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

    public Dictionary<CargoBay.CargoTypes, (long wanted, long has)> reservedCargo = new();
    public Dictionary<CargoBay.CargoTypes, (long wanted, long has)> contractedCargo = new();
    public Dictionary<CargoBay.CargoTypes, (long minWanted, long maxWanted, long has)> freeCargo = new();
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
        contractShipsDocked = new HashSet<FactionTrade.Contract>();
        switch (stationScriptableObject.stationType) {
            case StationType.MiningStation:
                stationAI = new MiningStationAI(this);
                break;
            case StationType.Shipyard:
            case StationType.FleetCommand:
            case StationType.TradeStation:
                stationAI = new ShipyardAI(this);
                long spaceAvailable = GetAvailableCargoSpace(CargoBay.CargoTypes.All);
                CargoBay.allCargoTypes.ForEach(c => {
                    if (reservedCargo.ContainsKey(c)) spaceAvailable -= reservedCargo[c].wanted;
                });
                spaceAvailable /= CargoBay.allCargoTypes.Count;
                CargoBay.allCargoTypes.ForEach(c => {
                    SetDesiredFreeCargoRange(c, 0, (long)(spaceAvailable * .7f));
                });
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
        if (!built || !IsSpawned()) return;

        base.UpdateUnit(deltaTime);
        if (Destroyed()) return;

        if (enemyUnitsInRange.Count == 0)
            repairTime -= deltaTime;
        SetRotation(rotation + rotationSpeed * deltaTime);
        stationAI.UpdateAI(deltaTime);
        if (repairTime <= 0) {
            repairTime += stationScriptableObject.repairSpeed;
        }

        foreach (FactionTrade.Contract contract in contractShipsDocked.ToList()) {
            if (contract.provider == this) {
                LoadContractToShip(200, contract);
            } else {
                UnloadContractFromShip(200, contract);
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
        return IsSpawned() && IsBuilt() && moduleSystem.Get<Hangar>().Any(h => h.DockShip(ship));
    }

    public void UndockShip(Ship ship) {
        moduleSystem.Get<Hangar>().First(h => h.ships.Contains(ship)).RemoveShip(ship);
        contractShipsDocked.RemoveWhere(c => c.provider == ship);
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
        long cargoUsed = amount - RemoveFreeCargo(amount, cargoType);

        // Use up reserved cargo next
        long reservedCargoUsed = math.min(amount - cargoUsed, reservedCargo.GetValueOrDefault(cargoType, (0, 0)).has);
        if (reservedCargoUsed != 0) {
            reservedCargo[cargoType] =
                (reservedCargo[cargoType].wanted, reservedCargo[cargoType].has - reservedCargoUsed);
            if (faction.factionTrade.resourcesRequested[cargoType].ContainsKey(this)) {
                faction.factionTrade.resourcesRequested[cargoType][this] =
                    new FactionTrade.Offer(faction.factionTrade.resourcesRequested[cargoType][this],
                        faction.factionTrade.resourcesRequested[cargoType][this].amount + reservedCargoUsed);
            } else {
                faction.factionTrade.resourcesRequested[cargoType][this] =
                    new FactionTrade.Offer(cargoType, reservedCargoUsed, 1.2f);
            }
        }

        // Now update the actual cargo
        return base.UseCargo(cargoUsed + reservedCargoUsed, cargoType);
    }

    public override long LoadCargo(long amount, CargoBay.CargoTypes cargoType, FactionTrade.Contract? contract = null) {
        long leftover = base.LoadCargo(amount, cargoType);
        long toStore = amount - leftover;
        long toAdd = 0;
        bool updateRequestedOfferedResources = contract == null || contract.Value.receiver != this;

        if (reservedCargo.ContainsKey(cargoType)) {
            // Add to reserved cargo first
            toAdd = math.min(toStore, reservedCargo[cargoType].wanted - reservedCargo[cargoType].has);
            if (toAdd > 0) {
                reservedCargo[cargoType] = (reservedCargo[cargoType].wanted,
                    reservedCargo[cargoType].has + math.min(toStore, toAdd));

                if (faction.factionTrade.resourcesRequested[cargoType].ContainsKey(this) &&
                    updateRequestedOfferedResources) {
                    // Remove the cargo request from the faction level
                    if (faction.factionTrade.resourcesRequested[cargoType][this].amount == toAdd) {
                        faction.factionTrade.resourcesRequested[cargoType].Remove(this);
                    } else {
                        faction.factionTrade.resourcesRequested[cargoType][this] =
                            new FactionTrade.Offer(faction.factionTrade.resourcesRequested[cargoType][this],
                                faction.factionTrade.resourcesRequested[cargoType][this].amount - toAdd);
                    }
                }
            }
            toStore -= toAdd;
            if (toStore <= 0) return leftover;
        }

        if (contractedCargo.ContainsKey(cargoType) && updateRequestedOfferedResources) {
            // Then add to contracted cargo next
            toAdd = math.min(toStore, contractedCargo[cargoType].wanted - contractedCargo[cargoType].has);
            if (toAdd > 0)
                contractedCargo[cargoType] = (contractedCargo[cargoType].wanted,
                    contractedCargo[cargoType].has + math.min(toStore, toAdd));
            toStore -= toAdd;
            if (toStore <= 0) return leftover;
        }

        // Finally, all the rest should be added to free cargo
        if (updateRequestedOfferedResources) {
            AddFreeCargo(toStore, cargoType);
        } else {
            if (freeCargo.ContainsKey(cargoType))
                freeCargo[cargoType] = (freeCargo[cargoType].minWanted, freeCargo[cargoType].maxWanted,
                    freeCargo[cargoType].has + amount);
            else freeCargo[cargoType] = (0, 0, amount);
        }
        return leftover;
    }

    public void AddContract(FactionTrade.Contract contract, bool mustHaveImmediateResources = true) {
        // Validate that the receiver can buy from the provider
        if (contract.provider.faction != contract.receiver.faction &&
            !contract.provider.faction.factionTrade.tradeSellAgreements.ContainsKey(contract.receiver.faction))
            throw new Exception("Trying to buy without a trade agreement!");
        if (contract.provider == this) {
            foreach (var offer in contract.cargo.Values) {
                if ((!faction.factionTrade.resourcesOffered[offer.cargoType].ContainsKey(this) ||
                        offer.amount > faction.factionTrade.resourcesOffered[offer.cargoType][this].amount)
                    && mustHaveImmediateResources)
                    throw new Exception("Trying to create a contract without supplied resources!");
                long initialAmount = offer.amount - RemoveFreeCargo(offer.amount, offer.cargoType);
                var current = contractedCargo.GetValueOrDefault(offer.cargoType, (0, 0));
                contractedCargo[offer.cargoType] = (current.wanted + offer.amount, current.has + initialAmount);
            }
        } else {
            foreach (var offer in contract.cargo.Values) {
                if ((!faction.factionTrade.resourcesRequested[offer.cargoType].ContainsKey(this) ||
                        offer.amount > faction.factionTrade.resourcesRequested[offer.cargoType][this].amount)
                    && mustHaveImmediateResources)
                    throw new Exception("Trying to create a contract without demanded resources!");
                if (!faction.factionTrade.resourcesRequested[offer.cargoType].ContainsKey(this)) continue;
                long amount = faction.factionTrade.resourcesRequested[offer.cargoType][this].amount;
                if (offer.amount == amount) {
                    faction.factionTrade.resourcesRequested[offer.cargoType].Remove(this);
                } else {
                    faction.factionTrade.resourcesRequested[offer.cargoType][this] = new FactionTrade.Offer(
                        offer.cargoType, amount - offer.amount,
                        faction.factionTrade.resourcesRequested[offer.cargoType][this].price);
                }
            }
        }
        contract.provider.faction.factionTrade.activeContracts.Add(contract);
        contract.receiver.faction.factionTrade.activeContracts.Add(contract);
    }

    public void RemoveContract(FactionTrade.Contract contract) {
        if (contract.provider == this) {
            foreach (var request in contract.cargo.Values) {
                long extra = contractedCargo[request.cargoType].has - contractedCargo[request.cargoType].wanted +
                    request.amount;
                var current = contractedCargo.GetValueOrDefault(request.cargoType, (0, 0));
                if (contractedCargo[request.cargoType].wanted - request.amount == 0)
                    contractedCargo.Remove(request.cargoType);
                else contractedCargo[request.cargoType] = (current.wanted - request.amount, current.has - extra);
                if (extra > 0) LoadCargo(extra, request.cargoType);
            }
        }
        contract.provider.faction.factionTrade.activeContracts.Remove(contract);
        contract.receiver.faction.factionTrade.activeContracts.Remove(contract);
        contractShipsDocked.Remove(contract);
    }

    /// <summary>
    /// Unloads the cargo from the ship to the station based on the contract.
    /// </summary>
    /// <param name="amount">The total amount of cargo to unload in this operation</param>
    /// <returns>True if the contract is finished, false otherwise</returns>
    public bool UnloadContractFromShip(long amount, FactionTrade.Contract contract) {
        Assert.AreEqual(contract.receiver, this);
        Assert.IsTrue(contract.provider.IsShip());
        Assert.AreEqual(((Ship)contract.provider).dockedStation, this);
        long cargoToMove = amount;

        foreach (var offer in contract.cargo.Values.ToList()) {
            long toMove = math.min(cargoToMove, offer.amount);
            toMove -= LoadCargoFromUnit(toMove, offer.cargoType, contract.provider, contract);
            cargoToMove -= toMove;
            contract.cargo[offer.cargoType] =
                new FactionTrade.Offer(offer.cargoType, offer.amount - toMove, offer.price);
            faction.TransferCredits((long)(toMove * offer.price), contract.provider.faction);
            if (contract.cargo[offer.cargoType].amount == 0) contract.cargo.Remove(offer.cargoType);
            if (cargoToMove == 0) break;
        }

        if (contract.cargo.Any()) return false;
        RemoveContract(contract);
        return true;
    }

    /// <summary>
    /// Loads cargo from the station onto the ship based on the contract.
    /// </summary>
    /// <param name="amount">The total amount of cargo to load in this operation</param>
    /// <param name="contract">The contract to load</param>
    /// <returns>True if the contract is finished, false otherwise</returns>
    public bool LoadContractToShip(long amount, FactionTrade.Contract contract) {
        Assert.AreEqual(contract.provider, this);
        Assert.IsTrue(contract.receiver.IsShip());
        Assert.AreEqual(((Ship)contract.receiver).dockedStation, this);
        long cargoToMove = amount;

        foreach (var offer in contract.cargo.Values.ToList()) {
            long toMove = math.min(math.min(math.min(cargoToMove, offer.amount),
                contract.receiver.GetAvailableCargoSpace(offer.cargoType)), contractedCargo[offer.cargoType].has);
            // Actually update the cargo bays
            Assert.AreEqual(0, base.UseCargo(toMove, offer.cargoType));
            Assert.AreEqual(0, contract.receiver.LoadCargo(toMove, offer.cargoType));
            if (offer.amount - toMove == 0) {
                contract.cargo.Remove(offer.cargoType);
            } else {
                contract.cargo[offer.cargoType] = new FactionTrade.Offer(offer, offer.amount - toMove);
            }

            if (contractedCargo[offer.cargoType].wanted - toMove == 0) {
                contractedCargo.Remove(offer.cargoType);
            } else {
                contractedCargo[offer.cargoType] = (contractedCargo[offer.cargoType].wanted - toMove,
                    contractedCargo[offer.cargoType].has - toMove);
                if (contractedCargo[offer.cargoType].has < 0)
                    Debug.Log("asdfasfasfdsf");
            }
            contract.receiver.faction.TransferCredits((long)(toMove * offer.price), faction);
            cargoToMove -= toMove;
            if (cargoToMove == 0)
                break;
        }

        if (contract.cargo.Any()) return false;
        RemoveContract(contract);
        return true;
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
        return !freeCargo.TryGetValue(cargoType, out (long minWanted, long maxWanted, long has) value2)
            ? cargo
            : cargo + value2.has;
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

    public void ReserveCargo(long amount, CargoBay.CargoTypes cargoType) {
        long initialAmount = amount - RemoveFreeCargo(amount, cargoType);
        if (reservedCargo.ContainsKey(cargoType)) {
            reservedCargo[cargoType] = (reservedCargo[cargoType].wanted + amount,
                reservedCargo[cargoType].has + initialAmount);
        } else {
            reservedCargo[cargoType] = (amount, initialAmount);
        }

        if (amount == initialAmount) return;
        // Request more resources at the faction level
        if (faction.factionTrade.resourcesRequested[cargoType].ContainsKey(this)) {
            faction.factionTrade.resourcesRequested[cargoType][this] = new FactionTrade.Offer(cargoType,
                faction.factionTrade.resourcesRequested[cargoType][this].amount + amount - initialAmount,
                GetRequestPriceForCargoType(cargoType));
        } else {
            faction.factionTrade.resourcesRequested[cargoType]
                .Add(this, new FactionTrade.Offer(cargoType, amount - initialAmount, 1.2f));
        }
    }

    public void UnReserveCargo(long amount, CargoBay.CargoTypes cargoType) {
        long extra = math.max(reservedCargo[cargoType].has - reservedCargo[cargoType].wanted + amount, 0);
        AddFreeCargo(extra, cargoType);
        if (reservedCargo[cargoType].wanted > amount) {
            reservedCargo[cargoType] = (reservedCargo[cargoType].wanted - amount, reservedCargo[cargoType].has - extra);
        } else {
            reservedCargo.Remove(cargoType);
        }

        if (extra == amount) return;
        // Remove requested resources at the faction level
        if (amount - extra == faction.factionTrade.resourcesRequested[cargoType][this].amount) {
            faction.factionTrade.resourcesRequested[cargoType].Remove(this);
        } else {
            faction.factionTrade.resourcesRequested[cargoType][this] = new FactionTrade.Offer(cargoType,
                faction.factionTrade.resourcesRequested[cargoType][this].amount - (amount - extra),
                GetRequestPriceForCargoType(cargoType));
        }
    }

    /// <returns>The amount of cargo not removed. Does not modify the cargo bay.</returns>
    private long RemoveFreeCargo(long amount, CargoBay.CargoTypes cargoType) {
        if (!freeCargo.ContainsKey(cargoType)) return amount;
        var previousCargo = freeCargo[cargoType];
        long amountUsed = math.min(amount, previousCargo.has);
        if (amountUsed == 0) return amount;

        // Remove the cargo
        if (previousCargo.has == amount && freeCargo[cargoType].minWanted == 0 && freeCargo[cargoType].maxWanted == 0) {
            freeCargo.Remove(cargoType);
        } else {
            freeCargo[cargoType] = (freeCargo[cargoType].minWanted, freeCargo[cargoType].maxWanted,
                freeCargo[cargoType].has - amountUsed);
        }

        // Recalculate the resources offered
        long offeredAmountUsed = math.min(amountUsed, previousCargo.has - previousCargo.maxWanted);
        if (offeredAmountUsed > 0 && faction.factionTrade.resourcesOffered[cargoType].ContainsKey(this)) {
            if (offeredAmountUsed == faction.factionTrade.resourcesOffered[cargoType][this].amount) {
                faction.factionTrade.resourcesOffered[cargoType].Remove(this);
            } else {
                faction.factionTrade.resourcesOffered[cargoType][this] = new FactionTrade.Offer(cargoType,
                    faction.factionTrade.resourcesOffered[cargoType][this].amount - offeredAmountUsed,
                    GetOfferPriceForCargoType(cargoType));
            }
        }

        // Recalculate the resources requested
        long requestedAmountUsed = math.min(amountUsed, previousCargo.minWanted - previousCargo.has + amountUsed);
        if (requestedAmountUsed > 0) {
            if (faction.factionTrade.resourcesRequested[cargoType].ContainsKey(this)) {
                faction.factionTrade.resourcesRequested[cargoType][this] = new FactionTrade.Offer(cargoType,
                    faction.factionTrade.resourcesRequested[cargoType][this].amount + requestedAmountUsed,
                    GetRequestPriceForCargoType(cargoType));
            } else {
                faction.factionTrade.resourcesRequested[cargoType]
                    .Add(this, new FactionTrade.Offer(cargoType, requestedAmountUsed, 1.2f));
            }
        }

        return amount - amountUsed;
    }

    /// <summary>
    /// Adds the free cargo, does not modify the cargo bay or check if it is over capacity.
    /// </summary>
    private void AddFreeCargo(long amount, CargoBay.CargoTypes cargoType) {
        if (amount == 0) return;

        var amountFree = freeCargo.GetValueOrDefault(cargoType, (0, 0, 0));

        // Recalculate the resources offered
        long offerAmountAdded = math.min(amount, amountFree.has - amountFree.maxWanted + amount);
        if (offerAmountAdded > 0) {
            if (!faction.factionTrade.resourcesOffered[cargoType].ContainsKey(this)) {
                faction.factionTrade.resourcesOffered[cargoType]
                    .Add(this, new FactionTrade.Offer(cargoType, offerAmountAdded, 1.2f));
            } else {
                faction.factionTrade.resourcesOffered[cargoType][this] = new FactionTrade.Offer(
                    cargoType, faction.factionTrade.resourcesOffered[cargoType][this].amount + offerAmountAdded,
                    GetOfferPriceForCargoType(cargoType));
            }
        }

        // Recalculate the resources requested
        long requestedAmountAdded = math.min(amount, amountFree.minWanted - amountFree.has);
        if (requestedAmountAdded > 0 && faction.factionTrade.resourcesRequested[cargoType].ContainsKey(this)) {
            if (faction.factionTrade.resourcesRequested[cargoType][this].amount == requestedAmountAdded) {
                faction.factionTrade.resourcesRequested[cargoType].Remove(this);
            } else {
                faction.factionTrade.resourcesRequested[cargoType][this] = new FactionTrade.Offer(
                    cargoType, faction.factionTrade.resourcesRequested[cargoType][this].amount - requestedAmountAdded,
                    GetOfferPriceForCargoType(cargoType));
            }
        }

        if (freeCargo.ContainsKey(cargoType))
            freeCargo[cargoType] = (freeCargo[cargoType].minWanted, freeCargo[cargoType].maxWanted,
                freeCargo[cargoType].has + amount);
        else freeCargo[cargoType] = (0, 0, amount);
    }

    public void SetDesiredFreeCargoRange(CargoBay.CargoTypes cargoType, long minWanted, long maxWanted) {
        var previousCargo = freeCargo.GetValueOrDefault(cargoType, (0, 0, 0));
        freeCargo[cargoType] = (minWanted, maxWanted, previousCargo.has);
        if (minWanted > maxWanted)
            throw new Exception("Illegal arguments given: minWanted must be lower than maxWanted!");

        long minWantedDifference = previousCargo.minWanted - minWanted;
        if (minWantedDifference > 0) {
            // We want to store less of this resource
            long change = math.min(minWantedDifference, previousCargo.minWanted - previousCargo.has);
            if (change > 0 && faction.factionTrade.resourcesRequested[cargoType].ContainsKey(this)) {
                if (change == faction.factionTrade.resourcesRequested[cargoType][this].amount) {
                    faction.factionTrade.resourcesRequested[cargoType].Remove(this);
                } else {
                    faction.factionTrade.resourcesRequested[cargoType][this] = new FactionTrade.Offer(
                        cargoType, faction.factionTrade.resourcesRequested[cargoType][this].amount - change,
                        GetRequestPriceForCargoType(cargoType));
                }
            }
        } else if (minWantedDifference < 0) {
            // We want to store more of this resource
            long change = math.min(-minWantedDifference, minWanted - previousCargo.has);
            if (change > 0) {
                if (faction.factionTrade.resourcesRequested[cargoType].ContainsKey(this)) {
                    faction.factionTrade.resourcesRequested[cargoType][this] = new FactionTrade.Offer(
                        cargoType, faction.factionTrade.resourcesRequested[cargoType][this].amount + change,
                        GetRequestPriceForCargoType(cargoType));
                } else {
                    faction.factionTrade.resourcesRequested[cargoType]
                        .Add(this, new FactionTrade.Offer(cargoType, change, 1.2f));
                }
            }
        }

        long maxWantedDifference = previousCargo.maxWanted - maxWanted;
        if (maxWantedDifference > 0) {
            // We want to store less of this resource
            long change = math.min(maxWantedDifference, previousCargo.has - maxWanted);
            if (change > 0) {
                if (faction.factionTrade.resourcesOffered[cargoType].ContainsKey(this)) {
                    faction.factionTrade.resourcesOffered[cargoType][this] = new FactionTrade.Offer(cargoType,
                        faction.factionTrade.resourcesOffered[cargoType][this].amount + change,
                        GetOfferPriceForCargoType(cargoType));
                } else {
                    faction.factionTrade.resourcesOffered[cargoType]
                        .Add(this, new FactionTrade.Offer(cargoType, change, 1.2f));
                }
            }
        } else if (maxWantedDifference < 0) {
            // We want to store more of this resource
            long change = math.min(-maxWantedDifference, previousCargo.has - previousCargo.maxWanted);
            if (change > 0 && faction.factionTrade.resourcesOffered[cargoType].ContainsKey(this)) {
                if (faction.factionTrade.resourcesOffered[cargoType][this].amount == change) {
                    faction.factionTrade.resourcesOffered[cargoType].Remove(this);
                } else {
                    faction.factionTrade.resourcesOffered[cargoType][this] = new FactionTrade.Offer(
                        cargoType, faction.factionTrade.resourcesOffered[cargoType][this].amount - change,
                        GetOfferPriceForCargoType(cargoType));
                }
            }
        }
    }

    float GetRequestPriceForCargoType(CargoBay.CargoTypes cargoType) {
        var reserved = reservedCargo.GetValueOrDefault(cargoType, (0, 0));
        var free = freeCargo.GetValueOrDefault(cargoType, (0, 0, 0));
        float priceModifier = 1f;
        // If we are a mining station then we can produce our own resources
        if (cargoType == CargoBay.CargoTypes.Metal && this is MiningStation) priceModifier = .7f;
        long reservedDiff = reserved.wanted - reserved.has;
        if (reservedDiff > 0) {
            // We desperately want more cargo for our more essential station functions
            int c = 500;
            priceModifier *= math.pow((reservedDiff + c) / (float)c, 1.1f) / ((reservedDiff + c) / (float)c) + .1f;
            // We might want more non-reserved cargo so slightly modify it further
            c = 5000;
            return priceModifier * (math.pow((free.minWanted + c) / (float)c, 1.1f)
                / ((free.minWanted + c) / (float)c) + .1f);
        } else if (free.minWanted > free.has) {
            // We would like to store extra cargo
            const int c = 1000;
            long freeDiff = free.minWanted - free.has;
            return priceModifier * (math.pow((freeDiff + c) / (float)c, 1.1f) / ((freeDiff + c) / (float)c) + .1f);
        } else if (free.maxWanted > free.has) {
            // We would buy cargo at a decent price
            const int c = 2000;
            long freeDiff = free.maxWanted - free.has;
            return priceModifier * (math.pow((freeDiff + c) / (float)c, 1.1f) / ((freeDiff + c) / (float)c) + .1f);
        } else {
            // We have too much cargo
            return 0;
        }
    }

    float GetOfferPriceForCargoType(CargoBay.CargoTypes cargoType) {
        var free = freeCargo.GetValueOrDefault(cargoType, (0, 0, 0));
        // We have too little cargo to sell
        if (free.has <= free.minWanted) return 1000;
        float priceModifier = 1f;
        // Mining stations can sell cargo for cheaper
        if (cargoType == CargoBay.CargoTypes.Metal && this is MiningStation) priceModifier = .7f;
        long freeWantDiff = free.maxWanted - free.has;
        if (free.maxWanted > free.has) {
            // We have a little extra cargo that we can sell
            int c = 1500;
            return priceModifier * math.pow((freeWantDiff + c) / (float)c, .9f) / ((freeWantDiff + c) / (float)c);
        } else {
            // We have too much cargo and would like to sell it
            int c = 500;
            long freeDiff = free.has - free.maxWanted;
            priceModifier *= math.pow((freeDiff + c) / (float)c, .9f) / ((freeDiff + c) / (float)c);
            // We would also like to sell our other cargo
            c = 5000;
            return priceModifier * math.pow((freeWantDiff + c) / (float)c, .9f) / ((freeWantDiff + c) / (float)c);
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
