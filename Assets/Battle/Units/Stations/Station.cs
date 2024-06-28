using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using static Ship;
using static System.Collections.Specialized.BitVector32;
using Random = UnityEngine.Random;

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

    public StationType stationType;

    public struct StationData {
        public Faction faction;
        public StationScriptableObject stationScriptableObject;
        public string stationName;
        public Vector2 wantedPosition;
        public float rotation;
        public bool built;

        public StationData(Faction faction, StationScriptableObject stationScriptableObject, string stationName, Vector2 wantedPosition, float rotation, bool built = true) {
            this.faction = faction;
            this.stationScriptableObject = stationScriptableObject;
            this.stationName = stationName;
            this.wantedPosition = wantedPosition;
            this.rotation = rotation;
            this.built = built;
        }
    }

    [System.Serializable]
    public class StationBlueprint {
        public string name;
        public int factionIndex;
        public StationScriptableObject stationScriptableObject;
        public long stationCost;
        public List<CargoBay.CargoTypes> resourcesTypes;
        public List<long> resources;
        public long totalResourcesRequired;

        private StationBlueprint(int factionIndex, StationScriptableObject stationScriptableObject, string name) {
            this.factionIndex = factionIndex;
            this.stationScriptableObject = stationScriptableObject;
            this.name = name;
            this.stationCost = stationScriptableObject.cost;
            this.resourcesTypes = new List<CargoBay.CargoTypes>(stationScriptableObject.resourceTypes);
            this.resources = new List<long>(stationScriptableObject.resourceCosts);
            for (int i = 0; i < resources.Count; i++) {
                totalResourcesRequired += resources[i];
            }
        }

        public StationBlueprint CreateStationBlueprint(int factionIndex, string name = null) {
            if (name == null)
                name = this.name;
            return new StationBlueprint(factionIndex, stationScriptableObject, name);
        }
    }


    public StationAI stationAI { get; protected set; }
    private Hanger hanger;
    private CargoBay cargoBay;
    public int repairAmount;
    public float repairSpeed;
    public float rotationSpeed;
    public float repairTime { get; protected set; }
    protected bool built;

    public virtual void SetupUnit(BattleManager battleManager, string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, bool built, float timeScale, UnitScriptableObject unitScriptableObject) {
        base.SetupUnit(battleManager, name, faction, positionGiver, rotation, timeScale, unitScriptableObject);
        stationAI = GetComponent<StationAI>();
        hanger = GetComponentInChildren<Hanger>();
        cargoBay = GetComponentInChildren<CargoBay>();
        stationAI.SetupStationAI(this);
        hanger.SetupHanger(this);
        this.built = built;
        if (!built) {
            faction.AddStationBlueprint(this);
            health = 0;
            ActivateColliders(false);
            spriteRenderer.color = new Color(.3f, 1f, .3f, .5f);
            for (int i = 0; i < turrets.Count; i++) {
                turrets[i].ShowTurret(false);
            }
        } else {
            faction.AddStation(this);
            Spawn();
            faction.GetFactionAI().OnStationBuilt(this);
        }
        rotationSpeed *= Random.Range(.5f, 1.5f);
        if (Random.Range(-1,1) < 0) {
            rotationSpeed *= -1;
        }
    }

    protected override float SetupSize() {
        return base.SetupSize() * 7 / 10;
    }

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
                if (Vector2.Distance(position, station.GetPosition()) <= minDistanceFromObject + enemyBonus + station.GetSize() + GetSize()) {
                    return false;
                }
            } else if (Vector2.Distance(position, blockingObject.GetPosition()) <= minDistanceFromObject + GetSize() + blockingObject.GetSize()) {
                return false;
            }
        }

        foreach (var stationBlueprint in battleManager.stationsInProgress) {
            float enemyBonus = 0;
            if (faction.IsAtWarWithFaction(stationBlueprint.faction))
                enemyBonus = GetMaxWeaponRange() * 2;
            if (Vector2.Distance(position, stationBlueprint.GetPosition()) <= minDistanceFromObject + enemyBonus + stationBlueprint.GetSize() + GetSize()) {
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
            Profiler.BeginSample("UpdateRotation");
            SetRotation(transform.eulerAngles.z + rotationSpeed * deltaTime);
            Profiler.EndSample();
            stationAI.UpdateAI(deltaTime);
            if (repairTime <= 0) {
                repairTime += repairSpeed;
            }
        }
    }

    #region StationControlls
    public virtual Ship BuildShip(Ship.ShipClass shipClass, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipClass, cost, undock);
    }

    public virtual Ship BuildShip(ShipType shipType, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipType, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipClass shipClass, long cost = 0, bool? undock = false) {
        ShipScriptableObject shipScriptableObject = BattleManager.Instance.GetShipBlueprint(shipClass).shipScriptableObject;
        return BuildShip(faction, BattleManager.Instance.GetShipBlueprint(shipClass).shipScriptableObject, shipScriptableObject.unitName, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipType shipType, long cost = 0, bool? undock = false) {
        ShipScriptableObject shipScriptableObject = BattleManager.Instance.GetShipBlueprint(shipType).shipScriptableObject;
        return BuildShip(faction, shipScriptableObject, shipScriptableObject.unitName, cost, undock);
    }

    public virtual Ship BuildShip(Faction faction, ShipScriptableObject shipScriptableObject, string shipName, long cost = 0, bool? undock = false) {
        return BuildShip(faction, new Ship.ShipData(faction, shipScriptableObject, shipName, transform.position, Random.Range(0, 360)), cost, undock);
    }

    public virtual Ship BuildShip(ShipData shipData, long cost = 0, bool? undock = false) {
        return BuildShip(faction, shipData, cost, undock);
    }

    /// <summary>
    /// Builds a ship from this station and adds it to the faction at factionIndex.
    /// If Undock is true, docks then undocks the ship
    /// If undock is false, docks the ship
    /// If undock is null, it doesn't dock the ship at all.
    /// </summary>
    /// <param name="factionIndex"></param>
    /// <param name="shipData"></param>
    /// <param name="cost"></param>
    /// <param name="undock"></param>
    /// <returns></returns>
    public virtual Ship BuildShip(Faction faction, ShipData shipData, long cost = 0, bool? undock = false) {
        Ship newShip = BattleManager.Instance.CreateNewShip(new ShipData(faction,shipData));
        if (undock == null) { 
        } else if ((bool)undock) {
            newShip.DockShip(this);
            newShip.UndockShip();
        } else {
            newShip.DockShip(this);
        }
        return newShip;
    }

    public override void Explode() {
        hanger.UndockAll();
        if (!built)
            Spawn();
        base.Explode();
    }

    public override void DestroyUnit() {
         base.DestroyUnit();
        BattleManager.Instance.DestroyStation(this);
    }

    public bool DockShip(Ship ship) {
        if (IsSpawned() && IsBuilt() && hanger.DockShip(ship)) {
            ship.transform.position = transform.position;
            ship.transform.eulerAngles = new Vector3(0, 0, 0);
            return true;
        }
        return false;
    }

    public void UndockShip(Ship ship, float rotation) {
        hanger.RemoveShip(ship);
        Vector2 undockPos = Calculator.GetPositionOutOfAngleAndDistance(rotation, GetSize() + ship.GetSize());
        ship.transform.position = new Vector2(transform.position.x + undockPos.x, transform.position.y + undockPos.y);
        ship.transform.eulerAngles = new Vector3(0, 0, rotation);
    }

    public int RepairUnit(Unit unit, int amount) {
        int leftOver = unit.Repair(amount);
        repairTime += repairSpeed * (amount - leftOver) / repairAmount;
        return leftOver;
    }

    public virtual bool BuildStation() {
        if (!built) {
            BattleManager.Instance.BuildStationBlueprint(this);
            faction.RemoveStationBlueprint(this);
            faction.AddStation(this);
            built = true;
            health = GetMaxHealth();
            for (int i = 0; i < turrets.Count; i++) {
                turrets[i].ShowTurret(true);
            }
            ShowUnit(true);
            spriteRenderer.color = new Color(1, 1, 1, 1);
            Spawn();
            GetUnitSelection().UpdateFactionColor();
            faction.GetFactionAI().OnStationBuilt(this);
            return true;
        }
        return false;
    }
    #endregion

    #region GetMethods
    public override bool Destroyed() {
        if (GetHealth() > 0) {
            return false;
        } else {
            return true;
        }
    }

    public CargoBay GetCargoBay() {
        return cargoBay;
    }

    public Hanger GetHanger() {
        return hanger;
    }

    public bool IsBuilt() {
        return built;
    }

    public int GetRepairAmmount() {
        return (int)(repairAmount * faction.GetImprovementModifier(Faction.ImprovementAreas.HullStrength));
    }
    #endregion

}
