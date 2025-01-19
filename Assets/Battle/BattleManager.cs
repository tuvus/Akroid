using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using static Faction;
using static Ship;
using static Station;
using Random = Unity.Mathematics.Random;

public class BattleManager : MonoBehaviour {
    public static BattleManager Instance { get; protected set; }
    private CampaingController campaignController;
    public EventManager eventManager { get; private set; }

    [field: SerializeField] public float researchModifier { get; private set; }
    [field: SerializeField] public float systemSizeModifier { get; private set; }
    public List<ShipBlueprint> shipBlueprints;
    public List<StationBlueprint> stationBlueprints;
    public List<AsteroidScriptableObject> asteroidBlueprints;
    public List<GasCloudScriptableObject> gasCloudBlueprints;
    public List<StarScriptableObject> starBlueprints;

    public HashSet<Faction> factions { get; private set; }
    public HashSet<BattleObject> battleObjects { get; private set; }
    public HashSet<Unit> units { get; private set; }
    public HashSet<Ship> ships { get; private set; }
    public HashSet<Station> stations { get; private set; }
    public HashSet<Station> stationsInProgress { get; private set; }
    public HashSet<Projectile> projectiles { get; private set; }
    public HashSet<Missile> missiles { get; private set; }
    public HashSet<Star> stars { get; private set; }
    public HashSet<Planet> planets { get; private set; }
    public HashSet<AsteroidField> asteroidFields { get; private set; }
    public HashSet<GasCloud> gasClouds { get; private set; }

    public HashSet<Unit> destroyedUnits { get; private set; }
    public HashSet<Projectile> usedProjectiles { get; private set; }
    public HashSet<Projectile> unusedProjectiles { get; private set; }
    public HashSet<Missile> usedMissiles { get; private set; }
    public HashSet<Missile> unusedMissiles { get; private set; }
    public HashSet<Player> players { get; private set; }

    public event Action<Faction> OnBattleEnd = delegate { };
    public event Action<IObject> OnObjectCreated = delegate { };
    public event Action<IObject> OnObjectRemoved = delegate { };

    [SerializeField] private bool threaded = true;
    public bool instantHit;
    public float timeScale;

    float startOfSimulation;
    double simulationTime;
    [field: SerializeField] public BattleState battleState { get; private set; } = BattleState.SettingUp;
    private Random random;

    public enum BattleState {
        SettingUp,
        Setup,
        Running,
        Ended
    }

    public struct PositionGiver {
        public Vector2 position;
        public bool isExactPosition;
        public float minDistance;
        public float maxDistance;
        public float incrementDistance;
        public float distanceFromObject;
        public int numberOfTries;

        public PositionGiver(Vector2 position) {
            this.position = position;
            this.isExactPosition = true;
            minDistance = 0;
            maxDistance = 0;
            incrementDistance = 0;
            distanceFromObject = 0;
            numberOfTries = 0;
        }

        public PositionGiver(Vector2 position, PositionGiver oldPositionGiver) {
            this.position = position;
            this.isExactPosition = false;
            this.minDistance = oldPositionGiver.minDistance;
            this.maxDistance = oldPositionGiver.maxDistance;
            this.incrementDistance = oldPositionGiver.incrementDistance;
            this.distanceFromObject = oldPositionGiver.distanceFromObject;
            this.numberOfTries = oldPositionGiver.numberOfTries;
        }

        public PositionGiver(Vector2 position, float minDistance, float maxDistance, float incrementDistance, float distanceFromObject,
            int numberOfTries) {
            this.position = position;
            this.isExactPosition = false;
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
            this.incrementDistance = incrementDistance;
            this.distanceFromObject = distanceFromObject;
            this.numberOfTries = numberOfTries;
        }
    }

    public struct BattleSettings {
        public int starCount;
        public int asteroidFieldCount;
        public float asteroidCountModifier;
        public int gasCloudCount;
        public float systemSizeModifier;
        public float researchModifier;
    }

    #region Setup

