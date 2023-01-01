using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public abstract class Unit : BattleObject {
    private bool spawned;
    [SerializeField] protected int maxHealth;
    [SerializeField] protected string unitName;

    protected int health;
    [SerializeField] protected int followDist;

    public Faction faction { get; protected set; }
    private UnitSelection unitSelection;
    protected ParticleSystem destroyParticle;
    protected List<Collider2D> colliders;
    private ShieldGenerator shieldGenerator;
    protected List<CargoBay> cargoBays;
    protected List<Turret> turrets;
    protected List<MissileLauncher> missileLaunchers;
    private float maxWeaponRange;
    private float minWeaponRange;
    protected Vector2 velocity;

    public List<Unit> enemyUnitsInRange { get; protected set; }
    public List<float> enemyUnitsInRangeDistance { get; protected set; }

    public virtual void SetupUnit(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation, float particleSpeed) {
        this.faction = faction;
        base.SetupBattleObject(positionGiver, rotation);
        this.unitName = name;
        health = GetMaxHealth();
        transform.eulerAngles = new Vector3(0, 0, rotation);
        enemyUnitsInRange = new List<Unit>(20);
        enemyUnitsInRangeDistance = new List<float>(20);
        cargoBays = new List<CargoBay>(GetComponentsInChildren<CargoBay>());
        turrets = new List<Turret>(GetComponentsInChildren<Turret>());
        missileLaunchers = new List<MissileLauncher>(GetComponentsInChildren<MissileLauncher>());
        unitSelection = GetComponentInChildren<UnitSelection>();
        destroyParticle = GetComponent<ParticleSystem>();
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
        followDist = (int)(size * 2);
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
        foreach (var enemyFaction in faction.enemyFactions) {
            if (Vector2.Distance(GetPosition(), enemyFaction.factionPosition) > maxWeaponRange * 2 + enemyFaction.factionUnitsSize)
                continue;
            for (int i = 0; i < enemyFaction.unitsNotInFleet.Count; i++) {
                FindUnit(enemyFaction.units[i]);
            }
            for (int i = 0; i < enemyFaction.fleets.Count; i++) {
                Fleet targetFleet = enemyFaction.fleets[i];
                if (Vector2.Distance(GetPosition(), targetFleet.GetPosition()) <= maxWeaponRange * 2 + targetFleet.GetSize()) {
                    for (int f = 0; f < targetFleet.ships.Count; f++) {
                        FindUnit(targetFleet.ships[f]);
                    }
                }
            }
        }
        Profiler.EndSample();
    }

    void FindUnit(Unit targetUnit) {
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
        for (int i = 0; i < turrets.Count; i++) {
            Profiler.BeginSample("Turret");
            turrets[i].UpdateTurret(deltaTime);
            Profiler.EndSample();
        }
        for (int i = 0; i < missileLaunchers.Count; i++) {
            Profiler.BeginSample("MissileLauncher");
            missileLaunchers[i].UpdateMissileLauncher(deltaTime);
            Profiler.EndSample();
        }
    }

    public void UpdateUnitUI(bool showIndicators) {
        if (spriteRenderer.enabled) {
            unitSelection.UpdateUnitSelection(showIndicators);
        }
    }

    public void UpdateDestroyedUnit() {
        if (destroyParticle.isPlaying == false && GetHealth() <= 0) {
            if (IsStation() && ((Station)this).stationType == Station.StationType.FleetCommand)
                return;
            BattleManager.Instance.RemoveDestroyedUnit(this);
            Destroy(gameObject);
        }
    }

    #region UnitControlls
    public void SetRotation(float rotation) {
        transform.eulerAngles = new Vector3(0, 0, rotation);
    }

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
        Debug.LogWarning("Unit not spawned is taking damage");
        return 0;
    }

    public void SelectUnit(UnitSelection.SelectionStrength selectionStrength = UnitSelection.SelectionStrength.Unselected) {
        if (spawned)
            unitSelection.SetSelected(selectionStrength);
    }

    public void UnselectUnit() {
        if (spawned)
            SelectUnit(UnitSelection.SelectionStrength.Unselected);
    }

    public UnitSelection.SelectionType GetSelectionTypeOfUnit() {
        if (LocalPlayer.Instance.GetFaction() == null)
            return UnitSelection.SelectionType.Neutral;
        if (LocalPlayer.Instance.ownedUnits.Contains(this))
            return UnitSelection.SelectionType.Owned;
        if (LocalPlayer.Instance.GetFaction() == faction)
            return UnitSelection.SelectionType.Friendly;
        if (LocalPlayer.Instance.GetFaction().IsAtWarWithFaction(faction))
            return UnitSelection.SelectionType.Enemy;
        return UnitSelection.SelectionType.Neutral;
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

    protected void Spawn() {
        spawned = true;
    }

    protected void Despawn() {
        spawned = false;
    }

    public virtual void Explode() {
        if (!IsSpawned())
            return;
        Despawn();
        health = 0;
        destroyParticle.Play(false);
        ActivateColliders(false);
        spriteRenderer.enabled = false;
        for (int i = 0; i < transform.childCount; i++) {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        DestroyUnit();
    }

    public abstract void DestroyUnit();

    public virtual bool IsSpawned() {
        return spawned;
    }

    public virtual bool IsSelectable() {
        return spawned;
    }

    public virtual bool IsTargetable() {
        return spawned;
    }
    #endregion

    #region GetMethods
    public float GetMaxWeaponRange() {
        return maxWeaponRange;
    }

    public float GetMinWeaponRange() {
        return minWeaponRange;
    }

    public long UseCargo(long amount, CargoBay.CargoTypes cargoType) {
        long totalCargoToUse = amount;
        foreach (var bay in GetCargoBays()) {
            totalCargoToUse = bay.UseCargo(totalCargoToUse, cargoType);
            if (totalCargoToUse <= 0) {
                return 0;
            }
        }
        return totalCargoToUse;
    }

    public long GetAllCargo(CargoBay.CargoTypes cargoType) {
        long totalCargo = 0;
        foreach (var cargo in GetCargoBays()) {
            totalCargo += cargo.GetAllCargo(cargoType);
        }
        return totalCargo;
    }

    public void LoadCargoFromUnit(Unit unit, CargoBay.CargoTypes cargoType) {
        for (int i = 0; i < cargoBays.Count; i++) {
            cargoBays[i].LoadCargoFromBay(unit.GetCargoBays()[i], cargoType);
        }
    }

    public string GetUnitName() {
        return unitName;
    }

    public int GetHealth() {
        return health;
    }
    public int GetMaxHealth() {
        return Mathf.RoundToInt(maxHealth * faction.HealthModifier);
    }

    public int GetTotalHealth() {
        return GetHealth() + GetShields();
    }

    public bool IsDammaged() {
        return health < GetMaxHealth();
    }

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
        int shields = 0;
        foreach (var generator in GetShieldGenerators()) {
            shields += generator.GetShieldStrength();
        }
        return shields;
    }

    public int GetMaxShields() {
        int shields = 0;
        foreach (var generator in GetShieldGenerators()) {
            shields += generator.GetMaxShieldStrenght();
        }
        return shields;
    }

    public int GetCost() {
        return 0;
    }

    public int GetFollowDistance() {
        return followDist;
    }

    public abstract bool Destroyed();

    public List<ShieldGenerator> GetShieldGenerators() {
        List<ShieldGenerator> shieldGenerators = new List<ShieldGenerator>();
        if (GetComponentsInChildren<ShieldGenerator>() != null) {
            foreach (var shieldGenerator in GetComponentsInChildren<ShieldGenerator>()) {
                shieldGenerators.Add(shieldGenerator);
            }
        }
        return shieldGenerators;
    }

    public List<Turret> GetTurrets() {
        List<Turret> turrets = new List<Turret>();
        if (GetComponentsInChildren<Turret>() != null) {
            foreach (var turret in GetComponentsInChildren<Turret>()) {
                turrets.Add(turret);
            }
        }
        return turrets;
    }

    public List<CargoBay> GetCargoBays() {
        return cargoBays;
    }

    public List<Hanger> GetHangers() {
        List<Hanger> hangers = new List<Hanger>();
        if (GetComponentsInChildren<Hanger>() != null) {
            foreach (var hanger in GetComponentsInChildren<Hanger>()) {
                hangers.Add(hanger);
            }
        }
        return hangers;
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

    public bool IsShip() {
        return this is Ship;
    }

    public bool IsStation() {
        return this is Station;
    }

    public virtual List<Unit> GetEnemyUnitsInRange() {
        return enemyUnitsInRange;
    }

    public virtual List<float> GetEnemyUnitsInRangeDistance() {
        return enemyUnitsInRangeDistance;
    }

    public virtual void SetParticleSpeed(float speed) {
        var main = destroyParticle.main;
        main.simulationSpeed = speed;
    }

    [ContextMenu("GetUnitDamagePerSecond")]
    public void GetUnitDamagePerSecond() {
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
        print(unitName + "Dps:" + dps);
    }
    #endregion

}