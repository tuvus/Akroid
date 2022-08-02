using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public abstract class Unit : MonoBehaviour {
    private bool spawned;
    [SerializeField] protected int maxHealth;
    [SerializeField] protected string unitName;
    [SerializeField] protected float size;

    protected int health;
    [SerializeField] protected int followDist;

    public Faction faction { get; protected set; }
    private UnitSelection unitSelection;
    protected SpriteRenderer spriteRenderer;
    protected ParticleSystem destroyParticle;
    protected List<Collider2D> colliders;
    private ShieldGenerator shieldGenerator;
    protected List<CargoBay> cargoBays;
    protected List<Turret> turrets;
    private float maxTurretRange;
    private float minTurretRange;
    protected Vector2 velocity;

    public List<Unit> enemyUnitsInRange { get; protected set; }

    public virtual void SetupUnit(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation) {
        this.unitName = name;
        this.faction = faction;
        health = GetMaxHealth();
        transform.eulerAngles = new Vector3(0, 0, rotation);
        enemyUnitsInRange = new List<Unit>(20);
        cargoBays = new List<CargoBay>(GetComponentsInChildren<CargoBay>());
        turrets = new List<Turret>(GetComponentsInChildren<Turret>());
        unitSelection = GetComponentInChildren<UnitSelection>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        destroyParticle = GetComponent<ParticleSystem>();
        shieldGenerator = GetComponentInChildren<ShieldGenerator>();
        colliders = new List<Collider2D>(GetComponents<Collider2D>());
        size = SetupSize();
        transform.position = GetSetupPosition(positionGiver);
        minTurretRange = -1;
        foreach (var turret in turrets) {
            turret.SetupTurret(this);
        }
        SetupTurretRanges();
        if (shieldGenerator != null)
            shieldGenerator.SetupShieldGenerator(this);
        unitSelection.SetupSelection(this);
    }

    public void SetupTurretRanges() {
        foreach (var turret in turrets) {
            maxTurretRange = Mathf.Max(maxTurretRange, turret.GetRange());
            if (minTurretRange <= 0)
                minTurretRange = turret.GetRange();
            else
                minTurretRange = Mathf.Min(minTurretRange, turret.GetRange());
        }
    }

    protected virtual float SetupSize() {
        return GetSpriteSize();
    }

    protected virtual Vector2 GetSetupPosition(BattleManager.PositionGiver position) {
        return position.position;
    }

    public virtual void UpdateUnit() {
        if (IsSpawned()) {
            velocity = Vector2.zero;
            if (turrets.Count > 0) {
                Profiler.BeginSample("FindingEnemies");
                enemyUnitsInRange.Clear();
                foreach (var enemyFaction in faction.enemyFactions) {
                    if (Vector2.Distance(GetPosition(), enemyFaction.factionPosition) > maxTurretRange * 2 + enemyFaction.factionUnitsSize)
                        continue;
                    for (int i = 0; i < enemyFaction.units.Count; i++) {
                        Unit targetUnit = enemyFaction.units[i];
                        if (targetUnit != null && targetUnit.IsTargetable() && targetUnit.faction != faction && Vector2.Distance(transform.position, targetUnit.GetPosition()) <= maxTurretRange * 2) {
                            enemyUnitsInRange.Add(targetUnit);
                        }
                    }
                }
                Profiler.EndSample();
                for (int i = 0; i < turrets.Count; i++) {
                    Profiler.BeginSample("Turret" + i);
                    turrets[i].UpdateTurret();
                    Profiler.EndSample();
                }
            }
            if (shieldGenerator != null) {
                Profiler.BeginSample("ShieldGenerator");
                shieldGenerator.UpdateShieldGenerator();
                Profiler.EndSample();
            }
        }
    }

    public void UpdateUnitUI(bool showIndicators) {
        if (spriteRenderer.enabled) {
            unitSelection.UpdateUnitSelection(showIndicators);
        }
    }

    public void UpdateDestroyedUnit() {
        if (destroyParticle.isPlaying == false && GetHealth() <= 0) {
            if (IsStation() && ((Station)this).stationType == Station.StationType.FleetCommmand)
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
    #endregion

    #region GetMethods
    public float GetMaxTurretRange() {
        return maxTurretRange;
    }

    public float GetMinTurretRange() {
        return minTurretRange;
    }

    public float UseCargo(float amount, CargoBay.CargoTypes cargoType) {
        float totalCargoToUse = amount;
        foreach (var bay in GetCargoBays()) {
            totalCargoToUse = bay.UseCargo(totalCargoToUse, cargoType);
            if (totalCargoToUse <= 0) {
                return 0;
            }
        }
        return totalCargoToUse;
    }

    public float GetAllCargo(CargoBay.CargoTypes cargoType) {
        float totalCargo = 0;
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
        return health < maxHealth;
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

    public Vector2 GetPosition() {
        return transform.position;
    }

    public virtual Vector2 GetVelocity() {
        return velocity;
    }

    public float GetRotation() {
        return transform.eulerAngles.z;
    }

    public float GetSize() {
        return size;
    }

    public float GetZoomIndicatorSize() {
        return unitSelection.GetSize();
    }

    public SpriteRenderer GetSpriteRenderer() {
        return spriteRenderer;
    }

    public UnitSelection GetUnitSelection() {
        return unitSelection;
    }


    public float GetSpriteSize() {
        return Mathf.Max(Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.y, spriteRenderer.sprite.bounds.size.z)),
Vector2.Distance(spriteRenderer.sprite.bounds.center, new Vector2(spriteRenderer.sprite.bounds.size.z, spriteRenderer.sprite.bounds.size.x))) / 2;
    }

    public virtual bool IsSelectable() {
        return spawned;
    }

    public virtual bool IsTargetable() {
        return spawned;
    }

    public bool IsShip() {
        return this is Ship;
    }

    public bool IsStation() {
        return this is Station;

    }
    #endregion

}