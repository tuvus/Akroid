using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public int faction;
        public string path;
        public string stationName;
        public Vector2 wantedPosition;
        public float rotation;
        public bool built;

        public StationData(int faction, StationType stationType, string stationName, Vector2 wantedPosition, float rotation, bool built = true) {
            this.faction = faction;
            this.path = "Prefabs/StationPrefabs/" + stationType.ToString();
            this.stationName = stationName;
            this.wantedPosition = wantedPosition;
            this.rotation = rotation;
            this.built = built;
        }

        public StationData(int faction, string path, string stationName, Vector2 wantedPosition, float rotation, bool built = true) {
            this.faction = faction;
            this.path = path;
            this.stationName = stationName;
            this.wantedPosition = wantedPosition;
            this.rotation = rotation;
            this.built = built;
        }
    }

    public StationAI stationAI { get; protected set; }
    private Hanger hanger;
    private CargoBay cargoBay;
    public int repairAmmount;
    public float repairSpeed;
    public float repairTime { get; protected set; }
    protected bool built;

    public virtual void SetupUnit(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, bool built) {
        base.SetupUnit(name, faction, positionGiver, rotation);
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
    }

    protected override float SetupSize() {
        return base.SetupSize() * 6 / 10;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrament(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    bool IPositionConfirmer.ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.stars) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject + star.GetSize() + size) {
                return false;
            }
        }
        foreach (var asteroidField in BattleManager.Instance.asteroidFields) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <= minDistanceFromObject + asteroidField.GetSize() + size) {
                return false;
            }
        }
        foreach (var planet in BattleManager.Instance.planets) {
            if (Vector2.Distance(position, planet.GetPosition()) <= minDistanceFromObject + planet.GetSize() + size) {
                return false;
            }
        }

        foreach (var station in BattleManager.Instance.stations) {
            float enemyBonus = 0;
            if (faction.IsAtWarWithFaction(station.faction))
                enemyBonus = GetMaxWeaponRange() * 2;
            if (Vector2.Distance(position, station.GetPosition()) <= minDistanceFromObject + enemyBonus + station.size + size) {
                return false;
            }
        }

        foreach (var stationBlueprint in BattleManager.Instance.stationBlueprints) {
            float enemyBonus = 0;
            if (faction.IsAtWarWithFaction(stationBlueprint.faction))
                enemyBonus = GetMaxWeaponRange() * 2;
            if (Vector2.Distance(position, stationBlueprint.GetPosition()) <= minDistanceFromObject + enemyBonus + stationBlueprint.size + size) {
                return false;
            }
        }
        return true;
    }

    public override void UpdateUnit(float deltaTime) {
        if (built && IsSpawned()) {
            base.UpdateUnit(deltaTime);
            repairTime -= deltaTime;
            stationAI.UpdateAI(deltaTime);
            if (repairTime <= 0) {
                repairTime += repairSpeed;
            }
        }
    }

    #region StationControlls
    public virtual Ship BuildShip(Ship.ShipClass shipClass, long cost, bool undock = false) {
        return BuildShip(faction.factionIndex, shipClass, cost, undock);
    }

    public virtual Ship BuildShip(int factionIndex, Ship.ShipClass shipClass, long cost, bool undock = false) {
        if (faction.UseCredits(cost)) {
            Ship newShip = BattleManager.Instance.CreateNewShip(new Ship.ShipData(factionIndex, shipClass, shipClass.ToString(), transform.position, Random.Range(0, 360)));
            if (undock) {
                newShip.DockShip(this);
                newShip.UndockShip();
            } else {
                newShip.DockShip(this);
            }
            return newShip;
        }
        return null;
    }

    public override void Explode() {
        hanger.UndockAll();
        if (!built)
            Spawn();
        base.Explode();
    }

    public override void DestroyUnit() {
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
        repairTime += repairSpeed * (amount - leftOver) / repairAmmount;
        return leftOver;
    }

    public bool BuildStation() {
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
        return (int)(repairAmmount * faction.HealthModifier);
    }
    #endregion

}
