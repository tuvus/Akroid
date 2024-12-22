using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using static Faction;
using static Ship;
using static Station;
using Random = UnityEngine.Random;

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
    public HashSet<BattleObject> objects { get; private set; }
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
    public event Action<Faction> OnFactionCreated = delegate { };
    public event Action<BattleObject> OnObjectCreated = delegate { };
    public event Action<BattleObject> OnObjectRemoved = delegate { };
    public event Action<Fleet> OnFleetCreated = delegate { };
    public event Action<Fleet> OnFleetRemoved = delegate { };

    public bool instantHit;
    public float timeScale;
    public static bool quickStart = true;

    float startOfSimulation;
    double simulationTime;
    [field: SerializeField] public BattleState battleState { get; private set; }

    public enum BattleState {
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

    /// <summary>
    /// Sets up the battle with only two factions, used for debugging.
    /// Two factions means better performance for faster debugging.
    /// </summary>
    protected virtual void Start() {
        battleState = BattleState.Setup;
        if (quickStart == true) {
            Debug.Log("Setting up test scene");
            ColorPicker colorPicker = new ColorPicker();
            List<FactionData> tempFactions = new List<FactionData> {
                new FactionData("Faction1", "F1", colorPicker.PickColor(), Random.Range(50000, 80000), 0, 50, 1),
                new FactionData("Faction2", "F2", colorPicker.PickColor(), Random.Range(50000, 80000), 0, 50, 1)
            };
            SetupBattle(new BattleSettings { asteroidCountModifier = 1, systemSizeModifier = 0.1f, researchModifier = 1.1f }, tempFactions);
        }
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
        InitializeBattle();
        for (int i = 0; i < battleSettings.starCount; i++) {
            CreateNewStar("Star" + (i + 1));
        }

        for (int i = 0; i < battleSettings.asteroidFieldCount; i++) {
            CreateNewAsteroidField(Vector2.zero,
                (int)Random.Range(6 * battleSettings.asteroidCountModifier, 14 * battleSettings.asteroidCountModifier));
        }

        for (int i = 0; i < factionDatas.Count; i++) {
            CreateNewFaction(factionDatas[i], new PositionGiver(Vector2.zero, 0, 1000000, 100, 1000, 10), 100);
        }

        for (int i = 0; i < battleSettings.gasCloudCount; i++) {
            CreateNewGasCloud(new PositionGiver(Vector2.zero, 1000, 100000, 500, 2000, 3));
        }

        foreach (var faction in factions) {
            faction.GetFleetCommand().LoadCargo(2400, CargoBay.CargoTypes.Gas);
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
        InitializeBattle();
        Player LocalPlayer = new Player(true);
        players.Add(LocalPlayer);
        LocalPlayer.SetFaction(null);
        campaignController.SetupBattle(this);
        foreach (var faction in factions) {
            faction.UpdateObjectGroup();
        }

        battleState = BattleState.Running;
    }

    private void InitializeBattle() {
        factions = new HashSet<Faction>(10);
        objects = new HashSet<BattleObject>(1000);
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

        timeScale = 1;
        startOfSimulation = Time.unscaledTime;

        if (eventManager == null) eventManager = new EventManager(this);
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
            float distance = Random.Range(minRange, maxRange);
            Vector2 tryPos = positionGiver.position + Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0f, 360f), distance);
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
        Faction newFaction = new Faction(this, factionData, positionGiver, startingResearchCost);
        factions.Add(newFaction);
        OnFactionCreated.Invoke(newFaction);
        return newFaction;
    }

    public Ship CreateNewShip(BattleObject.BattleObjectData battleObjectData, ShipScriptableObject shipScriptableObject) {
        Ship newShip = new Ship(battleObjectData, this, shipScriptableObject);
        newShip.SetupPosition(battleObjectData.positionGiver);
        units.Add(newShip);
        ships.Add(newShip);
        AddObject(newShip);
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
        } else newStation = new Station(battleObjectData, this, stationScriptableObject, built);

        newStation.SetupPosition(battleObjectData.positionGiver);
        if (built) {
            units.Add(newStation);
            stations.Add(newStation);
        } else {
            stationsInProgress.Add(newStation);
        }

        AddObject(newStation);

        return newStation;
    }

    public MiningStation CreateNewMiningStation(BattleObject.BattleObjectData battleObjectData,
        MiningStationScriptableObject miningStationScriptableObject, bool built) {
        MiningStation newStation = new MiningStation(battleObjectData, this, miningStationScriptableObject, built);
        newStation.SetupPosition(battleObjectData.positionGiver);
        if (built) {
            units.Add(newStation);
            stations.Add(newStation);
        } else {
            stationsInProgress.Add(newStation);
        }

        AddObject(newStation);
        return newStation;
    }

    public Star CreateNewStar(string name) {
        Star newStar = new Star(new BattleObject.BattleObjectData(name, Vector2.zero, Random.Range(0, 360),
            new Vector2(20, 20) * Random.Range(0.6f, 1.8f)), this, starBlueprints[Random.Range(0, starBlueprints.Count)]);
        newStar.SetupPosition(new PositionGiver(Vector2.zero, 1000, 100000, 100, 5000, 4));
        stars.Add(newStar);
        AddObject(newStar);
        return newStar;
    }

    public Planet CreateNewPlanet(Planet.PlanetData planetData, PlanetScriptableObject planetScriptableObject) {
        Planet newPlanet = new Planet(planetData, this, planetScriptableObject);
        newPlanet.SetupPosition(planetData.battleObjectData.positionGiver);
        planets.Add(newPlanet);
        AddObject(newPlanet);
        return newPlanet;
    }

    public Planet CreateNewMoon(Planet.PlanetData planetData, PlanetScriptableObject planetScriptableObject) {
        Planet newPlanet = new Planet(planetData, this, planetScriptableObject);
        newPlanet.SetupPosition(planetData.battleObjectData.positionGiver);
        planets.Add(newPlanet);
        AddObject(newPlanet);
        return newPlanet;
    }

    public void CreateNewAsteroidField(Vector2 center, int count, float resourceModifier = 1) {
        CreateNewAsteroidField(new PositionGiver(center, 100, 100000, 500, 1000, 2), count, resourceModifier);
    }

    public void CreateNewAsteroidField(PositionGiver positionGiver, int count, float resourceModifier = 1) {
        AsteroidField newAsteroidField = new AsteroidField(this);
        // We need to generate the asteroids first which can only collide with other asteroids in the same field
        for (int i = 0; i < count; i++) {
            float size = Random.Range(8f, 20f);
            Asteroid newAsteroid = new Asteroid(new BattleObject.BattleObjectData("Asteroid", Vector2.zero,
                    Random.Range(0, 360), Vector2.one * size), this, newAsteroidField,
                (long)(Random.Range(400, 600) * size * resourceModifier), asteroidBlueprints[Random.Range(0, asteroidBlueprints.Count)]);
            newAsteroid.SetupPosition(new PositionGiver(Vector2.zero, 0, 1000, 50, Random.Range(0, 10), 4));
            newAsteroidField.battleObjects.Add(newAsteroid);
            AddObject(newAsteroid);
        }

        // The Asteroid field position must be set after the asteroids have been generated so that we know the size of the asteroid field
        newAsteroidField.SetupAsteroidFieldPosition(positionGiver);
        asteroidFields.Add(newAsteroidField);
    }

    public void CreateNewGasCloud(PositionGiver positionGiver, float resourceModifier = 1) {
        float size = Random.Range(40, 80);
        GasCloud newGasCloud = new GasCloud(
            new BattleObject.BattleObjectData("Gas Cloud", Vector2.zero, Random.Range(0, 360), Vector2.one * size), this,
            (long)(Random.Range(1500, 3500) * size * resourceModifier), gasCloudBlueprints[Random.Range(0, gasCloudBlueprints.Count)]);
        newGasCloud.SetupPosition(positionGiver);
        gasClouds.Add(newGasCloud);
        AddObject(newGasCloud);
    }

    #endregion

    #region ObjectLists

    private void AddObject(BattleObject battleObject) {
        objects.Add(battleObject);
        OnObjectCreated.Invoke(battleObject);
    }

    private void RemoveObject(BattleObject battleObject) {
        objects.Remove(battleObject);
        OnObjectRemoved.Invoke(battleObject);
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
        RemoveObject(unit);
    }

    public void AddProjectile(Projectile projectile) {
        usedProjectiles.Add(projectile);
        unusedProjectiles.Remove(projectile);
        AddObject(projectile);
    }

    public void RemoveProjectile(Projectile projectile) {
        usedProjectiles.Remove(projectile);
        unusedProjectiles.Add(projectile);
        RemoveObject(projectile);
    }

    public Projectile GetNewProjectile() {
        if (unusedProjectiles.Count == 0) {
            PreSpawnNewProjectile();
        }

        return unusedProjectiles.First();
    }

    public void PreSpawnNewProjectile() {
        Projectile newProjectile = new Projectile(this);
        projectiles.Add(newProjectile);
        unusedProjectiles.Add(newProjectile);
    }

    public void AddMissile(Missile missile) {
        usedMissiles.Add(missile);
        unusedMissiles.Remove(missile);
        AddObject(missile);
    }

    public void RemoveMissile(Missile missile) {
        usedMissiles.Remove(missile);
        unusedMissiles.Add(missile);
        RemoveObject(missile);
    }

    public Missile GetNewMissile() {
        if (unusedMissiles.Count == 0) {
            PrespawnNewMissile();
        }

        return unusedMissiles.First();
    }

    public void PrespawnNewMissile() {
        Missile newMissile = new Missile(this);
        missiles.Add(newMissile);
        unusedMissiles.Add(newMissile);
    }

    public void CreateFleet(Fleet fleet) {
        OnFleetCreated.Invoke(fleet);
    }

    public void RemoveFleet(Fleet fleet) {
        OnFleetRemoved.Invoke(fleet);
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
        if (battleState == BattleState.Setup) return;
        float deltaTime = Time.fixedDeltaTime * timeScale;
        simulationTime += deltaTime;
        foreach (var faction in factions.ToList()) {
            Profiler.BeginSample("EarlyFactionUpdate");
            faction.EarlyUpdateFaction();
            Profiler.EndSample();
        }

        foreach (var faction in factions.ToList()) {
            Profiler.BeginSample("FactionUpdate");
            faction.UpdateFaction(deltaTime);
            Profiler.EndSample();
        }

        foreach (var faction in factions.ToList()) {
            faction.UpdateFleets(deltaTime);
        }

        foreach (var unit in units.ToList()) {
            Profiler.BeginSample("UnitUpdate");
            Profiler.BeginSample(unit.GetUnitName());
            unit.UpdateUnit(deltaTime);
            Profiler.EndSample();
            Profiler.EndSample();
        }

        Profiler.BeginSample("ProjectilesUpdate");
        foreach (var projectile in usedProjectiles.ToList()) {
            projectile.UpdateProjectile(deltaTime);
        }

        Profiler.EndSample();
        Profiler.BeginSample("MissilesUpdate");
        foreach (var missile in usedMissiles.ToList()) {
            missile.UpdateMissile(deltaTime);
        }

        Profiler.EndSample();
        Profiler.BeginSample("DestroyedUnitsUpdate");
        foreach (var destroyedUnit in destroyedUnits.ToList()) {
            destroyedUnit.UpdateDestroyedUnit(deltaTime);
        }

        Profiler.EndSample();
        Profiler.BeginSample("StarsUpdate");
        foreach (var star in stars.ToList()) {
            star.UpdateStar(deltaTime);
        }

        Profiler.EndSample();
        Profiler.BeginSample("PlanetsUpdate");
        foreach (var planet in planets) {
            planet.UpdatePlanet(deltaTime);
        }

        Profiler.EndSample();
        eventManager.UpdateEvents(deltaTime);
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
        // instantHit = time > 10;
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

    #endregion
}