    public void InitializeBattle() {
        timeScale = 1;
        startOfSimulation = Time.unscaledTime;
        random = new Random((uint)(startOfSimulation * 1000));
        factions = new HashSet<Faction>(10);
        battleObjects = new HashSet<BattleObject>(1000);
        units = new HashSet<Unit>(200);
        ships = new HashSet<Ship>(150);
        stations = new HashSet<Station>(50);
        stationsInProgress = new HashSet<Station>(10);
        stars = new HashSet<Star>(10);
        planets = new HashSet<Planet>(10);
        asteroidFields = new HashSet<AsteroidField>(10);
        gasClouds = new HashSet<GasCloud>(10);
        projectiles = new HashSet<Projectile>(500);
        missiles = new HashSet<Missile>(100);
        destroyedUnits = new HashSet<Unit>(200);
        usedProjectiles = new HashSet<Projectile>(500);
        unusedProjectiles = new HashSet<Projectile>(500);
        usedMissiles = new HashSet<Missile>(100);
        unusedMissiles = new HashSet<Missile>(100);
        players = new HashSet<Player>();

        for (int i = 0; i < 100; i++) {
            PreSpawnNewProjectile();
        }

        for (int i = 0; i < 20; i++) {
            PrespawnNewMissile();
        }

        if (eventManager == null) eventManager = new EventManager(this);

        shipBlueprints.ForEach(b => Resources.Load<GameObject>(b.shipScriptableObject.prefabPath).GetComponent<PrefabModuleSystem>()
            .modules.ForEach(m => m.SetupData()));
        stationBlueprints.ForEach(b => Resources.Load<GameObject>(b.stationScriptableObject.prefabPath).GetComponent<PrefabModuleSystem>()
            .modules.ForEach(m => m.SetupData()));
    }

    /// <summary>
    /// Sets up the battle with manual values.
    /// </summary>
    public void SetupBattle(BattleSettings battleSettings, List<FactionData> factionDatas) {
        if (Instance == null) {
            Instance = this;
        } else {
            return;
        }

        this.systemSizeModifier = battleSettings.systemSizeModifier;
        this.researchModifier = battleSettings.researchModifier;
        for (int i = 0; i < battleSettings.starCount; i++) {
            CreateNewStar("Star" + (i + 1));
        }

        for (int i = 0; i < battleSettings.asteroidFieldCount; i++) {
            CreateNewAsteroidField(Vector2.zero,
                (int)random.NextFloat(6 * battleSettings.asteroidCountModifier, 14 * battleSettings.asteroidCountModifier));
        }

        for (int i = 0; i < factionDatas.Count; i++) {
            CreateNewFaction(factionDatas[i], new PositionGiver(Vector2.zero, 0, 1000000, 100, 1000, 10), 100);
        }

        for (int i = 0; i < battleSettings.gasCloudCount; i++) {
            CreateNewGasCloud(new PositionGiver(Vector2.zero, 1000, 100000, 500, 2000, 3));
        }

        foreach (var faction in factions) {
            faction.GetFleetCommand().LoadCargo(2400 * 4, CargoBay.CargoTypes.Gas);
            foreach (var faction2 in factions) {
                if (faction == faction2) continue;
                faction.AddEnemyFaction(faction2);
            }
        }

        Player localPlayer = new Player(true);
        players.Add(localPlayer);
        if (factions.Count > 0) localPlayer.SetFaction(factions.First((f) => factionDatas.Any((d) => d.name == f.name)));
        else localPlayer.SetFaction(null);

        battleState = BattleState.Running;
        eventManager.AddEvent(eventManager.CreateVictoryCondition(),
            () => { EndBattle(factions.ToList().First(f => f.units.Count > 0 && !f.HasEnemy())); }
        );
        battleState = BattleState.Setup;
    }

    /// <summary>
    /// Sets up the battle with a CampaignController, doesn't spawn any asteroids or stars.
    /// Gets the BattleSettings from the CampaignController.
    /// </summary>
    /// <param name="campaignController">the given CampaignController</param>
    public void SetupBattle(CampaingController campaignController) {
        if (Instance == null) {
            Instance = this;
        } else {
            return;
        }

        this.campaignController = campaignController;
        systemSizeModifier = campaignController.systemSizeModifier;
        researchModifier = campaignController.researchModifier;
        Player LocalPlayer = new Player(true);
        players.Add(LocalPlayer);
        LocalPlayer.SetFaction(null);
        campaignController.SetupBattle(this);
        foreach (var faction in factions) {
            faction.UpdateObjectGroup();
        }

        battleState = BattleState.Setup;
    }

