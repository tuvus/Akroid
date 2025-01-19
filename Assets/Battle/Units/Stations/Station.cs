using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
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
        ReserchStation = 6,
    }

    public StationScriptableObject stationScriptableObject { get; private set; }

    [System.Serializable]
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
            this.stationCost = stationScriptableObject.cost;
            this.resourcesTypes = new List<CargoBay.CargoTypes>(stationScriptableObject.resourceTypes);
            this.resources = new List<long>(stationScriptableObject.resourceCosts);
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

    public StationAI stationAI { get; protected set; }
    public float repairTime { get; protected set; }
    protected bool built;
    private Random random;
    private float rotationSpeed;

    public Station(BattleObjectData battleObjectData, BattleManager battleManager, StationScriptableObject stationScriptableObject,
        bool built) : base(battleObjectData, battleManager, stationScriptableObject) {
        this.stationScriptableObject = stationScriptableObject;
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

    // protected override float SetupSize() {
    //     return base.SetupSize() * 7 / 10;
    // }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = battleManager.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var blockingObject in battleManager.GetPositionBlockingObjects()) {
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

        foreach (var stationBlueprint in battleManager.stationsInProgress) {
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

    public virtual Ship BuildShip(Ship.ShipClass shipClass, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipClass, cost, undock);
    }

    public virtual Ship BuildShip(ShipScriptableObject shipScriptableObject, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipScriptableObject, cost, undock);
    }

    public virtual Ship BuildShip(ShipType shipType, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipType, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipClass shipClass, long cost = 0, bool? undock = false) {
        ShipScriptableObject shipScriptableObject = BattleManager.Instance.GetShipBlueprint(shipClass).shipScriptableObject;
        return BuildShip(faction, battleManager.GetShipBlueprint(shipClass).shipScriptableObject, shipScriptableObject.unitName, cost,
            undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipClass shipClass, string shipName, long cost = 0, bool? undock = false) {
        return BuildShip(faction, battleManager.GetShipBlueprint(shipClass).shipScriptableObject, shipName, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipScriptableObject shipScriptableObject, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipScriptableObject, shipScriptableObject.unitName, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipType shipType, long cost = 0, bool? undock = false) {
        ShipScriptableObject shipScriptableObject = BattleManager.Instance.GetShipBlueprint(shipType).shipScriptableObject;
        return BuildShip(faction, shipScriptableObject, shipScriptableObject.unitName, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipType shipType, string shipName, long cost = 0, bool? undock = false) {
        ShipScriptableObject shipScriptableObject = BattleManager.Instance.GetShipBlueprint(shipType).shipScriptableObject;
        return BuildShip(faction, shipScriptableObject, shipName, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipScriptableObject shipScriptableObject, string shipName, long cost = 0,
        bool? undock = false) {
        return BuildShip(new BattleObjectData(shipName, position, random.NextFloat(0, 360), faction), shipScriptableObject, cost, undock);
    }

    /// <summary>
    /// Builds a ship from this station and adds it to the faction at factionIndex.
    /// If Undock is true, docks then undocks the ship
    /// If undock is false, docks the ship
    /// If undock is null, it doesn't dock the ship at all.
    /// </summary>
    /// <returns>The newly built ship</returns>
    public virtual Ship BuildShip(BattleObjectData battleObjectData, ShipScriptableObject shipScriptableObject, long cost = 0,
        bool? undock = false) {
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
        if (!built)
            Spawn();
        base.Explode();
    }

    public override void DestroyUnit() {
        base.DestroyUnit();
        BattleManager.Instance.DestroyStation(this);
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
            BattleManager.Instance.BuildStationBlueprint(this);
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

    #region GetMethods
    public bool IsBuilt() {
        return built;
    }

    public int GetRepairAmount() {
        return (int)(stationScriptableObject.repairAmount * faction.GetImprovementModifier(Faction.ImprovementAreas.HullStrength));
    }

    public StationType GetStationType() {
        return stationScriptableObject.stationType;
    }

    #endregion
}
