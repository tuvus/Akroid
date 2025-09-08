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

    public Dictionary<CargoBay.CargoType, (long wanted, long has)> reservedCargo = new();
    public Dictionary<CargoBay.CargoType, (long wanted, long has)> contractedCargo = new();
    public Dictionary<CargoBay.CargoType, (long minWanted, long maxWanted, long has)> freeCargo = new();
    public HashSet<FactionTrade.Contract> contractShipsDocked;
    public Dictionary<CargoBay.CargoType, long> pendingContractResources = new();
    public Population contractedPersonnel = new();
    public Population pendingPersonnel = new();
    public Dictionary<HabitationArea, Population> personnelRequests = new();
    public bool updatePopulation = false;

    [Serializable]
    public class StationBlueprint {
        public string name;
        public StationScriptableObject stationScriptableObject;
        public long stationCost;
        public List<CargoBay.CargoType> resourcesTypes;
        public List<long> resources;
        public long totalResourcesRequired;

        private StationBlueprint(StationScriptableObject stationScriptableObject, string name) {
            this.stationScriptableObject = stationScriptableObject;
            this.name = name;
            stationCost = stationScriptableObject.cost;
            resourcesTypes = new List<CargoBay.CargoType>(stationScriptableObject.resourceTypes);
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
        CargoBay.allCargoTypes.ForEach(c => pendingContractResources.TryAdd(c, 0));
        switch (stationScriptableObject.stationType) {
            case StationType.Shipyard:
            case StationType.FleetCommand:
            case StationType.TradeStation:
                stationAI = new ShipyardAI(this);
                long spaceAvailable = GetAvailableCargoSpace(CargoBay.CargoType.All);
                CargoBay.allCargoTypes.ForEach(c => {
                    if (reservedCargo.ContainsKey(c)) spaceAvailable -= reservedCargo[c].wanted;
                });
                spaceAvailable /= CargoBay.allCargoTypes.Count;
                CargoBay.allCargoTypes.ForEach(c => {
                    SetDesiredFreeCargoRange(c, 0, (long)(spaceAvailable * .8f));
                });
                break;
            case StationType.MiningStation:
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
        updatePopulation = true;
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
                if (contract is FactionTrade.TradeContract tradeContract)
                    LoadTradeContractToShip(200, tradeContract);
                else if (contract is FactionTrade.TransportContract transportContract)
                    LoadPersonnelToShip(transportContract);
            } else {
                if (contract is FactionTrade.TradeContract tradeContract)
                    UnloadContractFromShip(200, tradeContract);
                else if (contract is FactionTrade.TransportContract transportContract)
                    UnloadPersonnelFromShip(transportContract);

            }
        }

        if (updatePopulation) {
            UpdateJobMarket();
            updatePopulation = false;
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
        RemoveAllContracts();
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

    #region CargoAndPersonnel

    /// <summary>
    /// Always uses reserved cargo last
    /// </summary>
    public override long UseCargo(long amount, CargoBay.CargoType cargoType) {
        // Use up free cargo first
        long cargoUsed = amount - RemoveFreeCargo(amount, cargoType);

        // Use up reserved cargo next
        long reservedCargoUsed = math.min(amount - cargoUsed, reservedCargo.GetValueOrDefault(cargoType, (0, 0)).has);
        if (reservedCargoUsed != 0) {
            reservedCargo[cargoType] =
                (reservedCargo[cargoType].wanted, reservedCargo[cargoType].has - reservedCargoUsed);
        }

        UpdateCargoTrade(cargoType);
        // Now update the actual cargo
        return base.UseCargo(cargoUsed + reservedCargoUsed, cargoType);
    }

    public override long LoadCargo(long amount, CargoBay.CargoType cargoType,
        FactionTrade.TradeContract? contract = null) {
        long leftover = base.LoadCargo(amount, cargoType);
        long toStore = amount - leftover;
        long toAdd = 0;
        bool updateRequestedOfferedResources = contract == null || contract.receiver != this;

        if (reservedCargo.ContainsKey(cargoType)) {
            // Add to reserved cargo first
            toAdd = math.min(toStore, reservedCargo[cargoType].wanted - reservedCargo[cargoType].has);
            if (toAdd > 0) {
                reservedCargo[cargoType] = (reservedCargo[cargoType].wanted,
                    reservedCargo[cargoType].has + math.min(toStore, toAdd));
            }
            toStore -= toAdd;
            if (toStore <= 0) {
                if (contract == null)
                    UpdateCargoTrade(cargoType);
                return leftover;
            }
        }

        if (contractedCargo.ContainsKey(cargoType) && updateRequestedOfferedResources) {
            // Then add to contracted cargo next
            toAdd = math.min(toStore, contractedCargo[cargoType].wanted - contractedCargo[cargoType].has);
            if (toAdd > 0)
                contractedCargo[cargoType] = (contractedCargo[cargoType].wanted,
                    contractedCargo[cargoType].has + math.min(toStore, toAdd));
            toStore -= toAdd;
            if (toStore <= 0) {
                if (contract == null)
                    UpdateCargoTrade(cargoType);
                return leftover;
            }
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


        if (contract == null)
            UpdateCargoTrade(cargoType);
        return leftover;
    }

    public override bool AddContract(FactionTrade.TradeContract tradeContract, bool mustHaveImmediateResources = true) {
        // Validate that the receiver can buy from the provider
        if (tradeContract.provider.faction != tradeContract.receiver.faction &&
            !tradeContract.provider.faction.factionTrade.tradeSellAgreements
                .ContainsKey(tradeContract.receiver.faction))
            throw new Exception("Trying to buy without a trade agreement!");

        base.AddContract(tradeContract, mustHaveImmediateResources);

        if (tradeContract.provider == this) {
            foreach (var offer in tradeContract.cargo.Values) {
                if ((!faction.factionTrade.resourcesOffered[offer.cargoType].ContainsKey(this) ||
                        offer.amount > faction.factionTrade.resourcesOffered[offer.cargoType][this].amount)
                    && mustHaveImmediateResources)
                    throw new Exception("Trying to create a contract without supplied resources!");
                long initialAmount = offer.amount - RemoveFreeCargo(offer.amount, offer.cargoType);
                var current = contractedCargo.GetValueOrDefault(offer.cargoType, (0, 0));
                contractedCargo[offer.cargoType] = (current.wanted + offer.amount, current.has + initialAmount);
                UpdateCargoTrade(offer.cargoType);
            }
        } else {
            foreach (var offer in tradeContract.cargo.Values) {
                if ((!faction.factionTrade.resourcesRequested[offer.cargoType].ContainsKey(this) ||
                        offer.amount > faction.factionTrade.resourcesRequested[offer.cargoType][this].amount)
                    && mustHaveImmediateResources)
                    throw new Exception("Trying to create a contract without demanded resources!");

                if (pendingContractResources.ContainsKey(offer.cargoType))
                    pendingContractResources[offer.cargoType] += offer.amount;
                else pendingContractResources.Add(offer.cargoType, offer.amount);
                UpdateCargoTrade(offer.cargoType);
            }
        }

        return true;
    }

    public override bool AddContract(FactionTrade.TransportContract transportContract) {
        // Validate that the receiver can buy from the provider
        if (transportContract.provider.faction != transportContract.receiver.faction &&
            !transportContract.provider.faction.factionTrade.tradeSellAgreements
                .ContainsKey(transportContract.receiver.faction))
            throw new Exception("Trying to buy without a trade agreement!");

        if (transportContract.transportOffer.personnel.TotalPopulation() == 0)
            throw new Exception("Tyring to sign a contract that doesn't include any population!");

        base.AddContract(transportContract);
        if (transportContract.provider == this) {
            if (!faction.factionTrade.personnelToHire.ContainsKey(this))
                throw new Exception("Trying to hire personnel that aren't being offered!");
            Population offered = faction.factionTrade.personnelToHire[this].personnel;
            if (HabitationArea.allOccupations.Any(o =>
                offered.Get(o) < transportContract.transportOffer.personnel.Get(o)))
                throw new Exception("Trying to hire personnel that don't exist.");
            HabitationArea.allOccupations.ForEach(o =>
                contractedPersonnel.Add(o, transportContract.transportOffer.personnel.Get(o)));
        } else {
            if (!faction.factionTrade.personnelRequested.ContainsKey(this))
                throw new Exception("Trying to offer personnel that aren't being requested!");
            Population requested = faction.factionTrade.personnelRequested[this].personnel;
            if (HabitationArea.allOccupations.Any(o =>
                requested.Get(o) < transportContract.transportOffer.personnel.Get(o)))
                throw new Exception("Trying to provide a personnel that isn't requested.");
            HabitationArea.allOccupations.ForEach(o =>
                pendingPersonnel.Add(o, transportContract.transportOffer.personnel.Get(o)));
        }
        UpdateJobMarket();
        return true;
    }

    public override void RemoveContract(FactionTrade.Contract contract) {
        base.RemoveContract(contract);
        if (contract is FactionTrade.TradeContract tradeContract) {
            if (tradeContract.provider == this) {
                foreach (var request in tradeContract.cargo.Values) {
                    long extra = contractedCargo[request.cargoType].has - contractedCargo[request.cargoType].wanted +
                        request.amount;
                    var current = contractedCargo.GetValueOrDefault(request.cargoType, (0, 0));
                    if (contractedCargo[request.cargoType].wanted - request.amount == 0)
                        contractedCargo.Remove(request.cargoType);
                    else contractedCargo[request.cargoType] = (current.wanted - request.amount, current.has - extra);
                    if (extra > 0) LoadCargo(extra, request.cargoType);
                    UpdateCargoTrade(request.cargoType);
                }
            } else if (tradeContract.receiver == this) {
                foreach (var request in tradeContract.cargo.Values) {
                    pendingContractResources[request.cargoType] -= request.amount;
                    UpdateCargoTrade(request.cargoType);
                }
            }
        } else if (contract is FactionTrade.TransportContract transportContract) {
            if (transportContract.provider == this) {
                contractedPersonnel.SubtractPopulation(transportContract.transportOffer.personnel);
            } else if (transportContract.receiver == this) {
                pendingPersonnel.SubtractPopulation(transportContract.transportOffer.personnel);
            }
            UpdateJobMarket();
        }
        contractShipsDocked.Remove(contract);
    }

    /// <summary>
    /// Unloads the cargo from the ship to the station based on the contract.
    /// </summary>
    /// <param name="amount">The total amount of cargo to unload in this operation</param>
    /// <returns>True if the contract is finished, false otherwise</returns>
    public bool UnloadContractFromShip(long amount, FactionTrade.TradeContract tradeContract) {
        Assert.AreEqual(tradeContract.receiver, this);
        Assert.IsTrue(tradeContract.provider.IsShip());
        Assert.AreEqual(((Ship)tradeContract.provider).dockedStation, this);
        long cargoToMove = amount;

        foreach (var offer in tradeContract.cargo.Values.ToList()) {
            long toMove = math.min(cargoToMove, offer.amount);
            toMove -= LoadCargoFromUnit(toMove, offer.cargoType, tradeContract.provider, tradeContract);
            cargoToMove -= toMove;
            tradeContract.cargo[offer.cargoType] =
                new FactionTrade.TradeOffer(offer.cargoType, offer.amount - toMove, offer.price);
            faction.TransferCredits((long)(toMove * offer.price), tradeContract.provider.faction);
            pendingContractResources[offer.cargoType] -= toMove;
            if (tradeContract.cargo[offer.cargoType].amount == 0) tradeContract.cargo.Remove(offer.cargoType);
            UpdateCargoTrade(offer.cargoType);
            if (cargoToMove == 0) break;
        }

        if (tradeContract.cargo.Any()) return false;
        faction.factionTrade.RemoveContract(tradeContract);
        return true;
    }

    /// <summary>
    /// Loads cargo from the station onto the ship based on the contract.
    /// </summary>
    /// <param name="amount">The total amount of cargo to load in this operation</param>
    /// <param name="tradeContract">The contract to load</param>
    /// <returns>True if the contract is finished, false otherwise</returns>
    public bool LoadTradeContractToShip(long amount, FactionTrade.TradeContract tradeContract) {
        Assert.AreEqual(tradeContract.provider, this);
        Assert.IsTrue(tradeContract.receiver.IsShip());
        Assert.AreEqual(((Ship)tradeContract.receiver).dockedStation, this);
        long cargoToMove = amount;

        foreach (var offer in tradeContract.cargo.Values.ToList()) {
            long toMove = math.min(math.min(math.min(cargoToMove, offer.amount),
                tradeContract.receiver.GetAvailableCargoSpace(offer.cargoType)), contractedCargo[offer.cargoType].has);
            // Actually update the cargo bays
            base.UseCargo(toMove, offer.cargoType);
            tradeContract.receiver.LoadCargo(toMove, offer.cargoType);
            if (offer.amount - toMove == 0) {
                tradeContract.cargo.Remove(offer.cargoType);
            } else {
                tradeContract.cargo[offer.cargoType] = new FactionTrade.TradeOffer(offer, offer.amount - toMove);
            }

            if (contractedCargo[offer.cargoType].wanted - toMove == 0) {
                contractedCargo.Remove(offer.cargoType);
            } else {
                contractedCargo[offer.cargoType] = (contractedCargo[offer.cargoType].wanted - toMove,
                    contractedCargo[offer.cargoType].has - toMove);
            }
            tradeContract.receiver.faction.TransferCredits((long)(toMove * offer.price), faction);
            cargoToMove -= toMove;
            if (cargoToMove == 0)
                break;
        }

        if (tradeContract.cargo.Any()) return false;
        faction.factionTrade.RemoveContract(tradeContract);
        return true;
    }

    public override long GetAllCargoOfType(CargoBay.CargoType cargoType, bool includeReserved = false) {
        long cargo = 0;
        if (includeReserved) {
            if (cargoType == CargoBay.CargoType.All)
                cargo = reservedCargo.Sum(c => c.Value.has);
            else cargo = !reservedCargo.TryGetValue(cargoType, out (long wanted, long has) value) ? 0 : value.has;
        }
        if (cargoType == CargoBay.CargoType.All)
            return cargo + freeCargo.Sum(c => c.Value.has);
        return !freeCargo.TryGetValue(cargoType, out (long minWanted, long maxWanted, long has) value2)
            ? cargo
            : cargo + value2.has;
    }

    public long GetAllCargoOfType(CargoBay.CargoType cargoType, bool includeReserved, bool includeContract) {
        long cargo = 0;
        if (includeContract) {
            if (cargoType == CargoBay.CargoType.All)
                cargo += contractedCargo.Sum(c => c.Value.has);
            else cargo += !contractedCargo.TryGetValue(cargoType, out (long wanted, long has) value) ? 0 : value.has;
        }
        return cargo + GetAllCargoOfType(cargoType, includeReserved);
    }

    public void ReserveCargo(long amount, CargoBay.CargoType cargoType) {
        long initialAmount = amount - RemoveFreeCargo(amount, cargoType);
        if (reservedCargo.ContainsKey(cargoType)) {
            reservedCargo[cargoType] = (reservedCargo[cargoType].wanted + amount,
                reservedCargo[cargoType].has + initialAmount);
        } else {
            reservedCargo[cargoType] = (amount, initialAmount);
        }

        UpdateCargoTrade(cargoType);
    }

    public void UnReserveCargo(long amount, CargoBay.CargoType cargoType) {
        long extra = math.max(reservedCargo[cargoType].has - reservedCargo[cargoType].wanted + amount, 0);
        AddFreeCargo(extra, cargoType);
        if (reservedCargo[cargoType].wanted > amount) {
            reservedCargo[cargoType] = (reservedCargo[cargoType].wanted - amount, reservedCargo[cargoType].has - extra);
        } else {
            reservedCargo.Remove(cargoType);
        }

        UpdateCargoTrade(cargoType);
    }

    /// <returns>The amount of cargo not removed. Does not modify the cargo bay or resources offered.</returns>
    private long RemoveFreeCargo(long amount, CargoBay.CargoType cargoType) {
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

        return amount - amountUsed;
    }

    /// <summary> Adds the free cargo, does not modify the cargo bay, check if it is over capacity or update resources offered. </summary>
    private void AddFreeCargo(long amount, CargoBay.CargoType cargoType) {
        if (amount == 0) return;

        if (freeCargo.ContainsKey(cargoType))
            freeCargo[cargoType] = (freeCargo[cargoType].minWanted, freeCargo[cargoType].maxWanted,
                freeCargo[cargoType].has + amount);
        else freeCargo[cargoType] = (0, 0, amount);
    }

    public void SetDesiredFreeCargoRange(CargoBay.CargoType cargoType, long minWanted, long maxWanted) {
        var previousCargo = freeCargo.GetValueOrDefault(cargoType, (0, 0, 0));
        freeCargo[cargoType] = (minWanted, maxWanted, previousCargo.has);
        if (minWanted > maxWanted)
            throw new Exception("Illegal arguments given: minWanted must be lower than maxWanted!");

        UpdateCargoTrade(cargoType);
    }

    /// <summary>
    /// Recalculates and modifies the resources requested an offered at the faction level.
    /// </summary>
    public void UpdateCargoTrade(CargoBay.CargoType cargoType) {
        var reserved = reservedCargo.GetValueOrDefault(cargoType, (0, 0));
        var free = freeCargo.GetValueOrDefault(cargoType, (0, 0, 0));
        pendingContractResources.TryAdd(cargoType, 0);
        var factionTrade = faction.factionTrade;
        long cargoRequested = reserved.wanted - reserved.has + free.maxWanted - free.has -
            pendingContractResources[cargoType];
        if (cargoRequested > 0) {
            float price = GetRequestPriceForCargoType(cargoType);
            if (price > 0) {
                factionTrade.resourcesRequested[cargoType][this] = new FactionTrade.TradeOffer(cargoType,
                    cargoRequested, price);
            } else {
                factionTrade.resourcesRequested[cargoType].Remove(this);
            }
        } else {
            factionTrade.resourcesRequested[cargoType].Remove(this);
        }
        long cargoOffered = free.has - free.minWanted;
        if (cargoOffered > 0) {
            float price = GetOfferPriceForCargoType(cargoType);
            if (price < 100) {
                factionTrade.resourcesOffered[cargoType][this] = new FactionTrade.TradeOffer(cargoType,
                    cargoOffered, price);
            } else {
                factionTrade.resourcesOffered[cargoType].Remove(this);
            }
        } else {
            factionTrade.resourcesOffered[cargoType].Remove(this);
        }
    }

    float GetRequestPriceForCargoType(CargoBay.CargoType cargoType) {
        var reserved = reservedCargo.GetValueOrDefault(cargoType, (0, 0));
        var free = freeCargo.GetValueOrDefault(cargoType, (0, 0, 0));
        float priceModifier = 1f;
        // If we are a mining station then we can produce our own resources
        if (cargoType == CargoBay.CargoType.Metal && this is MiningStation) priceModifier = .7f;
        long reservedDiff = reserved.wanted - reserved.has - pendingContractResources[cargoType];
        if (reservedDiff > 0) {
            // We desperately want more cargo for our more essential station functions
            int c = 2000;
            priceModifier *= math.pow((reservedDiff + c) / (float)c, 1.2f) / ((reservedDiff + c) / (float)c);
            // We might want more free cargo so slightly modify it further
            c = 5000;
            return priceModifier * (math.pow((free.minWanted + c) / (float)c, 1.2f)
                / ((free.minWanted + c) / (float)c));
        } else if (free.minWanted > free.has) {
            // We would like to store extra cargo
            const int c = 5000;
            long freeDiff = free.minWanted - free.has - pendingContractResources[cargoType];
            return priceModifier * (math.pow((freeDiff + c) / (float)c, 1.2f) / ((freeDiff + c) / (float)c));
        } else if (free.maxWanted > free.has) {
            // We would buy cargo if it were at a decent price
            const int c = 10000;
            long freeDiff = free.maxWanted - free.has - pendingContractResources[cargoType];
            return priceModifier * (math.pow((freeDiff + c) / (float)c, 1.2f) / ((freeDiff + c) / (float)c));
        } else {
            // We have too much cargo
            return -100;
        }
    }

    float GetOfferPriceForCargoType(CargoBay.CargoType cargoType) {
        var free = freeCargo.GetValueOrDefault(cargoType, (0, 0, 0));
        // We have too little cargo to sell
        if (free.has <= free.minWanted) return 1000000;
        float priceModifier = 1f;
        // Mining stations can sell cargo for cheaper
        if (cargoType == CargoBay.CargoType.Metal && this is MiningStation) priceModifier = .7f;
        long freeWantDiff = free.maxWanted - free.has;
        if (freeWantDiff > 0) {
            // We have a little extra cargo that we can sell
            int c = 1000;
            // Goes from 1.3 to 0.5 and equals 1.0 when c==freeWantDiff
            return priceModifier * (.8f / ((freeWantDiff * .6f / c) + 1) + .5f);
        } else {
            // We have too much cargo and would like to sell it
            int c = 10000;
            // Goes from 1.0 to .5 and equals .75 when c==freeDiff
            long freeDiff = free.has - free.maxWanted;
            priceModifier *= (.5f / ((float)freeDiff / c + 1)) + .5f;
            // We would also like to sell our other cargo
            freeWantDiff = free.maxWanted - free.minWanted;
            c = 20000;
            return priceModifier * (.5f / ((float)freeWantDiff / c + 1)) + .5f;
        }
    }

    public void LoadPersonnelToShip(FactionTrade.TransportContract transportContract) {
        Assert.AreEqual(transportContract.provider, this);
        Assert.IsTrue(transportContract.receiver.IsShip());
        Assert.AreEqual(((Ship)transportContract.receiver).dockedStation, this);

        foreach (HabitationArea habitationArea in
            moduleSystem.Get<HabitationArea>().Where(h => h.IsTransferHabitat())) {
            Population toTransfer = new Population(habitationArea.population);
            toTransfer.Min(transportContract.transportOffer.personnel);
            toTransfer.SubtractPopulation(transportContract.receiver.LoadPopulation(toTransfer));
            habitationArea.population.SubtractPopulation(toTransfer);
            transportContract.transportOffer.personnel.SubtractPopulation(toTransfer);
            transportContract.receiver.faction.TransferCredits(
                transportContract.transportOffer.payment.GetTotalValue(toTransfer), transportContract.provider.faction);
            if (transportContract.transportOffer.personnel.TotalPopulation() == 0) {
                faction.factionTrade.RemoveContract(transportContract);
                break;
            }
        }
        updatePopulation = true;
    }

    public void UnloadPersonnelFromShip(FactionTrade.TransportContract transportContract) {
        Assert.AreEqual(transportContract.receiver, this);
        Assert.IsTrue(transportContract.provider.IsShip());
        Assert.AreEqual(((Ship)transportContract.provider).dockedStation, this);

        foreach (HabitationArea habitationArea in transportContract.provider.moduleSystem.Get<HabitationArea>()
            .Where(h => h.IsTransferHabitat())) {
            Population toTransfer = new Population(habitationArea.population);
            toTransfer.Min(transportContract.transportOffer.personnel);
            foreach (var request in personnelRequests.ToList().OrderBy(a => a.Value.TotalPopulation())) {
                Population amountTransferred = new Population(toTransfer);
                toTransfer.MovePopulationTo(request.Key.population, request.Value);
                amountTransferred.SubtractPopulation(toTransfer);
                habitationArea.population.SubtractPopulation(amountTransferred);
                transportContract.transportOffer.personnel.SubtractPopulation(amountTransferred);
                request.Value.SubtractPopulation(amountTransferred);
                if (request.Value.TotalPopulation() == 0) personnelRequests.Remove(request.Key);
                transportContract.receiver.faction.TransferCredits(
                    transportContract.transportOffer.payment.GetTotalValue(amountTransferred),
                    transportContract.provider.faction);
                if (transportContract.transportOffer.personnel.TotalPopulation() == 0) {
                    break;
                }
            }
            if (transportContract.transportOffer.personnel.TotalPopulation() == 0) {
                faction.factionTrade.RemoveContract(transportContract);
                break;
            }
        }

        updatePopulation = true;
    }

    public void UpdateJobMarket() {
        PopulationFloat hireCost = new PopulationFloat(4, 40, 12, 25);
        Population totalPop = new Population();
        moduleSystem.Get<HabitationArea>().ForEach(h => totalPop.AddPopulation(h.population));
        moduleSystem.Get<PopulationCenter>().ForEach(pc => {
            totalPop.AddPopulation(pc.population);
            // Population centers should keep around half of the civilians that they want
            totalPop.civilians -= (long)math.min(pc.population.civilians,
                pc.GetCapacity() * PopulationCenter.civilianRatio / 2);
        });
        Population requestedPop = new Population();
        personnelRequests.Values.ToList().ForEach(r => requestedPop.AddPopulation(r));
        Population availablePop = new Population(totalPop);
        availablePop.SubtractPopulation(contractedPersonnel);
        availablePop.SubtractPopulation(requestedPop);

        FactionTrade factionTrade = faction.factionTrade;
        if (availablePop.TotalPopulation() == 0) {
            factionTrade.personnelToHire.Remove(this);
        } else {
            PopulationFloat hireCosts = new PopulationFloat(
                GetHireOfferCost(availablePop.civilians, hireCost.civilians, 300),
                GetHireOfferCost(availablePop.pilots, hireCost.pilots, 20),
                GetHireOfferCost(availablePop.engineers, hireCost.engineers, 80),
                GetHireOfferCost(availablePop.marines, hireCost.marines, 60));
            if (factionTrade.personnelToHire.ContainsKey(this)) {
                factionTrade.personnelToHire[this].personnel = availablePop;
                factionTrade.personnelToHire[this].payment = hireCosts;
            } else {
                factionTrade.personnelToHire.Add(this, new FactionTrade.TransportOffer(availablePop, hireCosts));
            }
        }

        if (requestedPop.TotalPopulation() == 0) {
            factionTrade.personnelRequested.Remove(this);
        } else {
            factionTrade.personnelRequested.Remove(this);
            factionTrade.personnelRequested.Add(this, new FactionTrade.TransportOffer(requestedPop, new PopulationFloat(
                GetHireRequestCost(requestedPop.civilians, hireCost.civilians, 50),
                GetHireRequestCost(requestedPop.pilots, hireCost.pilots, 2),
                GetHireRequestCost(requestedPop.engineers, hireCost.engineers, 20),
                GetHireRequestCost(requestedPop.marines, hireCost.marines, 15))));
        }
    }

    /// <summary>
    /// Finds the hire cost of the personnel.
    /// The value goes from baseCost to baseCost/2 as personnel is increased.
    /// The value will equal baseCost * .75 when personnel equals modifier.
    /// </summary>
    private float GetHireOfferCost(long personnel, float baseCost, long modifier) {
        return baseCost * (.5f / ((personnel / (float)modifier) + 1) + .5f);
    }

    private float GetHireRequestCost(long personnelWanted, float baseCost, long modifier) {
        return baseCost * (math.pow((personnelWanted + modifier) / (float)modifier, 1.2f) /
            ((personnelWanted + modifier) / (float)modifier) + .1f);
    }

    public void RequestPersonnel(HabitationArea habitationArea, Population personnelRequested) {
        if (personnelRequested.TotalPopulation() == 0) {
            personnelRequests.Remove(habitationArea);
            return;
        }
        if (personnelRequests.ContainsKey(habitationArea)) {
            if (!personnelRequests[habitationArea].Equals(personnelRequested)) {
                personnelRequests[habitationArea] = personnelRequested;
                updatePopulation = true;
            }
            return;
        }

        Population internalPopulation = new Population();
        moduleSystem.Get<HabitationArea>().Where(h => h.IsTransferHabitat()).ToList().ForEach(h => {
            Population tmp = new Population();
            h.population.MovePopulationTo(tmp, personnelRequested);
            personnelRequested.SubtractPopulation(tmp);
            internalPopulation.AddPopulation(tmp);
        });

        habitationArea.population.AddPopulation(internalPopulation);

        if (personnelRequested.TotalPopulation() > 0) {
            personnelRequests.Add(habitationArea, personnelRequested);
            updatePopulation = true;
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
