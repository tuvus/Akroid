using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

public class Ship : Unit {
    public ShipScriptableObject ShipScriptableObject { get; private set; }

    public enum ShipClass {
        Transport,
        HeavyTransport,
        Aria,
        Lancer,
        Aterna,
        StationBuilder,
        Zarrack,
        Eletera,
    }
    public enum ShipType {
        Civilian,
        Transport,
        Construction,
        Research,
        Fighter,
        Frigate,
        Cruiser,
        Dreadnaught,
        GasCollector,
        Colonizer,
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
        MoveAndRotate,
    }

    public ShipAI shipAI { get; private set; }
    public Fleet fleet;
    [field: SerializeField] private ShipClass shipClass;
    [field: SerializeField] private ShipType shipType;
    public float turnSpeed;
    public float combatRotation;
    public Station dockedStation;
    private float mass;
    private float thrust;
    private bool thrusting;
    private float maxSetSpeed;

    public ShipAction shipAction;
    [SerializeField] private float targetRotation;
    [SerializeField] private Vector2 movePosition;
    [SerializeField] private Station targetStation;
    private float timeUntilCheckRotation;
    private float checkRotationSpeed = 0.2f;

    public struct ShipData {
        public Faction faction;
        public ShipScriptableObject shipScriptableObject;
        public string shipName;
        public Vector2 position;
        public float rotation;

        public ShipData(Faction faction, ShipScriptableObject shipScriptableObject, string shipName, Vector2 position, float rotation) {
            this.faction = faction;
            this.shipScriptableObject = shipScriptableObject;
            this.shipName = shipName;
            this.position = position;
            this.rotation = rotation;
        }

        public ShipData(Faction faction, ShipData shipData) {
            this.faction = faction;
            this.shipScriptableObject = shipData.shipScriptableObject;
            this.shipName = shipData.shipName;
            this.position = shipData.position;
            this.rotation = shipData.rotation;
        }
    }

    [System.Serializable]
    public class ShipBlueprint {
        public string name;
        public Faction faction;
        public ShipScriptableObject shipScriptableObject;

        protected ShipBlueprint(Faction faction, ShipScriptableObject shipScriptableObject, string name = null) {
            if (name == null)
                this.name = shipScriptableObject.name;
            else
                this.name = name;
            this.faction = faction;
            this.shipScriptableObject = shipScriptableObject;
        }
    }

    [System.Serializable]
    public class ShipConstructionBlueprint : ShipBlueprint {
        /// <summary> The credit cost of constructing the blueprint. </summary>
        public long cost;
        /// <summary>
        /// The amount of resources to be put into the blueprint before it can be constructed.
        /// This value may be reduced throughout construction.
        /// </summary>
        public Dictionary<CargoBay.CargoTypes, long> resourceCosts;
        public long totalResourcesRequired { get; private set; }

        public ShipConstructionBlueprint(Faction faction, ShipBlueprint shipBlueprint, String name = null) : base(faction, shipBlueprint.shipScriptableObject, name) {
            cost = shipScriptableObject.cost;
            resourceCosts = new Dictionary<CargoBay.CargoTypes, long>();
            totalResourcesRequired = 0;
            for (int i = 0; i < shipScriptableObject.resourceTypes.Count; i++) {
                resourceCosts.Add(shipScriptableObject.resourceTypes[i], shipScriptableObject.resourceCosts[i]);
                totalResourcesRequired += shipScriptableObject.resourceCosts[i];
            }
        }

        public long GetTotalResourcesLeftToUse() {
            return resourceCosts.Sum(c => c.Value);
        }

        public bool IsFinished() {
            return resourceCosts.Count == 0;
        }

        public Faction GetFaction() {
            return faction;
        }
    }
    public override void SetupUnit(BattleManager battleManager, string shipName, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, float particleSpeed, UnitScriptableObject unitScriptableObject) {
        this.ShipScriptableObject = (ShipScriptableObject)unitScriptableObject;
        faction.AddShip(this);
        base.SetupUnit(battleManager, shipName, faction, positionGiver, rotation, particleSpeed, unitScriptableObject);
        shipAI = GetComponent<ShipAI>();
        SetParticleSpeed(particleSpeed);
        SetupThrusters();
        shipAI.SetupShipAI(this);
        mass = GetSize() * 100;
        Spawn();
        SetIdle();
    }

    public void SetupThrusters() {
        thrusting = false;
        thrust = moduleSystem.Get<Thruster>().Sum(t => t.GetThrust() * faction.GetImprovementModifier(Faction.ImprovementAreas.ThrustPower));
    }

