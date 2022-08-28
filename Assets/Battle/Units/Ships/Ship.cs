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
    public enum ShipAction {
        Idle,
        Rotate,
        Move,
        MoveRotate,
        Dock,
        DockMove,
        DockRotate,
    }

    public ShipAI shipAI { get; private set; }
    [SerializeField] private ShipClass shipClass;
    [SerializeField] private ShipType shipType;
    private CargoBay cargoBay;
    private ResearchEquiptment researchEquiptment;
    private List<Thruster> thrusters;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float combatRotation;
    public Station dockedStation;
    private float mass;
    private float thrust;

    public ShipAction shipAction;
    [SerializeField] private float targetRotation;
    [SerializeField] private Vector2 movePosition;
    [SerializeField] private Station targetStation;

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
        SetIdle();
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
            Profiler.BeginSample("Movement");
            UpdateMovement();
            Profiler.EndSample();
            shipAI.UpdateAI(Time.fixedDeltaTime * BattleManager.Instance.timeScale);
        }
    }

    void UpdateMovement() {
        velocity = Vector2.zero;
        if (dockedStation != null && (shipAction == ShipAction.MoveRotate || shipAction == ShipAction.DockRotate)) {
            UndockShip(targetRotation);
        } else if (dockedStation != null) {
            return;
        } else if ((targetStation == null || !targetStation.IsSpawned()) && (shipAction == ShipAction.Dock || shipAction == ShipAction.DockMove || shipAction == ShipAction.DockRotate)) {
            SetIdle();
        }
        if ((shipAction == ShipAction.Move || shipAction == ShipAction.DockMove) && targetRotation != transform.rotation.z) {
            SetThrusters(false);
            if (shipAction == ShipAction.Move) {
                SetMovePosition(movePosition);
            } else if (shipAction == ShipAction.DockMove) {
                SetDockTarget(targetStation);
            }
        }
        if (shipAction == ShipAction.Rotate || shipAction == ShipAction.MoveRotate || shipAction == ShipAction.DockRotate) {
            float localRotation = Calculator.GetLocalTargetRotation(transform.eulerAngles.z, targetRotation);
            if (Mathf.Abs(localRotation) <= GetTurnSpeed() * Time.fixedDeltaTime * BattleManager.Instance.timeScale) {
                SetRotation(targetRotation);
                if (shipAction == ShipAction.Rotate) {
                    shipAction = ShipAction.Idle;
                } else if (shipAction == ShipAction.MoveRotate) {
                    shipAction = ShipAction.Move;
                } else if (shipAction == ShipAction.DockRotate) {
                    shipAction = ShipAction.DockMove;
                }
            } else if (localRotation > 0) {
                SetRotation(transform.eulerAngles.z + (GetTurnSpeed() * Time.fixedDeltaTime * BattleManager.Instance.timeScale));
            } else {
                SetRotation(transform.eulerAngles.z - (GetTurnSpeed() * Time.fixedDeltaTime * BattleManager.Instance.timeScale));
            }
        }
        if (shipAction == ShipAction.Move || shipAction == ShipAction.DockMove) {
            float distance = Calculator.GetDistanceToPosition((Vector2)transform.position - movePosition);
            float thrust = GetThrust() * Time.fixedDeltaTime * BattleManager.Instance.timeScale / GetMass();
            if (distance <= thrust + 2) {
                transform.position = movePosition;
                if (shipAction == ShipAction.Move) {
                    shipAction = ShipAction.Idle;
                    SetThrusters(false);
                } else if (shipAction == ShipAction.DockMove) {
                    SetThrusters(false);
                    shipAction = ShipAction.Dock;
                }
            } else {
                transform.Translate(Vector2.up * thrust);
                SetThrusters(true);
                velocity = transform.up * GetThrust() / GetMass();
            }
            position = transform.position;
        }
        if (shipAction == ShipAction.Dock) {
            DockShip(targetStation);
            shipAction = ShipAction.Idle;
        }
    }

    #region ShipControlls
    public void SetIdle() {
        shipAction = ShipAction.Idle;
        SetThrusters(false);
        faction.GetFactionAI().AddIdleShip(this);
    }

    public void SetTargetRotate(float rotation) {
        if (shipAction == ShipAction.Idle) {
            faction.GetFactionAI().RemoveIdleShip(this);
        }
        SetThrusters(false);
        shipAction = ShipAction.Rotate;
        this.targetRotation = rotation;
    }

    public void SetTargetRotate(Vector2 position) {
        SetTargetRotate(Calculator.GetAngleOutOfTwoPositions(GetPosition(), position));
    }

    public void SetTargetRotate(Vector2 position, float extraAngle) {
        SetTargetRotate(Calculator.ConvertTo360DegRotation(Calculator.GetAngleOutOfTwoPositions(GetPosition(), position) + extraAngle));
    }

    public void SetMovePosition(Vector2 position) {
        if (shipAction == ShipAction.Idle) {
            faction.GetFactionAI().RemoveIdleShip(this);
        }
        this.movePosition = position;
        SetTargetRotate(position);
        shipAction = ShipAction.MoveRotate;
    }

    public void SetMovePosition(Vector2 position, float distanceFromPosition) {
        SetMovePosition(Vector2.MoveTowards(GetPosition(), position, Vector2.Distance(GetPosition(), position) - distanceFromPosition));
    }

    public void SetDockTarget(Station targetStation) {
        if (shipAction == ShipAction.Idle) {
            faction.GetFactionAI().RemoveIdleShip(this);
        }
        this.targetStation = targetStation;
        SetMovePosition(targetStation.GetPosition(), GetSize() + targetStation.GetSize());
        shipAction = ShipAction.DockRotate;
    }

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
        if (shipAction == ShipAction.Idle)
            faction.GetFactionAI().RemoveIdleShip(this);
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

    public void OnCollisionStay2D(Collision2D collision) {
        if (shipAction == ShipAction.Move || shipAction == ShipAction.MoveRotate) {
            SetMovePosition(movePosition);
        }
    }

    public bool IsIdle() {
        return shipAction == ShipAction.Idle && (shipAI.commands.Count == 0 || shipAI.currentCommandType == UnitAICommand.CommandType.Idle);
    }

    #endregion

}