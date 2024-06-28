using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(ModuleSystem))]
public abstract class Unit : BattleObject, IParticleHolder {
    public UnitScriptableObject UnitScriptableObject { get; private set; }
    [field: SerializeField] public ModuleSystem moduleSystem { get; private set; }
    private UnitGroup group;

    protected int health;
    [SerializeField] protected int followDist;
    private UnitSelection unitSelection;
    protected List<Collider2D> colliders;
    private ShieldGenerator shieldGenerator;
    protected List<CargoBay> cargoBays;
    protected List<Turret> turrets;
    protected List<MissileLauncher> missileLaunchers;
    private float maxWeaponRange;
    private float minWeaponRange;
    protected Vector2 velocity;
    private DestroyEffect destroyEffect;

    [field: SerializeField] public List<Unit> enemyUnitsInRange { get; protected set; }
    [field: SerializeField] public List<float> enemyUnitsInRangeDistance { get; protected set; }

    public virtual void SetupUnit(BattleManager battleManager, string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, float particleSpeed, UnitScriptableObject unitScriptableObject) {
        this.UnitScriptableObject = unitScriptableObject;
        this.faction = faction;
        base.SetupBattleObject(battleManager, positionGiver, rotation);
        moduleSystem = GetComponent<ModuleSystem>();
        moduleSystem.SetupModuleSystem(this, unitScriptableObject);
        this.objectName = name;
        health = GetMaxHealth();
        transform.eulerAngles = new Vector3(0, 0, rotation);
        enemyUnitsInRange = new List<Unit>(20);
        enemyUnitsInRangeDistance = new List<float>(20);
        cargoBays = new List<CargoBay>(GetComponentsInChildren<CargoBay>());
        turrets = new List<Turret>(GetComponentsInChildren<Turret>());
        missileLaunchers = new List<MissileLauncher>(GetComponentsInChildren<MissileLauncher>());
        unitSelection = GetComponentInChildren<UnitSelection>();
        destroyEffect = GetComponentInChildren<DestroyEffect>();
        destroyEffect.SetupDestroyEffect(this, spriteRenderer);
        shieldGenerator = GetComponentInChildren<ShieldGenerator>();
        colliders = new List<Collider2D>(GetComponents<Collider2D>());
        minWeaponRange = float.MaxValue;
        maxWeaponRange = float.MinValue;
        foreach (var turret in turrets) {
            turret.SetupTurret(this);
        }
        foreach (var missileLauncher in missileLaunchers) {
            missileLauncher.SetupMissileLauncher(this);
        }
        SetupWeaponRanges();
        if (shieldGenerator != null)
            shieldGenerator.SetupShieldGenerator(this);
        unitSelection.SetupSelection(this);
        SetParticleSpeed(particleSpeed);
        followDist = (int)(GetSize() * 2);
    }

