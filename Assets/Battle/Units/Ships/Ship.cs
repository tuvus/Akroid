using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class Ship : Unit {
    public enum ShipAction {
        Idle,
        Rotate,
        Move,
        MoveRotate,
        MoveLateral,
        Dock,
        DockMove,
        DockRotate,
        MoveAndRotate
    }

    public enum ShipClass {
        Transport,
        HeavyTransport,
        Aria,
        Lancer,
        Aterna,
        StationBuilder,
        Zarrack,
        Eletera
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
        Colonizer
    }

    private const float checkRotationSpeed = 0.2f;
    public Station dockedStation;
    public Fleet fleet;
    private float mass;
    private float maxSetSpeed;
    [SerializeField] private Vector2 movePosition;

    public ShipAction shipAction;
    [SerializeField] private float targetRotation;
    [SerializeField] private Station targetStation;
    private float thrust;
    public float speed { get; private set; }
    private float timeUntilCheckRotation;
    public Random random;

    public Ship(BattleObjectData battleObjectData, BattleManager battleManager,
        ShipScriptableObject shipScriptableObject, Random random) :
        base(battleObjectData, battleManager, shipScriptableObject) {
        this.shipScriptableObject = shipScriptableObject;
        this.random = random;
        faction.AddShip(this);
        switch (shipScriptableObject.shipType) {
            default:
                shipAI = new ShipAI(this);
                break;
        }

        mass = GetSize() * 100;
        thrusting = false;
        RecalculateThrust();
        SetIdle();
        visible = true;
    }
    public ShipScriptableObject shipScriptableObject { get; }

    public ShipAI shipAI { get; }
    public bool thrusting { get; private set; }
    /// <summary> A modifier to the thrust size between 0 and 1 based on the ships speed. </summary>
    public float thrustSize { get; private set; }

    [Serializable]
    public class ShipBlueprint {
        public string name;
        public ShipScriptableObject shipScriptableObject;
        public Faction faction;

        protected ShipBlueprint(Faction faction, ShipScriptableObject shipScriptableObject, string name = null) {
            if (name == null) this.name = shipScriptableObject.unitName;
            else this.name = name;

            this.faction = faction;
            this.shipScriptableObject = shipScriptableObject;
        }
        public ShipBlueprint(ShipBlueprint shipBlueprint, Faction faction) {
            name = shipBlueprint.name;
            this.faction = faction;
            shipScriptableObject = shipBlueprint.shipScriptableObject;
        }
    }

    [Serializable]
    public class ShipConstructionBlueprint : ShipBlueprint {
        /// <summary> The credit cost of constructing the blueprint. </summary>
        public long cost;

        /// <summary>
        ///     The amount of resources to be put into the blueprint before it can be constructed.
        ///     This value may be reduced throughout construction.
        /// </summary>
        public Dictionary<CargoBay.CargoTypes, long> resourceCosts;

        public ShipConstructionBlueprint(Faction faction, ShipBlueprint shipBlueprint, string name = null) : base(
            faction,
            shipBlueprint.shipScriptableObject, name) {
            cost = shipScriptableObject.cost;
            resourceCosts = new Dictionary<CargoBay.CargoTypes, long>();
            totalResourcesRequired = 0;
            for (int i = 0; i < shipScriptableObject.resourceTypes.Count; i++) {
                resourceCosts.Add(shipScriptableObject.resourceTypes[i], shipScriptableObject.resourceCosts[i]);
                totalResourcesRequired += shipScriptableObject.resourceCosts[i];
            }
        }

        public long totalResourcesRequired { get; private set; }

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

    #region Update

    public override void UpdateUnit(float deltaTime) {
        base.UpdateUnit(deltaTime);
        if (!IsSpawned()) return;
        // Profiler.BeginSample("ShipAction");
        UpdateMovement(deltaTime);
        // Profiler.EndSample();
        shipAI.UpdateAI(deltaTime);
    }

    public override void FindEnemies() {
        if (fleet == null) base.FindEnemies();
    }

    private void UpdateMovement(float deltaTime) {
        if (shipAction == ShipAction.Idle) {
            return;
        }

        velocity = Vector2.zero;
        if ((shipAction == ShipAction.Dock || shipAction == ShipAction.DockMove ||
                shipAction == ShipAction.DockRotate) &&
            (targetStation == null || !targetStation.IsSpawned())) {
            SetIdle();
        }

        if (shipAction == ShipAction.Move || shipAction == ShipAction.DockMove) {
            if (timeUntilCheckRotation > 0)
                timeUntilCheckRotation -= deltaTime;
            if (timeUntilCheckRotation <= 0) {
                targetRotation = Calculator.GetAngleOutOfTwoPositions(GetPosition(), movePosition);
                if (Mathf.Abs(rotation - targetRotation) > 0.00001) {
                    if (shipAction == ShipAction.Move) {
                        shipAction = ShipAction.MoveRotate;
                    } else if (shipAction == ShipAction.DockMove) {
                        shipAction = ShipAction.DockRotate;
                    }
                }

                timeUntilCheckRotation += checkRotationSpeed;
            }
        }

        if (shipAction == ShipAction.Rotate || shipAction == ShipAction.MoveRotate ||
            shipAction == ShipAction.DockRotate ||
            shipAction == ShipAction.MoveAndRotate) {
            float localRotation = Calculator.GetLocalTargetRotation(rotation, targetRotation);
            float turnSpeed = GetTurnSpeed() * deltaTime;
            if (shipAction == ShipAction.MoveAndRotate && GetEnemyUnitsInRangeDistance().Count != 0)
                turnSpeed *= GetBattleSpeed(GetEnemyUnitsInRangeDistance().First());
            if (Mathf.Abs(localRotation) <= turnSpeed) {
                SetRotation(targetRotation);
                if (shipAction == ShipAction.Rotate) {
                    SetIdle();
                } else if (shipAction == ShipAction.MoveRotate) {
                    shipAction = ShipAction.Move;
                    thrusting = true;
                } else if (shipAction == ShipAction.DockRotate) {
                    shipAction = ShipAction.DockMove;
                    thrusting = true;
                } else if (shipAction == ShipAction.MoveAndRotate) {
                    shipAction = ShipAction.Move;
                    thrusting = true;
                }
            } else if (localRotation > 0) {
                SetRotation(rotation + turnSpeed);
                if (shipAction != ShipAction.MoveAndRotate) {
                    thrusting = false;
                    return;
                }
            } else {
                SetRotation(rotation - turnSpeed);
                if (shipAction == ShipAction.MoveAndRotate) {
                    thrusting = false;
                    return;
                }
            }
        }

        if (shipAction == ShipAction.Move || shipAction == ShipAction.DockMove ||
            shipAction == ShipAction.MoveAndRotate) {
            float distance = Calculator.GetDistanceToPosition(position - movePosition);
            float currentSpeed = math.min(maxSetSpeed, speed);
            if (GetEnemyUnitsInRangeDistance().Count != 0)
                currentSpeed *= GetBattleSpeed(GetEnemyUnitsInRangeDistance().First());
            float thrust = currentSpeed * deltaTime;
            thrustSize = currentSpeed / speed;

            if (shipAction == ShipAction.DockMove && distance - thrust < GetSize() + targetStation.GetSize()) {
                DockShip(targetStation);
                return;
            }

            if (distance <= thrust + 2) {
                position = movePosition;
                if (shipAction == ShipAction.Move || shipAction == ShipAction.MoveAndRotate) {
                    SetIdle();
                } else if (shipAction == ShipAction.DockMove) {
                    shipAction = ShipAction.Dock;
                }
            } else {
                position += Calculator.GetPositionOutOfAngleAndDistance(rotation, thrust);
                velocity = Calculator.GetPositionOutOfAngleAndDistance(rotation, currentSpeed);
                return;
            }
        }

        if (shipAction == ShipAction.Dock) {
            DockShip(targetStation);
            return;
        }

        if (shipAction == ShipAction.MoveLateral) {
            thrusting = false;
            float currentSpeed = math.min(maxSetSpeed, speed) / 2;
            if (Vector2.Distance(GetPosition(), movePosition) <= currentSpeed * deltaTime) {
                position = movePosition;
                SetIdle();
            } else {
                Vector3 temp = Vector2.MoveTowards(GetPosition(), movePosition, currentSpeed * deltaTime) - GetPosition();
                position += (Vector2)temp;
            }
        }
    }

    public void RecalculateThrust() {
        thrust = moduleSystem.Get<Thruster>()
            .Sum(t => t.GetThrust() * faction.GetImprovementModifier(Faction.ImprovementAreas.ThrustPower));
        speed = thrust / GetMass();
    }
    #endregion

    #region ShipControlls

    public void SetIdle() {
        faction.GetFactionAI().AddIdleShip(this);
        shipAction = ShipAction.Idle;
        velocity = Vector2.zero;
        thrusting = false;
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
        targetRotation = rotation;
    }

    public void SetTargetRotate(Vector2 position) {
        SetTargetRotate(Calculator.GetAngleOutOfTwoPositions(GetPosition(), position));
    }

    public void SetTargetRotate(Vector2 position, float extraAngle) {
        SetTargetRotate(
            Calculator.ConvertTo360DegRotation(Calculator.GetAngleOutOfTwoPositions(GetPosition(), position) +
                extraAngle));
    }

    public void SetLateralMovePosition(Vector2 position) {
        if (shipAction == ShipAction.Idle) {
            faction.GetFactionAI().RemoveShip(this);
        }

        if (dockedStation != null) UndockShip(position);
        shipAction = ShipAction.MoveLateral;
        movePosition = position;
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
        movePosition = position;
        SetTargetRotate(position);
        shipAction = ShipAction.MoveRotate;
    }

    public void SetMovePosition(Vector2 position, float distanceFromPosition) {
        SetMovePosition(Vector2.MoveTowards(GetPosition(), position,
            Vector2.Distance(GetPosition(), position) - distanceFromPosition));
    }

    public void SetDockTarget(Station targetStation) {
        if (shipAction == ShipAction.Idle) {
            faction.GetFactionAI().RemoveShip(this);
        }

        if (dockedStation == targetStation) {
            SetIdle();
            return;
        }

        if (Vector2.Distance(position, targetStation.GetPosition()) < GetSize() + targetStation.GetSize()) {
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


    public float GetBattleSpeed(float distanceToClosestEnemy) {
        if (distanceToClosestEnemy > GetMaxWeaponRange()) return 1f;
        if (distanceToClosestEnemy <= GetMinWeaponRange()) return 0.5f;
        return 0.5f + 0.5f * ((distanceToClosestEnemy - GetMinWeaponRange()) /
            (GetMaxWeaponRange() - GetMinWeaponRange()));
    }

    public override void DestroyUnit() {
        base.DestroyUnit();
        if (fleet != null) fleet.RemoveShip(this);
        fleet = null;
        battleManager.DestroyShip(this);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DockShip(Station station) {
        if (station.DockShip(this)) {
            visible = false;
            dockedStation = station;
            position = station.position;
            rotation = 0;
            SetIdle();
        }
    }

    public void UndockShip() {
        UndockShip(random.NextFloat(0f, 360f));
        velocity = Vector2.zero;
    }

    public void UndockShip(Vector2 position) {
        UndockShip(Calculator.GetAngleOutOfTwoPositions(this.position, position));
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void UndockShip(float angle) {
        dockedStation.UndockShip(this);
        visible = true;
        Vector2 undockPos = Calculator.GetPositionOutOfAngleAndDistance(angle, GetSize() + dockedStation.GetSize());
        position += undockPos;
        rotation = angle;
        dockedStation = null;
    }

    public override void Explode() {
        base.Explode();
        shipAI.ClearCommands();
        thrusting = false;
    }

    #endregion

    #region GetMethods

    public float GetTurnSpeed() {
        return shipScriptableObject.turnSpeed;
    }

    public float GetCombatRotation() {
        return shipScriptableObject.combatRotation;
    }

    public Vector2 GetTargetMovePosition() {
        return movePosition;
    }



    public ShipClass GetShipClass() {
        return shipScriptableObject.shipClass;
    }

    public ShipType GetShipType() {
        return shipScriptableObject.shipType;
    }

    public override bool IsTargetable() {
        return base.IsTargetable() && dockedStation == null;
    }

    public bool IsCombatShip() {
        return shipScriptableObject.shipType == ShipType.Fighter || shipScriptableObject.shipType == ShipType.Cruiser ||
            shipScriptableObject.shipType == ShipType.Frigate || shipScriptableObject.shipType == ShipType.Dreadnaught;
    }

    public bool IsTransportShip() {
        return shipScriptableObject.shipType == ShipType.Transport;
    }

    public bool IsConstructionShip() {
        return shipScriptableObject.shipType == ShipType.Construction;
    }

    public bool IsScienceShip() {
        return shipScriptableObject.shipType == ShipType.Research;
    }

    public bool IsGasCollectorShip() {
        return shipScriptableObject.shipType == ShipType.GasCollector;
    }

    public bool IsColonizerShip() {
        return shipScriptableObject.shipType == ShipType.Colonizer;
    }

    public bool IsCivilianShip() {
        return shipScriptableObject.shipType == ShipType.Civilian;
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
        return shipAction == ShipAction.Idle &&
            (shipAI.commands.Count == 0 || shipAI.commands[0].commandType == Command.CommandType.Idle);
    }

    [ContextMenu("GetShipThrust")]
    public void GetShipThrust() {
        float thrust = moduleSystem.Get<Thruster>().Sum(t => t.GetThrust());

        mass = SetupSize() * 100;
        thrust /= mass;
        Debug.Log(objectName + "Thrust:" + thrust);
    }

    #endregion
}
