using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Ship : Unit {
    public enum ShipClass {
        Transport,
        HeavyTransport,
        Aria,
        Lancer,
        Aterna,
        StationBuilder,
        Zarrack,
    }
    public enum ShipType {
        Transport,
        Construction,
        Research,
        Fighter,
        Frigate,
        Cruiser,
        Dreadnaught,
    }
    public enum ThrustDirections {
        forwards = 1,
        backwards = 2,
        left = 3,
        right = 4,
    }
    public enum RotationDircetions {
        left = 1,
        right = 2,
    }

    public ShipAI shipAI { get; private set; }
    [SerializeField] private ShipClass shipClass;
    [SerializeField] private ShipType shipType;
    private CargoBay cargoBay;
    private ResearchEquiptment researchEquiptment;
    private List<Thruster> thrusters;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float combatRotation;
    public Station dockedStation { get; private set; }
    private float mass;
    private float thrust;

    public struct ShipData {
        public int faction;
        public ShipClass shipClass;
        public string shipName;
        public Vector2 position;
        public float rotation;

        public ShipData(int faction, ShipClass shipClass, string shipName, Vector2 position, float rotation) {
            this.faction = faction;
            this.shipClass = shipClass;
            this.shipName = shipName;
            this.position = position;
            this.rotation = rotation;
        }
    }

    [System.Serializable]
    public class ShipBlueprint {
        public ShipClass shipClass;
        public string shipName;
        public int shipCost;
        public List<CargoBay.CargoTypes> resourcesTypes;
        public List<float> resources;

        public ShipBlueprint(ShipClass shipClass, string shipName, int shipCost, List<CargoBay.CargoTypes> resourcesTypes, List<float> resources) {
            this.shipClass = shipClass;
            this.shipName = shipName;
            this.shipCost = shipCost;
            this.resourcesTypes = resourcesTypes;
            this.resources = resources;
        }

        public bool IsFinished() {
            return resources.Count == 0;
        }
    }

    public override void SetupUnit(string shipName, Faction faction, BattleManager.PositionGiver positionGiver, float rotation) {
        faction.AddShip(this);
        base.SetupUnit(shipName, faction, positionGiver, rotation);
        shipAI = GetComponent<ShipAI>();
        cargoBay = GetComponentInChildren<CargoBay>();
        if (IsScienceShip()) {
            researchEquiptment = GetComponentInChildren<ResearchEquiptment>();
            researchEquiptment.SetupResearchEquiptment(this);
        }
        thrusters = new List<Thruster>(GetComponentsInChildren<Thruster>());
        SetupThrusters();
        shipAI.SetupShipAI(this);
        foreach (var thruster in thrusters) {
            thruster.SetupThruster();
        }

        mass = size * 100;
        Spawn();
    }

    public void SetupThrusters() {
        thrust = 0;
        for (int i = 0; i < thrusters.Count; i++) {
            thrust += thrusters[i].thrustSpeed * faction.ThrusterPowerModifier;
        }
    }

    public override void UpdateUnit() {
        base.UpdateUnit();
        if (IsSpawned()) {
            if (thrusters[0].IsThrusting())
                velocity = transform.up * GetThrust() / GetMass();
            shipAI.UpdateAI(Time.fixedDeltaTime * BattleManager.Instance.timeScale);

            if (dockedStation != null)
                return;
            for (int i = 0; i < thrusters.Count; i++) {
                Profiler.BeginSample("Thruster" + i);
                thrusters[i].UpdateThruster();
                Profiler.EndSample();
            }
        }
    }

    #region ShipControlls
    public float GetThrust() {
        return thrust;
    }

    public void SetThrusters(bool trueOrFalse) {
        for (int i = 0; i < thrusters.Count; i++) {
            if (trueOrFalse) {
                thrusters[i].BeginThrust();
            } else {
                thrusters[i].EndThrust();
            }
        }
    }

    public override void ShowUnit(bool show) {
        base.ShowUnit(show);
        foreach (var turret in turrets) {
            turret.ShowTurret(show);
        }
    }

    public override void ActivateColliders(bool active) {
        base.ActivateColliders(active);
    }

    public override void DestroyUnit() {
        BattleManager.Instance.DestroyShip(this);
    }

    public void DockShip(Station station) {
        if (station.DockShip(this)) {
            ShowUnit(false);
            dockedStation = station;
        }
    }

    public void UndockShip() {
        UndockShip(Random.Range(0f, 360f));
    }

    public void UndockShip(Vector2 position) {
        UndockShip(Calculator.GetAngleOutOfTwoPositions(transform.position, position));
    }

    public void UndockShip(float angle) {
        dockedStation.UndockShip(this, angle);
        ShowUnit(true);
        dockedStation = null;
    }
    #endregion

    #region GetMethods
    public float GetTurnSpeed() {
        return turnSpeed;
    }

    public float GetCombatRotation() {
        return combatRotation;
    }

    public override bool Destroyed() {
        return GetHealth() <= 0;
    }

    public ShipClass GetShipClass() {
        return shipClass;
    }

    public ShipType GetShipType() {
        return shipType;
    }

    public override bool IsSelectable() {
        return base.IsSelectable() && dockedStation == null;
    }

    public override bool IsTargetable() {
        return base.IsTargetable() && dockedStation == null;
    }

    public bool IsCombatShip() {
        return shipType == ShipType.Fighter || shipType == ShipType.Cruiser || shipType == ShipType.Frigate || shipType == ShipType.Dreadnaught;
    }

    public bool IsTransportShip() {
        return shipType == ShipType.Transport;
    }

    public bool IsConstructionShip() {
        return shipType == ShipType.Construction;
    }

    public bool IsScienceShip() {
        return shipType == ShipType.Research;
    }

    public float GetMass() {
        return mass;
    }

    public CargoBay GetCargoBay() {
        return cargoBay;
    }

    public ResearchEquiptment GetResearchEquiptment() {
        return researchEquiptment;
    }

    #endregion

}