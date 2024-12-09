using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public abstract class Unit : BattleObject {
    public UnitScriptableObject unitScriptableObject { get; private set; }
    [field: SerializeField] public ComponentModuleSystem moduleSystem { get; private set; }
    private UnitGroup group;

    protected int health;
    private float maxWeaponRange;
    private float minWeaponRange;
    protected Vector2 velocity;
    private DestroyEffect destroyEffect;

    [field: SerializeField] public List<Unit> enemyUnitsInRange { get; protected set; }
    [field: SerializeField] public List<float> enemyUnitsInRangeDistance { get; protected set; }

    public Unit() { }

    public Unit(BattleObjectData battleObjectData, BattleManager battleManager, UnitScriptableObject unitScriptableObject) :
        base(battleObjectData, battleManager) {
        this.unitScriptableObject = unitScriptableObject;
        moduleSystem = new ComponentModuleSystem(battleManager, this, unitScriptableObject);
        health = GetMaxHealth();
        enemyUnitsInRange = new List<Unit>(20);
        enemyUnitsInRangeDistance = new List<float>(20);
        minWeaponRange = float.MaxValue;
        maxWeaponRange = float.MinValue;
        SetupWeaponRanges();
        Spawn();
        SetSize(SetupSize());
    }

    protected override float SetupSize() {
        return GetSpriteSize();
    }

    public void SetupWeaponRanges() {
        foreach (var turret in moduleSystem.Get<Turret>()) {
            maxWeaponRange = Mathf.Max(maxWeaponRange, turret.GetRange());
            minWeaponRange = Mathf.Min(minWeaponRange, turret.GetRange());
        }

        foreach (var missileLuancher in moduleSystem.Get<MissileLauncher>()) {
            maxWeaponRange = Mathf.Max(maxWeaponRange, missileLuancher.GetRange() / 2);
            minWeaponRange = Mathf.Min(minWeaponRange, missileLuancher.GetRange() / 2);
        }
    }

    #region Update

    public virtual void UpdateUnit(float deltaTime) {
        if (IsTargetable() && HasWeapons()) {
            FindEnemies();
            UpdateWeapons(deltaTime);
        }

        if (IsSpawned()) {
            moduleSystem.Get<ShieldGenerator>().ForEach(s => s.UpdateShieldGenerator(deltaTime));
            moduleSystem.Get<Generator>().ForEach(s => s.UpdateGenerator(deltaTime));
        }
    }

    protected virtual void FindEnemies() {
        Profiler.BeginSample("FindingEnemies");
        enemyUnitsInRange.Clear();
        enemyUnitsInRangeDistance.Clear();
        float distanceFromFactionCenter = Vector2.Distance(faction.GetPosition(), GetPosition());
        for (int i = 0; i < faction.closeEnemyGroups.Count; i++) {
            if (faction.closeEnemyGroupsDistance[i] > distanceFromFactionCenter + maxWeaponRange * 2)
                break;
            FindEnemyGroup(faction.closeEnemyGroups[i]);
        }

        Profiler.EndSample();
    }

    void FindEnemyGroup(UnitGroup targetGroup) {
        foreach (var battleObject in targetGroup.battleObjects) {
            FindEnemyUnit(battleObject);
        }
    }

    void FindEnemyUnit(Unit targetUnit) {
        if (targetUnit == null || !targetUnit.IsTargetable())
            return;
        float distance = Vector2.Distance(GetPosition(), targetUnit.GetPosition());
        if (distance <= maxWeaponRange * 2 + targetUnit.GetSize()) {
            for (int f = 0; f < enemyUnitsInRangeDistance.Count; f++) {
                if (enemyUnitsInRangeDistance[f] >= distance) {
                    enemyUnitsInRangeDistance.Insert(f, distance);
                    enemyUnitsInRange.Insert(f, targetUnit);
                    return;
                }
            }

            //Has not been added yet
            enemyUnitsInRange.Add(targetUnit);
            enemyUnitsInRangeDistance.Add(distance);
        }
    }

    protected virtual void UpdateWeapons(float deltaTime) {
        Profiler.BeginSample("Weapons");

        moduleSystem.Get<Turret>().ForEach(t => t.UpdateTurret(deltaTime));
        moduleSystem.Get<MissileLauncher>().ForEach(m => m.UpdateMissileLauncher(deltaTime));
        Profiler.EndSample();
    }

    public void UpdateDestroyedUnit(float deltaTime) {
        if (!destroyEffect.UpdateDestroyEffect(deltaTime)) {
            BattleManager.Instance.RemoveDestroyedUnit(this);
        }
    }

    #endregion

    #region UnitControlls

    public virtual int TakeDamage(int damage) {
        if (IsSpawned()) {
            health -= damage;
            if (Destroyed()) {
                int returnValue = -health;
                health = 0;
                Explode();
                return returnValue;
            }

            return 0;
        }

        Debug.LogWarning("Unit not spawned is taking damage" + objectName + " position:" + GetPosition());
        return 0;
    }

    protected override void Despawn(bool removeImmediately) {
        base.Despawn(removeImmediately);
        health = 0;
        DestroyUnit();
    }

    public virtual void Explode() {
        if (!IsSpawned())
            return;
        moduleSystem.Get<ShieldGenerator>().ForEach(s => s.DestroyShield());
        moduleSystem.Get<Turret>().ForEach(s => s.StopFiring());
        moduleSystem.Get<Hangar>().ForEach(h => h.UndockAll());
        destroyEffect = new DestroyEffect(unitScriptableObject.destroyEffect);
        Despawn(false);
    }

    public virtual void DestroyUnit() {
        SetGroup(null);
        moduleSystem.Get<ShieldGenerator>().ForEach(s => s.DestroyShield());
        RemoveFromAllGroups();
    }

    public virtual bool IsTargetable() {
        return IsSpawned();
    }

    #endregion

    #region HelperMethods

    public void SetGroup(UnitGroup newGroup) {
        if (group != null) {
            UnitGroup oldGroup = group;
            group = null;
            oldGroup.RemoveBattleObject(this);
        }

        if (newGroup == null) {
            newGroup = faction.baseGroup;
        }

        group = newGroup;
        if (newGroup != null)
            group.AddBattleObject(this);
    }

    public UnitGroup GetGroup() {
        return group;
    }

    public float GetMaxWeaponRange() {
        return maxWeaponRange;
    }

    public float GetMinWeaponRange() {
        return minWeaponRange;
    }

    /// <summary>
    /// Tries and uses up to the amount cargo from all of the cargo bays
    /// </summary>
    /// <returns>The leftover amount that couldn't be used, or 0 if all of it was used</returns>
    public long UseCargo(long amount, CargoBay.CargoTypes cargoType) {
        long totalCargoToUse = amount;
        foreach (var cargoBay in moduleSystem.Get<CargoBay>()) {
            totalCargoToUse = cargoBay.UseCargo(totalCargoToUse, cargoType);
            if (totalCargoToUse <= 0) return 0;
        }

        return totalCargoToUse;
    }

    /// <summary>
    /// Tries to load up to the amount in cargo to all of the cargo bays
    /// </summary>
    /// <returns>The leftover amount that couldn't be loaded to any cargo bay, or 0 if all was added</returns>
    public long LoadCargo(long amount, CargoBay.CargoTypes cargoType) {
        long totalCargoToLoad = amount;
        foreach (var cargoBay in moduleSystem.Get<CargoBay>()) {
            totalCargoToLoad = cargoBay.LoadCargo(totalCargoToLoad, cargoType);
            if (totalCargoToLoad <= 0) return 0;
        }

        return totalCargoToLoad;
    }

    /// <summary>
    /// Tries to load up to the amount in cargo from the unit given to this unit
    /// </summary>
    /// <returns>The leftover amount that couldn't be loaded </returns>
    public long LoadCargoFromUnit(long amount, CargoBay.CargoTypes cargoType, Unit unit) {
        if (cargoType == CargoBay.CargoTypes.All) {
            foreach (var type in CargoBay.allCargoTypes) {
                amount = LoadCargoFromUnit(amount, type, unit);
                if (amount == 0) break;
            }

            return amount;
        }

        long cargoToLoad = Math.Min(amount, Math.Min(unit.GetAllCargoOfType(cargoType), GetAvailableCargoSpace(cargoType)));
        unit.UseCargo(cargoToLoad, cargoType);
        LoadCargo(cargoToLoad, cargoType);
        return amount - cargoToLoad;
    }

    public long GetAllCargoOfType(CargoBay.CargoTypes cargoType, bool includeReserved = false) {
        long reserved = 0;
        if (includeReserved) {
            if (cargoType == CargoBay.CargoTypes.All) {
                reserved = CargoBay.allCargoTypes.Sum((t) => GetReservedCargoSpace().GetValueOrDefault(t, 0));
            } else {
                reserved = GetReservedCargoSpace().GetValueOrDefault(cargoType, 0);
            }
        }

        return moduleSystem.Get<CargoBay>().Sum(cargoBay => cargoBay.GetAllCargo(cargoType) - reserved);
    }

    public long GetAvailableCargoSpace(CargoBay.CargoTypes cargoType) {
        return moduleSystem.Get<CargoBay>().Sum(cargoBay => cargoBay.GetOpenCargoCapacityOfType(cargoType));
    }

    public Dictionary<CargoBay.CargoTypes, long> GetReservedCargoSpace() {
        if (IsStation() && ((Station)this).GetStationType() == Station.StationType.Shipyard ||
            ((Station)this).GetStationType() == Station.StationType.FleetCommand) {
            return ((Shipyard)this).GetConstructionBay().GetReservedResources();
        }

        return new Dictionary<CargoBay.CargoTypes, long>();
    }

    public string GetUnitName() {
        return objectName;
    }

    public int GetHealth() {
        return health;
    }

    public int GetMaxHealth() {
        return Mathf.RoundToInt(unitScriptableObject.maxHealth * faction.GetImprovementModifier(Faction.ImprovementAreas.HullStrength));
    }

    public int GetTotalHealth() {
        return GetHealth() + GetShields();
    }

    public bool IsDamaged() {
        return health < GetMaxHealth();
    }

    /// <summary>
    /// Repairs the unit and returns the extra amount that was not used
    /// </summary>
    /// <param name="ammount">the amount to repair</param>
    /// <returns>the extra amount not used</returns>
    public int Repair(int ammount) {
        health += ammount;
        if (health > GetMaxHealth()) {
            int returnValue = health - GetMaxHealth();
            health = GetMaxHealth();
            return returnValue;
        }

        return 0;
    }

    public int GetShields() {
        return moduleSystem.Get<ShieldGenerator>().Sum(s => s.GetShieldStrength());
    }

    public int GetMaxShields() {
        return moduleSystem.Get<ShieldGenerator>().Sum(s => s.GetMaxShieldStrength());
    }

    public abstract bool Destroyed();

    public virtual Vector2 GetVelocity() {
        return velocity;
    }

    public bool HasWeapons() {
        return moduleSystem.Get<Turret>().Count > 0 || moduleSystem.Get<MissileLauncher>().Count > 0;
    }

    public virtual List<Unit> GetEnemyUnitsInRange() {
        return enemyUnitsInRange;
    }

    public virtual List<float> GetEnemyUnitsInRangeDistance() {
        return enemyUnitsInRangeDistance;
    }

    public List<Ship> GetAllDockedShips() {
        List<Ship> dockedShips = new();
        moduleSystem.Get<Hangar>().ForEach(h => dockedShips.AddRange(h.ships));
        return dockedShips;
    }

    [ContextMenu("GetUnitDamagePerSecond")]
    public void GetEditorUnitDamagePerSecond() {
        // float dps = 0;
        // foreach (var projectileTurret in GetComponentsInChildren<ProjectileTurret>()) {
        //     dps += projectileTurret.GetDamagePerSecond();
        // }
        // foreach (var laserTurret in GetComponentsInChildren<LaserTurret>()) {
        //     dps += laserTurret.GetDamagePerSecond();
        // }
        // foreach (var missileLauncher in GetComponentsInChildren<MissileLauncher>()) {
        //     dps += missileLauncher.GetDamagePerSecond();
        // }
        // print(objectName + "Dps:" + dps);
    }

    [ContextMenu("ForceDestroy")]
    public void EditorForceDestroy() {
        TakeDamage(GetHealth());
    }

    public float GetUnitDamagePerSecond() {
        return moduleSystem.Get<Turret>().Sum(t => t.GetDamagePerSecond())
               + moduleSystem.Get<LaserTurret>().Sum(t => t.GetDamagePerSecond())
               + moduleSystem.Get<MissileLauncher>().Sum(t => t.GetDamagePerSecond());
    }

    public int GetWeaponCount() {
        return moduleSystem.Get<Turret>().Count
               + moduleSystem.Get<LaserTurret>().Count
               + moduleSystem.Get<MissileLauncher>().Count;
    }

    public override float GetSpriteSize() {
        return Calculator.GetSpriteSize(unitScriptableObject.sprite, scale);
    }

    public DestroyEffect GetDestroyEffect() {
        return destroyEffect;
    }

    #endregion

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load(unitScriptableObject.prefabPath);
    }
}
