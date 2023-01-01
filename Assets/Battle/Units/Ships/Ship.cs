using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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
        MoveLateral,
        Dock,
        DockMove,
        DockRotate,
    }

    public ShipAI shipAI { get; private set; }
    public Fleet fleet;
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
    private bool thrusting;
    private float maxSetSpeed;

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
        public int factionIndex;
        public ShipClass shipClass;
        public string shipName;
        public long shipCost;
        public List<CargoBay.CargoTypes> resourcesTypes;
        public List<long> resources;

        public ShipBlueprint(int factionIndex, ShipClass shipClass, string shipName, long shipCost, List<CargoBay.CargoTypes> resourcesTypes, List<long> resources) {
            this.factionIndex = factionIndex;
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

    public override void SetupUnit(string shipName, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, float particleSpeed) {
        faction.AddShip(this);
        thrusters = new List<Thruster>(GetComponentsInChildren<Thruster>());
        foreach (var thruster in thrusters) {
            thruster.SetupThruster();
        }
        base.SetupUnit(shipName, faction, positionGiver, rotation, particleSpeed);
        shipAI = GetComponent<ShipAI>();
        cargoBay = GetComponentInChildren<CargoBay>();
        if (IsScienceShip()) {
            researchEquiptment = GetComponentInChildren<ResearchEquiptment>();
            researchEquiptment.SetupResearchEquiptment(this);
        }
        SetupThrusters();
        shipAI.SetupShipAI(this);
        mass = size * 100;
        Spawn();
        shipAction = ShipAction.Move;
        SetIdle();
    }

    public void SetupThrusters() {
        thrusting = false;
        thrust = 0;
        for (int i = 0; i < thrusters.Count; i++) {
            thrust += thrusters[i].thrustSpeed * faction.ThrusterPowerModifier;
        }
    }

    public override void UpdateUnit(float deltaTime) {
        base.UpdateUnit(deltaTime);
        if (IsSpawned()) {
            Profiler.BeginSample(shipAction.ToString());
            UpdateMovement(deltaTime);
            Profiler.EndSample();
            shipAI.UpdateAI(deltaTime);
        }
    }

    protected override void FindEnemies() {
        if (fleet == null)
            base.FindEnemies();
    }

    void UpdateMovement(float deltaTime) {
        if (shipAction == ShipAction.Idle) {
            return;
        }
        velocity = Vector2.zero;
        if ((shipAction == ShipAction.Dock || shipAction == ShipAction.DockMove || shipAction == ShipAction.DockRotate) && (targetStation == null || !targetStation.IsSpawned())) {
            SetIdle();
        }
        if (shipAction == ShipAction.Move || shipAction == ShipAction.DockMove) {
            targetRotation = Calculator.GetAngleOutOfTwoPositions(GetPosition(), movePosition);
            if (Mathf.Abs(transform.eulerAngles.z - targetRotation) > 0.00001) {
                if (shipAction == ShipAction.Move) {
                    shipAction = ShipAction.MoveRotate;
                } else if (shipAction == ShipAction.DockMove) {
                    shipAction = ShipAction.DockRotate;
                }
            }
        }
        if (shipAction == ShipAction.Rotate || shipAction == ShipAction.MoveRotate || shipAction == ShipAction.DockRotate) {
            float localRotation = Calculator.GetLocalTargetRotation(transform.eulerAngles.z, targetRotation);
            if (Mathf.Abs(localRotation) <= GetTurnSpeed() * deltaTime) {
                SetRotation(targetRotation);
                if (shipAction == ShipAction.Rotate) {
                    shipAction = ShipAction.Idle;
                    SetThrusters(false);
                } else if (shipAction == ShipAction.MoveRotate) {
                    shipAction = ShipAction.Move;
                    SetThrusters(true);
                } else if (shipAction == ShipAction.DockRotate) {
                    shipAction = ShipAction.DockMove;
                    SetThrusters(true);
                }
            } else if (localRotation > 0) {
                SetRotation(transform.eulerAngles.z + (GetTurnSpeed() * deltaTime));
                SetThrusters(false);
                return;
            } else {
                SetRotation(transform.eulerAngles.z - (GetTurnSpeed() * deltaTime));
                SetThrusters(false);
                return;
            }
        }

        if (shipAction == ShipAction.Move || shipAction == ShipAction.DockMove) {
            float distance = Calculator.GetDistanceToPosition((Vector2)transform.position - movePosition);
            float speed = math.min(maxSetSpeed, GetSpeed());
            float thrust = speed * deltaTime;
            if (shipAction == ShipAction.DockMove && distance - thrust < GetSize() + targetStation.GetSize()) {
                DockShip(targetStation);
                return;
            }
            if (distance <= thrust + 2) {
                transform.position = movePosition;
                position = movePosition;
                if (shipAction == ShipAction.Move) {
                    shipAction = ShipAction.Idle;
                    SetThrusters(false);
                } else if (shipAction == ShipAction.DockMove) {
                    shipAction = ShipAction.Dock;
                }
            } else {
                transform.Translate(Vector2.up * thrust);
                velocity = transform.up * math.min(maxSetSpeed, GetSpeed());
                position = transform.position;
                return;
            }
        }
        if (shipAction == ShipAction.Dock) {
            DockShip(targetStation);
        }
        if (shipAction == ShipAction.MoveLateral) {
            SetThrusters(false);
            float speed = math.min(maxSetSpeed, GetSpeed()) / 2;
            if (Vector2.Distance(GetPosition(), movePosition) <= speed * deltaTime) {
                transform.position = movePosition;
                position = movePosition;
                SetIdle();
                return;
            } else {
                Vector3 temp = Vector2.MoveTowards(GetPosition(), movePosition, speed * deltaTime) - GetPosition();
                transform.Translate(temp, Space.World);
                position = transform.position;
                return;
            }
        }
    }

    #region ShipControlls
    public void SetIdle() {
        if (shipAction != ShipAction.Idle) {
            faction.GetFactionAI().AddIdleShip(this);
        }
        shipAction = ShipAction.Idle;
        velocity = Vector2.zero;
        SetThrusters(false);
    }

    public void SetTargetRotate(float rotation) {
        if (shipAction == ShipAction.Idle) {
            faction.GetFactionAI().RemoveIdleShip(this);
        }
        if (dockedStation != null) {
            SetIdle();
            return;
        }
        shipAction = ShipAction.Rotate;
        this.targetRotation = rotation;
    }

    public void SetTargetRotate(Vector2 position) {
        SetTargetRotate(Calculator.GetAngleOutOfTwoPositions(GetPosition(), position));
    }

    public void SetTargetRotate(Vector2 position, float extraAngle) {
        SetTargetRotate(Calculator.ConvertTo360DegRotation(Calculator.GetAngleOutOfTwoPositions(GetPosition(), position) + extraAngle));
    }

    public void SetLateralMovePosition(Vector2 position) {
        if (shipAction == ShipAction.Idle) {
            faction.GetFactionAI().RemoveIdleShip(this);
        }
        if (dockedStation != null)
            UndockShip(position);
        shipAction = ShipAction.MoveLateral;
        this.movePosition = position;
    }

    public void SetMovePosition(Vector2 position) {
        if (Vector2.Distance(GetPosition(), position) < GetSize() * 2) {
            SetLateralMovePosition(position);
            return;
        }
        if (shipAction == ShipAction.Idle) {
            faction.GetFactionAI().RemoveIdleShip(this);
        }
        if (dockedStation != null)
            UndockShip(position);
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
        if (dockedStation == targetStation) {
            SetIdle();
            return;
        }
        if (Vector2.Distance(transform.position, targetStation.GetPosition()) < GetSize() + targetStation.GetSize()) {
            DockShip(targetStation);
            return;
        }
        this.targetStation = targetStation;
        SetMovePosition(targetStation.GetPosition(), GetSize() + targetStation.GetSize());
        shipAction = ShipAction.DockRotate;
    }

    public void SetMaxSpeed() {
        maxSetSpeed = GetSpeed();
    }

    public void SetMaxSpeed(float maxspeed) {
        maxSetSpeed = maxspeed;
    }

    public float GetThrust() {
        return thrust;
    }

    public float GetSpeed() {
        return thrust / GetMass();
    }

    public void SetThrusters(bool trueOrFalse) {
        if (trueOrFalse != thrusting) {
            thrusting = trueOrFalse;
            for (int i = 0; i < thrusters.Count; i++) {
                if (thrusting) {
                    thrusters[i].BeginThrust();
                } else {
                    thrusters[i].EndThrust();
                }
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
        if (fleet != null) {
            fleet.RemoveShip(this);
        }
        BattleManager.Instance.DestroyShip(this);
    }

    public void DockShip(Station station) {
        if (station.DockShip(this)) {
            SetIdle();
            ShowUnit(false);
            dockedStation = station;
        }
    }

    public void UndockShip() {
        UndockShip(UnityEngine.Random.Range(0f, 360f));
        velocity = Vector2.zero;
    }

    public void UndockShip(Vector2 position) {
        UndockShip(Calculator.GetAngleOutOfTwoPositions(transform.position, position));
    }

    public void UndockShip(float angle) {
        dockedStation.UndockShip(this, angle);
        position = transform.position;
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
    public Vector2 GetTargetMovePosition() {
        return movePosition;
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

    public override List<Unit> GetEnemyUnitsInRange() {
        if (fleet != null) {
            return fleet.enemyUnitsInRange;
        }
        return base.GetEnemyUnitsInRange();
    }

    public override List<float> GetEnemyUnitsInRangeDistance() {
        if (fleet != null) {
            return fleet.enemyUnitsInRangeDistance;
        }
        return base.GetEnemyUnitsInRangeDistance();
    }

    public ResearchEquiptment GetResearchEquiptment() {
        return researchEquiptment;
    }

    public bool IsIdle() {
        return shipAction == ShipAction.Idle && (shipAI.commands.Count == 0 || shipAI.commands[0].commandType == Command.CommandType.Idle);
    }

    public override void SetParticleSpeed(float speed) {
        base.SetParticleSpeed(speed);
        foreach (var thruster in thrusters) {
            thruster.SetParticleSpeed(speed);
        }
    }

    [ContextMenu("GetShipThrust")]
    public void GetShipThrust() {
        float thrust = 0;
        spriteRenderer = GetComponent<SpriteRenderer>();
        foreach (var thruster in GetComponentsInChildren<Thruster>()) {
            thrust += thruster.thrustSpeed;
        }

        mass = SetupSize() * 100;
        thrust /= mass;
        print(unitName + "Thrust:" + thrust);
    }
    #endregion

}