    public void StartBattle() {
        battleState = BattleState.Running;
    }

    #endregion

    #region Spawning

    /// <summary>
    /// Finds a position around a center location that is minDistanceFromStationOrAsteroid and less than maxDistance from the center point.
    /// If the center location is an asteroid or station isCenterObject should be true.
    /// </summary>
    /// <returns></returns>
    public Vector2? FindFreeLocation(PositionGiver positionGiver, IPositionConfirmer positionConfirmer, float minRange, float maxRange) {
        for (int i = 0; i < positionGiver.numberOfTries; i++) {
            float distance = random.NextFloat(minRange, maxRange);
            Vector2 tryPos = positionGiver.position + Calculator.GetPositionOutOfAngleAndDistance(random.NextFloat(0f, 360f), distance);
            if (positionConfirmer.ConfirmPosition(tryPos, positionGiver.distanceFromObject * systemSizeModifier)) {
                return tryPos;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds a position around a center location that is minDistanceFromStationOrAsteroid and less than maxDistance from the center point.
    /// If the center location is an asteroid or station isCenterObject should be true.
    /// If the point cannot be found then it increases the search distance.
    /// </summary>
    /// <returns></returns>
    public Vector2? FindFreeLocationIncrement(PositionGiver positionGiver, IPositionConfirmer positionConfirmer) {
        float distance = positionGiver.minDistance * systemSizeModifier;
        if (positionGiver.numberOfTries == 0) return positionGiver.position;
        while (true) {
            Vector2? targetPosition = FindFreeLocation(positionGiver, positionConfirmer, distance,
                distance + positionGiver.incrementDistance * systemSizeModifier);
            if (targetPosition.HasValue) {
                return targetPosition.Value;
            }

            distance += positionGiver.incrementDistance * math.max(.001f, systemSizeModifier);
            if (distance > (positionGiver.maxDistance - positionGiver.incrementDistance) * systemSizeModifier) {
                return null;
            }
        }
    }

    public Faction CreateNewFaction(FactionData factionData, PositionGiver positionGiver, int startingResearchCost) {
        Faction newFaction = new Faction(this, factionData, positionGiver);
        factions.Add(newFaction);
        OnObjectCreated.Invoke(newFaction);
        newFaction.GenerateFaction(factionData, startingResearchCost);
        return newFaction;
    }

    public Ship CreateNewShip(BattleObject.BattleObjectData battleObjectData, ShipScriptableObject shipScriptableObject) {
        Ship newShip = new Ship(battleObjectData, this, shipScriptableObject);
        newShip.SetupPosition(battleObjectData.positionGiver);
        units.Add(newShip);
        ships.Add(newShip);
        AddBattleObject(newShip);
        return newShip;
    }

    public Station CreateNewStation(BattleObject.BattleObjectData battleObjectData, StationScriptableObject stationScriptableObject,
        bool built) {
        Station newStation;
        if (stationScriptableObject.stationType == StationType.Shipyard ||
            stationScriptableObject.stationType == StationType.FleetCommand ||
            stationScriptableObject.stationType == StationType.TradeStation) {
            newStation = new Shipyard(battleObjectData, this, stationScriptableObject, built);
        } else if (stationScriptableObject.stationType == StationType.MiningStation) {
            newStation = new MiningStation(battleObjectData, this, (MiningStationScriptableObject)stationScriptableObject, built);
            ((MiningStation)newStation).GetMiningStationAI().SetupMiningStation();
        } else newStation = new Station(battleObjectData, this, stationScriptableObject, built);

        newStation.SetupPosition(battleObjectData.positionGiver);
        if (built) {
            units.Add(newStation);
            stations.Add(newStation);
        } else {
            stationsInProgress.Add(newStation);
        }

        AddBattleObject(newStation);

        return newStation;
    }

    public MiningStation CreateNewMiningStation(BattleObject.BattleObjectData battleObjectData,
        MiningStationScriptableObject miningStationScriptableObject, bool built) {
        MiningStation newStation = new MiningStation(battleObjectData, this, miningStationScriptableObject, built);
        newStation.SetupPosition(battleObjectData.positionGiver);
        if (built) {
            units.Add(newStation);
            stations.Add(newStation);
            newStation.GetMiningStationAI().SetupMiningStation();
        } else {
            stationsInProgress.Add(newStation);
        }

        AddBattleObject(newStation);
        return newStation;
    }

    public Star CreateNewStar(string name) {
        Star newStar = new Star(new BattleObject.BattleObjectData(name, Vector2.zero, random.NextFloat(0, 360),
            new Vector2(10, 10) * random.NextFloat(0.6f, 1.8f)), this, starBlueprints[random.NextInt(0, starBlueprints.Count)]);
        newStar.SetupPosition(new PositionGiver(Vector2.zero, 1000, 100000, 100, 5000, 4));
        stars.Add(newStar);
        AddBattleObject(newStar);
        return newStar;
    }

    public Planet CreateNewPlanet(Planet.PlanetData planetData, PlanetScriptableObject planetScriptableObject) {
        Planet newPlanet = new Planet(planetData, this, planetScriptableObject);
        newPlanet.SetupPosition(planetData.battleObjectData.positionGiver);
        planets.Add(newPlanet);
        AddBattleObject(newPlanet);
        return newPlanet;
    }

    public Planet CreateNewMoon(Planet.PlanetData planetData, PlanetScriptableObject planetScriptableObject) {
        Planet newPlanet = new Planet(planetData, this, planetScriptableObject);
        newPlanet.SetupPosition(planetData.battleObjectData.positionGiver);
        planets.Add(newPlanet);
        AddBattleObject(newPlanet);
        return newPlanet;
    }

    public void CreateNewAsteroidField(Vector2 center, int count, float resourceModifier = 1) {
        CreateNewAsteroidField(new PositionGiver(center, 100, 100000, 500, 1000, 2), count, resourceModifier);
    }

    public void CreateNewAsteroidField(PositionGiver positionGiver, int count, float resourceModifier = 1) {
        AsteroidField newAsteroidField = new AsteroidField(this);
        // We need to generate the asteroids first which can only collide with other asteroids in the same field
        for (int i = 0; i < count; i++) {
            float size = random.NextFloat(8f, 20f);
            Asteroid newAsteroid = new Asteroid(new BattleObject.BattleObjectData("Asteroid", Vector2.zero,
                    random.NextFloat(0, 360), Vector2.one * size), this, newAsteroidField,
                (long)(random.NextFloat(400, 600) * size * resourceModifier),
                asteroidBlueprints[random.NextInt(0, asteroidBlueprints.Count)]);
            newAsteroid.SetupPosition(new PositionGiver(Vector2.zero, 0, 1000, 50, random.NextFloat(0, 10), 4));
            newAsteroidField.battleObjects.Add(newAsteroid);
            AddBattleObject(newAsteroid);
        }

        // The Asteroid field position must be set after the asteroids have been generated so that we know the size of the asteroid field
        newAsteroidField.SetupAsteroidFieldPosition(positionGiver);
        asteroidFields.Add(newAsteroidField);
        AddObject(newAsteroidField);
    }

    public void CreateNewGasCloud(PositionGiver positionGiver, float resourceModifier = 1) {
        float size = random.NextFloat(20, 40);
        GasCloud newGasCloud = new GasCloud(
            new BattleObject.BattleObjectData("Gas Cloud", Vector2.zero, random.NextFloat(0, 360), Vector2.one * size), this,
            (long)(random.NextFloat(1500, 3500) * size * resourceModifier),
            gasCloudBlueprints[random.NextInt(0, gasCloudBlueprints.Count)]);
        newGasCloud.SetupPosition(positionGiver);
        gasClouds.Add(newGasCloud);
        AddBattleObject(newGasCloud);
    }

    #endregion

    #region ObjectLists

    private void AddObject(IObject iObject) {
        OnObjectCreated.Invoke(iObject);
    }

    private void RemoveObject(IObject iObject) {
        OnObjectRemoved.Invoke(iObject);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void AddBattleObject(BattleObject battleObject) {
        battleObjects.Add(battleObject);
        AddObject(battleObject);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    private void RemoveBattleObject(BattleObject battleObject) {
        battleObjects.Remove(battleObject);
        RemoveObject(battleObject);
    }

    public void BuildStationBlueprint(Station station) {
        stationsInProgress.Remove(station);
        units.Add(station);
        stations.Add(station);
    }

    public void DestroyShip(Ship ship) {
        units.Remove(ship);
        ships.Remove(ship);
        if (ship.faction != null)
            ship.faction.RemoveShip(ship);
        if (destroyedUnits.Contains(ship)) throw new System.Exception("The ship that was trying to be destroyed was already destroyed");
        destroyedUnits.Add(ship);
    }

    public void DestroyStation(Station station) {
        if (station.IsBuilt()) {
            units.Remove(station);
            stations.Remove(station);
            if (station.faction != null)
                station.faction.RemoveStation(station);
        } else {
            stationsInProgress.Remove(station);
            if (station.faction != null)
                station.faction.RemoveStationBlueprint(station);
        }

        destroyedUnits.Add(station);
    }

    public void RemoveDestroyedUnit(Unit unit) {
        destroyedUnits.Remove(unit);
        RemoveBattleObject(unit);
    }

    public void AddProjectile(Projectile projectile) {
        AddBattleObject(projectile);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void RemoveProjectile(Projectile projectile) {
        usedProjectiles.Remove(projectile);
        unusedProjectiles.Add(projectile);
        RemoveBattleObject(projectile);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Projectile GetNewProjectile() {
        if (unusedProjectiles.Count == 0) PreSpawnNewProjectile();

        Projectile projectile = unusedProjectiles.First();
        unusedProjectiles.Remove(projectile);
        usedProjectiles.Add(projectile);
        return projectile;
    }

    public void PreSpawnNewProjectile() {
        Projectile newProjectile = new Projectile(this);
        projectiles.Add(newProjectile);
        unusedProjectiles.Add(newProjectile);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void AddMissile(Missile missile) {
        AddBattleObject(missile);
    }

    public void RemoveMissile(Missile missile) {
        usedMissiles.Remove(missile);
        unusedMissiles.Add(missile);
        RemoveBattleObject(missile);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Missile GetNewMissile() {
        if (unusedMissiles.Count == 0) PrespawnNewMissile();

        Missile missile = unusedMissiles.First();
        usedMissiles.Add(missile);
        unusedMissiles.Remove(missile);
        return missile;
    }

    public void PrespawnNewMissile() {
        Missile newMissile = new Missile(this);
        missiles.Add(newMissile);
        unusedMissiles.Add(newMissile);
    }

    public void CreateFleet(Fleet fleet) {
        AddObject(fleet);
        OnObjectCreated.Invoke(fleet);
    }

    public void RemoveFleet(Fleet fleet) {
        RemoveObject(fleet);
    }

    public List<IPositionConfirmer> GetPositionBlockingObjects() {
        var blockingObjects = new List<IPositionConfirmer>();
        blockingObjects.AddRange(stars);
        blockingObjects.AddRange(stations);
        blockingObjects.AddRange(planets);
        blockingObjects.AddRange(asteroidFields);
        blockingObjects.AddRange(gasClouds);
        return blockingObjects;
    }

    #endregion

    /// <summary>
    /// Updates the faction AI, units, projectiles etc owned by this faction based on the time elapsed.
    ///
    /// Also has profiling for most method calls.
    /// </summary>
    public virtual void FixedUpdate() {
        if (battleState != BattleState.Running) return;
        float deltaTime = Time.fixedDeltaTime * timeScale;
        simulationTime += deltaTime;

        if (PlayerPrefs.HasKey("Threading")) {
            threaded = PlayerPrefs.GetInt("Threading") == 1;
        }

        UpdateCollection(factions, f => f.EarlyUpdateFaction(), "EarlyFactionsUpdate");
        UpdateCollection(factions, f => f.UpdateNearbyEnemyUnits(), "FactionsFindingEnemies");
        UpdateCollection(factions, f => f.UpdateFaction(deltaTime), "FactionUpdate", false);
        UpdateCollection(factions.SelectMany(f => f.fleets), f => f.FindEnemies(), "FleetsFindingEnemies");
        UpdateCollection(factions, f => f.UpdateFleets(deltaTime), "FleetsUpdate", false);
        UpdateCollection(units.Where(u => !u.IsShip() || ((Ship)u).fleet == null).ToList(), u => u.FindEnemies(), "UnitsFindingEnemies");
        UpdateCollection(units.ToList(), u => {
            Profiler.BeginSample(u.GetUnitName());
            u.UpdateUnit(deltaTime);
            Profiler.EndSample();
        }, "UnitsUpdate", false);
        UpdateCollection(units, u => u.UpdateWeapons(deltaTime), "UnitWeaponsUpdate");
        UpdateCollection(usedProjectiles.ToList(), p => p.UpdateProjectile(deltaTime), "ProjectilesUpdate");
        UpdateCollection(usedMissiles.ToList(), m => m.UpdateMissile(deltaTime), "MissilesUpdate");
        UpdateCollection(destroyedUnits.ToList(), u => u.UpdateDestroyedUnit(deltaTime), "DestroyedUnitsUpdate", false);
        UpdateCollection(stars, s => s.UpdateStar(deltaTime), "StarsUpdate", false);
        UpdateCollection(planets, p => p.UpdatePlanet(deltaTime), "PlanetsUpdate", false);

        eventManager.UpdateEvents(deltaTime);
    }

    /// <summary>
    /// Handles apply an action to a collection of objects in parallel or serial depending on the input
    /// and if the player want to use multithreading.
    /// </summary>
    public void UpdateCollection<T>(IEnumerable<T> collection, Action<T> action, String profileName, bool parallel = true) {
        Profiler.BeginSample(profileName);
        if (threaded && parallel) {
            Parallel.ForEach(collection, c => action.Invoke(c));
        } else {
            foreach (var c in collection) {
                action.Invoke(c);
            }
        }

        Profiler.EndSample();
    }

    #region HelperMethods

    public void EndBattle(Faction faction) {
        OnBattleEnd(faction);
        battleState = BattleState.Ended;
        Time.timeScale = 0;
    }

    public double GetSimulationTime() {
        return simulationTime;
    }

    [ContextMenu("UpdateSimulationTimeScale")]
    private void ManualUpdateTimeScale() {
        SetSimulationTimeScale(timeScale);
    }

    /// <summary>
    /// Sets the playbackSpeed of all particles in the game.
    /// </summary>
    /// <param name="time"></param>
    public void SetSimulationTimeScale(float time) {
        timeScale = time;
        // foreach (var unit in units) {
        //     unit.SetParticleSpeed(time);
        // }
        // foreach (var projectile in projectiles) {
        //     projectile.SetParticleSpeed(time);
        // }
        // foreach (var missile in missiles) {
        //     missile.SetParticleSpeed(time);
        // }
        instantHit = time > 10;
    }

    public double GetRealTime() {
        return Time.unscaledTime - startOfSimulation;
    }

    public ShipBlueprint GetShipBlueprint(ShipClass shipClass) {
        return shipBlueprints.ToList().First(ship => ship.shipScriptableObject.shipClass == shipClass);
    }

    public ShipBlueprint GetShipBlueprint(ShipType shipType) {
        return shipBlueprints.ToList().First(ship => ship.shipScriptableObject.shipType == shipType);
    }

    public StationBlueprint GetStationBlueprint(StationType stationType) {
        return stationBlueprints.First(station => station.stationScriptableObject.stationType == stationType);
    }

    public static GameObject GetSizeIndicatorPrefab() {
        return Resources.Load<GameObject>("Prefabs/SizeIndicator");
    }

    public Player GetLocalPlayer() {
        return players.First(p => p.isLocalPlayer);
    }

    public void SetEventManager(EventManager eventManager) {
        if (this.eventManager != null)
            throw new AggregateException("Trying to set the BattleManager EventManager after the EventManager has already been set!");
        this.eventManager = eventManager;
    }

    public uint GetRandomSeed() {
        return random.NextUInt();
    }

    #endregion
}