    public void SetupWeaponRanges() {
        foreach (var turret in turrets) {
            maxWeaponRange = Mathf.Max(maxWeaponRange, turret.GetRange());
            minWeaponRange = Mathf.Min(minWeaponRange, turret.GetRange());
        }
        foreach (var missileLuancher in missileLaunchers) {
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
            if (shieldGenerator != null) {
                shieldGenerator.UpdateShieldGenerator(deltaTime);
            }
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
        foreach (var turret in turrets) {
            Profiler.BeginSample("Turret");
            turret.UpdateTurret(deltaTime);
            Profiler.EndSample();
        }
        foreach (var missileLauncher in missileLaunchers) {
            Profiler.BeginSample("MissileLauncher");
            missileLauncher.UpdateMissileLauncher(deltaTime);
            Profiler.EndSample();
        }
    }

    public void UpdateUnitUI(bool showIndicators) {
        if (spriteRenderer.enabled) {
            unitSelection.UpdateUnitSelection(showIndicators);
        }
    }

    public void UpdateDestroyedUnit(float deltaTime) {
        if (destroyEffect.IsPlaying() == false && GetHealth() <= 0) {
            BattleManager.Instance.RemoveDestroyedUnit(this);
            //if (IsStation() && ((Station)this).stationType == Station.StationType.FleetCommand)
            //    return;
            Destroy(gameObject);
        } else {
            destroyEffect.UpdateExplosion(deltaTime);
        }
    }
    #endregion

    #region UnitControlls
    public virtual int TakeDamage(int damage) {
        damage /= 4;
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

    public override void SelectObject(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) {
        if (IsSpawned())
            unitSelection.SetSelected(selectionStrength);
    }

    public override void UnselectObject() {
        if (IsSpawned())
            SelectObject(UnitSelection.SelectionStrength.Unselected);
    }

    public virtual void ShowUnit(bool show) {
        spriteRenderer.enabled = show;
        ActivateColliders(show);
        unitSelection.ShowUnitSelection(show);
    }

    public virtual void ActivateColliders(bool active) {
        for (int i = 0; i < colliders.Count; i++) {
            colliders[i].enabled = active;
        }
        if (shieldGenerator != null) {
            shieldGenerator.ShowShield(active);
        }
    }

    protected override void Despawn(bool removeImmediately) {
        base.Despawn(removeImmediately);
        health = 0;
        ActivateColliders(false);
        unitSelection.ShowUnitSelection(false);
        DestroyUnit();
    }

    public virtual void Explode() {
        if (!IsSpawned())
            return;
        if (BattleManager.Instance.GetParticlesShown())
            destroyEffect.Explode();
        if (shieldGenerator != null)
            shieldGenerator.DestroyShield();
        for (int i = 0; i < turrets.Count; i++) {
            turrets[i].StopFiring();
        }
        float value = UnityEngine.Random.Range(0.2f, 0.6f);
        GetSpriteRenderers().ForEach(r => { r.color = new Color(value, value, value, 1); });
        Despawn(false);
    }

    public virtual void DestroyUnit() {
        SetGroup(null);
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
        foreach (var cargoBay in GetCargoBays()) {
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
        foreach (var cargoBay in GetCargoBays()) {
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
        long cargoToLoad = Math.Min(amount, Math.Min(unit.GetAllCargoOfType(cargoType), GetAvailableCargoSpace(cargoType)));
        unit.UseCargo(cargoToLoad, cargoType);
        LoadCargo(cargoToLoad, cargoType);
        return amount - cargoToLoad;
    }

    public long GetAllCargoOfType(CargoBay.CargoTypes cargoType) {
        return cargoBays.Sum(cargoBay => cargoBay.GetAllCargo(cargoType));
    }

    public long GetAvailableCargoSpace(CargoBay.CargoTypes cargoType) {
        return GetCargoBays().Sum(cargoBay => cargoBay.GetOpenCargoCapacityOfType(cargoType));
    }

    public string GetUnitName() {
        return objectName;
    }

    public int GetHealth() {
        return health;
    }
    public int GetMaxHealth() {
        return Mathf.RoundToInt(UnitScriptableObject.maxHealth * faction.GetImprovementModifier(Faction.ImprovementAreas.HullStrength));
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
        if (shieldGenerator == null)
            return 0;
        return shieldGenerator.GetShieldStrength();
    }

    public int GetMaxShields() {
        if (shieldGenerator == null)
            return 0;
        return shieldGenerator.GetMaxShieldStrength();
    }

    public int GetFollowDistance() {
        return followDist;
    }

    public abstract bool Destroyed();

    public List<Turret> GetTurrets() {
        return turrets;
    }

    public List<CargoBay> GetCargoBays() {
        return cargoBays;
    }

    public ShieldGenerator GetShieldGenerator() {
        return shieldGenerator;
    }

    public float GetCombinedFollowDistance(Unit unit) {
        return GetFollowDistance() + unit.GetFollowDistance();
    }

    public virtual Vector2 GetVelocity() {
        return velocity;
    }

    public float GetZoomIndicatorSize() {
        return unitSelection.GetSize();
    }
    public UnitSelection GetUnitSelection() {
        return unitSelection;
    }

    public bool HasWeapons() {
        return turrets.Count > 0 || missileLaunchers.Count > 0;
    }

    public virtual List<Unit> GetEnemyUnitsInRange() {
        return enemyUnitsInRange;
    }

    public virtual List<float> GetEnemyUnitsInRangeDistance() {
        return enemyUnitsInRangeDistance;
    }

    public virtual void ShowEffects(bool shown) {
        destroyEffect.ShowEffects(shown);
    }

    public virtual void SetParticleSpeed(float speed) {
        destroyEffect.SetParticleSpeed(speed);
    }

    public virtual void ShowParticles(bool shown) {
        destroyEffect.ShowParticles(shown);
    }

    [ContextMenu("GetUnitDamagePerSecond")]
    public void GetEditorUnitDamagePerSecond() {
        float dps = 0;
        foreach (var massTurret in GetComponentsInChildren<MassTurret>()) {
            dps += massTurret.GetDamagePerSecond();
        }
        foreach (var laserTurret in GetComponentsInChildren<LaserTurret>()) {
            dps += laserTurret.GetDamagePerSecond();
        }
        foreach (var missileLauncher in GetComponentsInChildren<MissileLauncher>()) {
            dps += missileLauncher.GetDamagePerSecond();
        }
        print(objectName + "Dps:" + dps);
    }

    [ContextMenu("ForceDestroy")]
    public void EditorForceDestroy() {
        TakeDamage(GetHealth());
    }

    public float GetUnitDamagePerSecond() {
        float dps = 0;
        foreach (var massTurret in turrets) {
            dps += massTurret.GetDamagePerSecond();
        }

        foreach (var missileLauncher in missileLaunchers) {
            dps += missileLauncher.GetDamagePerSecond();
        }
        return dps;
    }

    public int GetWeaponCount() {
        return turrets.Count + missileLaunchers.Count;
    }

    public override List<SpriteRenderer> GetSpriteRenderers() {
        List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer> {
            spriteRenderer
        };
        spriteRenderers.AddRange(turrets.Select(t => t.GetSpriteRenderer()));
        return spriteRenderers;
    }
    #endregion

}