    #region Update
    public override void UpdateUnit(float deltaTime) {
        base.UpdateUnit(deltaTime);
        if (IsSpawned()) {
            Profiler.BeginSample("ShipAction");
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
            if (timeUntilCheckRotation > 0)
                timeUntilCheckRotation -= deltaTime;
            if (timeUntilCheckRotation <= 0) {
                targetRotation = Calculator.GetAngleOutOfTwoPositions(GetPosition(), movePosition);
                if (Mathf.Abs(transform.eulerAngles.z - targetRotation) > 0.00001) {
                    if (shipAction == ShipAction.Move) {
                        shipAction = ShipAction.MoveRotate;
                    } else if (shipAction == ShipAction.DockMove) {
                        shipAction = ShipAction.DockRotate;
                    }
                }
                timeUntilCheckRotation += checkRotationSpeed;
            }
        }
        if (shipAction == ShipAction.Rotate || shipAction == ShipAction.MoveRotate || shipAction == ShipAction.DockRotate || shipAction == ShipAction.MoveAndRotate) {
            float localRotation = Calculator.GetLocalTargetRotation(transform.eulerAngles.z, targetRotation);
            float turnSpeed = GetTurnSpeed() * deltaTime;
            if (shipAction == ShipAction.MoveAndRotate && GetEnemyUnitsInRangeDistance().Count != 0)
                    turnSpeed *= GetBattleSpeed(GetEnemyUnitsInRangeDistance().First());
            if (Mathf.Abs(localRotation) <= turnSpeed) {
                SetRotation(targetRotation);
                if (shipAction == ShipAction.Rotate) {
                    SetIdle();
                } else if (shipAction == ShipAction.MoveRotate) {
                    shipAction = ShipAction.Move;
                    SetThrusters(true);
                } else if (shipAction == ShipAction.DockRotate) {
                    shipAction = ShipAction.DockMove;
                    SetThrusters(true);
                } else if (shipAction == ShipAction.MoveAndRotate) {
                    shipAction = ShipAction.Move;
                    SetThrusters(true);
                }
            } else if (localRotation > 0) {
                SetRotation(transform.eulerAngles.z + turnSpeed);
                if (shipAction != ShipAction.MoveAndRotate) {
                    SetThrusters(false);
                    return;
                }
            } else {
                SetRotation(transform.eulerAngles.z - turnSpeed);
                if (shipAction == ShipAction.MoveAndRotate) {
                    SetThrusters(false);
                    return;
                }
            }
        }

        if (shipAction == ShipAction.Move || shipAction == ShipAction.DockMove || shipAction == ShipAction.MoveAndRotate) {
            float distance = Calculator.GetDistanceToPosition((Vector2)transform.position - movePosition);
            float speed = math.min(maxSetSpeed, GetSpeed());
            if (GetEnemyUnitsInRangeDistance().Count != 0)
                speed *= GetBattleSpeed(GetEnemyUnitsInRangeDistance().First());
            float thrust = speed * deltaTime;
            moduleSystem.Get<Thruster>().ForEach(thruster => thruster.SetThrustSize(speed / GetSpeed()));

            if (shipAction == ShipAction.DockMove && distance - thrust < GetSize() + targetStation.GetSize()) {
                DockShip(targetStation);
                return;
            }
            if (distance <= thrust + 2) {
                transform.position = movePosition;
                position = movePosition;
                if (shipAction == ShipAction.Move || shipAction == ShipAction.MoveAndRotate) {
                    SetIdle();
                } else if (shipAction == ShipAction.DockMove) {
                    shipAction = ShipAction.Dock;
                }
            } else {
                transform.Translate(Vector2.up * thrust); // Most of ShipAction computation cost
                velocity = transform.up * speed;
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
    #endregion

    #region ShipControlls
    public void SetIdle() {
        faction.GetFactionAI().AddIdleShip(this);
        shipAction = ShipAction.Idle;
        velocity = Vector2.zero;
        SetThrusters(false);
    }

    public void SetTargetRotate(float rotation) {
        if (shipAction == ShipAction.Idle) {
            faction.GetFactionAI().RemoveShip(this);
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
            faction.GetFactionAI().RemoveShip(this);
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
            faction.GetFactionAI().RemoveShip(this);
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
            faction.GetFactionAI().RemoveShip(this);
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

    public void SetMoveRotateTarget(Vector2 position) {
        movePosition = position;
        targetRotation = Calculator.GetAngleOutOfTwoPositions(GetPosition(), movePosition);
        shipAction = ShipAction.MoveAndRotate;
    }

    public void SetMaxSpeed(float maxspeed = float.MaxValue) {
        maxSetSpeed = maxspeed;
    }

    public float GetThrust() {
        return thrust;
    }

    public float GetSpeed() {
        return thrust / GetMass();
    }

    public float GetBattleSpeed(float distanceToClosestEnemy) {
        if (distanceToClosestEnemy > GetMaxWeaponRange()) return 1f;
        if (distanceToClosestEnemy <= GetMinWeaponRange()) return 0.5f;
        return 0.5f + 0.5f * ((distanceToClosestEnemy - GetMinWeaponRange()) / (GetMaxWeaponRange() - GetMinWeaponRange())) ;
    }

    public void SetThrusters(bool trueOrFalse) {
        if (trueOrFalse != thrusting) {
            thrusting = trueOrFalse;
            if (thrusting) {
                moduleSystem.Get<Thruster>().ForEach(t => t.BeginThrust());
            } else {
                moduleSystem.Get<Thruster>().ForEach(t => t.EndThrust());

            }
        }
    }

    public override void ShowUnit(bool show) {
        base.ShowUnit(show);
        moduleSystem.Get<Turret>().ForEach(t => t.ShowTurret(show));
    }

    public override void ActivateColliders(bool active) {
        base.ActivateColliders(active);
    }

    public override void DestroyUnit() {
        base.DestroyUnit();
        if (fleet != null) fleet.RemoveShip(this);
        fleet = null;
        battleManager.DestroyShip(this);
    }

    public void DockShip(Station station) {
        if (station.DockShip(this)) {
            ShowUnit(false);
            dockedStation = station;
        }
        SetIdle();
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

    public override void Explode() {
        base.Explode();
        moduleSystem.Get<Thruster>().ForEach(t => t.EndThrust());
    }
    #endregion

    #region GetMethods
    public float GetTurnSpeed() {
        return ShipScriptableObject.turnSpeed;
    }

    public float GetCombatRotation() {
        return ShipScriptableObject.combatRotation;
    }
    public Vector2 GetTargetMovePosition() {
        return movePosition;
    }

    public override bool Destroyed() {
        return GetHealth() <= 0;
    }

    public ShipClass GetShipClass() {
        return ShipScriptableObject.shipClass;
    }

    public ShipType GetShipType() {
        return ShipScriptableObject.shipType;
    }

    public override bool IsSelectable() {
        return base.IsSelectable() && dockedStation == null;
    }

    public override bool IsTargetable() {
        return base.IsTargetable() && dockedStation == null;
    }

    public bool IsCombatShip() {
        return ShipScriptableObject.shipType == ShipType.Fighter || ShipScriptableObject.shipType == ShipType.Cruiser || ShipScriptableObject.shipType == ShipType.Frigate || ShipScriptableObject.shipType == ShipType.Dreadnaught;
    }

    public bool IsTransportShip() {
        return ShipScriptableObject.shipType == ShipType.Transport;
    }

    public bool IsConstructionShip() {
        return ShipScriptableObject.shipType == ShipType.Construction;
    }

    public bool IsScienceShip() {
        return ShipScriptableObject.shipType == ShipType.Research;
    }

    public bool IsGasCollectorShip() {
        return ShipScriptableObject.shipType == ShipType.GasCollector;
    }

    public bool IsColonizerShip() {
        return ShipScriptableObject.shipType == ShipType.Colonizer;
    }

    public bool IsCivilianShip() {
        return ShipScriptableObject.shipType == ShipType.Civilian;
    }

    public float GetMass() {
        return mass;
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


    public bool IsIdle() {
        return shipAction == ShipAction.Idle && (shipAI.commands.Count == 0 || shipAI.commands[0].commandType == Command.CommandType.Idle);
    }

    public override void ShowEffects(bool shown) {
        base.ShowEffects(shown);
        moduleSystem.Get<Thruster>().ForEach(t => t.ShowEffects(shown));
    }

    public override void SetParticleSpeed(float speed) {
        base.SetParticleSpeed(speed);
        moduleSystem.Get<Thruster>().ForEach(t => t.SetParticleSpeed(speed));
    }

    public override void ShowParticles(bool shown) {
        base.ShowParticles(shown);
        if (thrusting || !IsSpawned()) {
            moduleSystem.Get<Thruster>().ForEach(t => t.ShowParticles(shown));
        }
    }

    [ContextMenu("GetShipThrust")]
    public void GetShipThrust() {
        float thrust = 0;
        spriteRenderer = GetComponent<SpriteRenderer>();
        foreach (var thruster in GetComponentsInChildren<Thruster>()) {
            thrust += thruster.GetThrust();
        }

        mass = SetupSize() * 100;
        thrust /= mass;
        print(objectName + "Thrust:" + thrust);
    }
    #endregion
}