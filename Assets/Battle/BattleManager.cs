using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using static Asteroid;
using static Faction;
using static Ship;
using static Station;

public class BattleManager : MonoBehaviour {
    public static BattleManager Instance { get; protected set; }
    CampaingController campaignController;

    [field: SerializeField] public float researchModifier { get; private set; }
    [field: SerializeField] public float systemSizeModifier { get; private set; }
    public List<ShipBlueprint> shipBlueprints;
    public List<StationBlueprint> stationBlueprints;

    public HashSet<Faction> factions { get; private set; }
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

        public PositionGiver(Vector2 position, float minDistance, float maxDistance, float incrementDistance, float distanceFromObject, int numberOfTries) {
            this.position = position;
            this.isExactPosition = false;
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
            this.incrementDistance = incrementDistance;
            this.distanceFromObject = distanceFromObject;
            this.numberOfTries = numberOfTries;
        }
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
            SetupBattle(0, 0, 1, 0, 0.1f, 1.1f, tempFactions);
        }
    }

    /// <summary>
    /// Sets up the battle with manual values
    /// </summary>
    /// <param name="starCount"></param>
    /// <param name="asteroidFieldCount"></param>
    /// <param name="asteroidCountModifier"></param>
    /// <param name="researchModifier"></param>
    /// <param name="factionDatas"></param>
    public void SetupBattle(int starCount, int asteroidFieldCount, float asteroidCountModifier, int gasCloudCount, float systemSizeModifier, float researchModifier, List<FactionData> factionDatas) {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
        this.systemSizeModifier = systemSizeModifier;
        this.researchModifier = researchModifier;
        InitializeBattle();
        for (int i = 0; i < starCount; i++) {
            CreateNewStar("Star" + (i + 1));
        }
        for (int i = 0; i < asteroidFieldCount; i++) {
            CreateNewAsteroidField(Vector2.zero, (int)Random.Range(6 * asteroidCountModifier, 14 * asteroidCountModifier));
        }

        for (int i = 0; i < factionDatas.Count; i++) {
            CreateNewFaction(factionDatas[i], new PositionGiver(Vector2.zero, 0, 1000000, 100, 1000, 10), 100);
        }

        for (int i = 0; i < gasCloudCount; i++) {
            CreateNewGasCloud(new PositionGiver(Vector2.zero, 1000, 100000, 500, 2000, 3));
        }

        foreach (var faction in factions) {
            faction.GetFleetCommand().LoadCargo(2400, CargoBay.CargoTypes.Gas);
            foreach (var faction2 in factions) {
                if (faction == faction2) continue;
                faction.AddEnemyFaction(faction2);
            }
        }

        if (factions.Count > 0)
            LocalPlayer.Instance.SetupFaction(factions.First((f) => factionDatas.Any((d) => d.name == f.name)));
        else
            LocalPlayer.Instance.SetupFaction(null);
        LocalPlayer.Instance.GetLocalPlayerInput().CenterCamera();

        startOfSimulation = Time.unscaledTime;
        battleState = BattleState.Running;
        if (CheckVictory())
            battleState = BattleState.Ended;
    }

    /// <summary>
    /// Sets up the battle with a CampaignController, doesn't spawn any asteroids or stars.
    /// Lets the CampaignController set the settings.
    /// </summary>
    /// <param name="campaignControler">the given CampaignController</param>
    public void SetupBattle(CampaingController campaignControler) {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
        this.campaignController = campaignControler;
        systemSizeModifier = campaignControler.systemSizeModifier;
        researchModifier = campaignControler.researchModifier;
        InitializeBattle();
        LocalPlayer.Instance.SetupFaction(null);
        LocalPlayer.Instance.playerUI.playerEventUI.SetWorldSpaceTransform(GetEventVisulationTransform());
        campaignControler.SetupBattle(this);
        foreach (var faction in factions) {
            faction.UpdateObjectGroup();
        }
        startOfSimulation = Time.unscaledTime;
        battleState = BattleState.Running;
    }

    private void InitializeBattle() {
        factions = new HashSet<Faction>(10);
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

        for (int i = 0; i < 100; i++) {
            PreSpawnNewProjectile();
        }
        for (int i = 0; i < 20; i++) {
            PrespawnNewMissile();
        }
        transform.parent.Find("Player").GetComponent<LocalPlayer>().SetUpPlayer();
    }
    #endregion

    #region Spawning
    /// <summary>
    /// Finds a position around a center location that is minDistanceFromStationOrAsteroid and less than maxDistance from the center point.
    /// If the center location is an asteroid or station isCenterObject should be true.
    /// </summary>
    /// <param name="centerLocation">The position to be close to</param>
    /// <param name="minDistanceFromStationOrAsteroid">The minimum distance to stations and asteroids</param>
    /// <param name="maxDistance">The maximum distance from the center location</param>
    /// <param name="isCenterObject">Is the centerLocation on a station or asteroid?</param>
    /// <param name="tyCount">The amount of times to try</param>
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
    /// <param name="centerLocation">The position to be close to</param>
    /// <param name="minDistanceFromStationOrAsteroid">The minimum distance to stations and asteroids</param>
    /// <param name="startingDistance">The starting distance to check</param>
    /// <param name="maxDistance">The maximum amount that the distance can be incremented</param>
    /// <param name="incrementValue">The amount to increment by</param>
    /// <param name="isCenterObject">Is the centerLocation on a station or asteroid?</param>
    /// <param name="tyCount">The amount of times to try for each increment</param>
    /// <returns></returns>
    public Vector2? FindFreeLocationIncrement(PositionGiver positionGiver, IPositionConfirmer positionConfirmer) {
        float distance = positionGiver.minDistance * systemSizeModifier;
        if (positionGiver.numberOfTries == 0) return positionGiver.position;
        while (true) {
            Vector2? targetPosition = FindFreeLocation(positionGiver, positionConfirmer, distance, distance + positionGiver.incrementDistance * systemSizeModifier);
            if (targetPosition.HasValue) {
                return targetPosition.Value;
            }
            distance += positionGiver.incrementDistance * systemSizeModifier;
            if (distance > (positionGiver.maxDistance - positionGiver.incrementDistance) * systemSizeModifier) {
                return null;
            }
        }
    }

    public Faction CreateNewFaction(FactionData factionData, PositionGiver positionGiver, int startingResearchCost) {
        Faction newFaction = Instantiate(Resources.Load<GameObject>("Prefabs/Faction"), GetFactionsTransform()).GetComponent<Faction>();
        factions.Add(newFaction);
        newFaction.SetUpFaction(this, factionData, positionGiver, startingResearchCost);
        return newFaction;
    }

    public Ship CreateNewShip(ShipData shipData) {
        Ship shipPrefab = Resources.Load<Ship>(shipData.shipScriptableObject.prefabPath);
        Ship newShip = Instantiate(shipPrefab.gameObject, shipData.faction.GetShipTransform()).GetComponent<Ship>();
        units.Add(newShip);
        ships.Add(newShip);
        newShip.SetupUnit(this, shipData.shipName, shipData.faction, new PositionGiver(shipData.position), shipData.rotation, timeScale, shipData.shipScriptableObject);
        return newShip;
    }

    public Station CreateNewStation(StationData stationData) {
        return CreateNewStation(stationData, new PositionGiver(stationData.wantedPosition, 0, 1000, 20, 100, 2));
    }

    public Station CreateNewStation(StationData stationData, PositionGiver positionGiver) {
        GameObject stationPrefab = (GameObject)Resources.Load(stationData.stationScriptableObject.prefabPath);
        Station newStation = Instantiate(stationPrefab, stationData.faction.GetStationTransform()).GetComponent<Station>();
        newStation.SetupUnit(this, stationData.stationName, stationData.faction, positionGiver, stationData.rotation, stationData.built, timeScale, stationData.stationScriptableObject);
        if (stationData.built) {
            units.Add(newStation);
            stations.Add(newStation);
        } else {
            stationsInProgress.Add(newStation);
        }
        return newStation;
    }

    public void CreateNewStar(string name) {
        GameObject starPrefab = (GameObject)Resources.Load("Prefabs/Star");
        Star newStar = Instantiate(starPrefab, GetStarTransform()).GetComponent<Star>();
        newStar.SetupStar(this, name, new PositionGiver(Vector2.zero, 1000, 100000, 100, 5000, 4));
        stars.Add(newStar);
    }

    public Planet CreateNewPlanet(PositionGiver positionGiver, Planet.PlanetData planetData) {
        GameObject planetPrefab = (GameObject)Resources.Load("Prefabs/Planet");
        Planet newPlanet = Instantiate(planetPrefab, GetPlanetsTransform()).GetComponent<Planet>();
        newPlanet.SetupPlanet(this, positionGiver, planetData);
        planets.Add(newPlanet);
        return newPlanet;
    }

    public Planet CreateNewMoon(PositionGiver positionGiver, Planet.PlanetData planetData) {
        GameObject planetPrefab = (GameObject)Resources.Load("Prefabs/Moon");
        Planet newPlanet = Instantiate(planetPrefab, GetPlanetsTransform()).GetComponent<Planet>();
        newPlanet.SetupPlanet(this, positionGiver, planetData);
        planets.Add(newPlanet);
        return newPlanet;
    }

    public void CreateNewAsteroidField(Vector2 center, int count, float resourceModifier = 1) {
        CreateNewAsteroidField(new PositionGiver(center, 0, 100000, 500, 1000, 2), count, resourceModifier);
    }

    public void CreateNewAsteroidField(PositionGiver positionGiver, int count, float resourceModifier = 1) {
        GameObject asteroidFieldPrefab = (GameObject)Resources.Load("Prefabs/AsteroidField");
        AsteroidField newAsteroidField = Instantiate(asteroidFieldPrefab, Vector2.zero, Quaternion.identity, GetAsteroidFieldTransform()).GetComponent<AsteroidField>();
        // The Asteroid field must be set up before the asteroids are generated
        newAsteroidField.SetupAsteroidField(this);
        for (int i = 0; i < count; i++) {
            GameObject asteroidPrefab = (GameObject)Resources.Load("Prefabs/Asteroids/Asteroid" + ((int)Random.Range(1, 4)).ToString());
            Asteroid newAsteroid = Instantiate(asteroidPrefab, newAsteroidField.transform).GetComponent<Asteroid>();
            float size = Random.Range(8f, 20f);
            newAsteroid.SetupAsteroid(newAsteroidField, new PositionGiver(Vector2.zero, 0, 1000, 50, Random.Range(0, 100), 4), new AsteroidData("Asteroid", Random.Range(0, 360), size, (long)(Random.Range(400, 600) * size * resourceModifier), CargoBay.CargoTypes.Metal));
            newAsteroidField.battleObjects.Add(newAsteroid);
        }
        // The Asteroid field position must be set after the asteroids have been generated
        newAsteroidField.SetupAstroidFieldPosition(positionGiver);
        asteroidFields.Add(newAsteroidField);
    }

    public void CreateNewGasCloud(PositionGiver positionGiver, float resourceModifier = 1) {
        GameObject gasCloudPrefab = (GameObject)Resources.Load("Prefabs/GasClouds/GasCloud");
        GasCloud newGasCloud = Instantiate(gasCloudPrefab, Vector2.zero, Quaternion.identity, GetGasCloudsTransform()).GetComponent<GasCloud>();
        float size = Random.Range(25, 35);
        newGasCloud.SetupGasCloud(this, positionGiver, new GasCloud.GasCloudData("Gas Cloud", Random.Range(0,360), size, (long)(Random.Range(5000, 17000) * size * resourceModifier), CargoBay.CargoTypes.Gas));
        gasClouds.Add(newGasCloud);
    }
    #endregion

    #region ObjectLists
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
    }

    public void AddProjectile(Projectile projectile) {
        usedProjectiles.Add(projectile);
        unusedProjectiles.Remove(projectile);
    }

    public void RemoveProjectile(Projectile projectile) {
        usedProjectiles.Remove(projectile);
        unusedProjectiles.Add(projectile);
    }

    public Projectile GetNewProjectile() {
        if (unusedProjectiles.Count == 0) {
            PreSpawnNewProjectile();
        }
        return unusedProjectiles.First();
    }

    public void PreSpawnNewProjectile() {
        GameObject projectilePrefab = (GameObject)Resources.Load("Prefabs/Projectile");
        Projectile newProjectile = Instantiate(projectilePrefab, Vector2.zero, Quaternion.identity, GetProjectileTransform()).GetComponent<Projectile>();
        newProjectile.PrespawnProjectile(this, projectiles.Count, timeScale);
        projectiles.Add(newProjectile);
    }

    public void AddMissile(Missile missile) {
        usedMissiles.Add(missile);
        unusedMissiles.Remove(missile);
    }

    public void RemoveMissile(Missile missile) {
        usedMissiles.Remove(missile);
        unusedMissiles.Add(missile);
    }

    public Missile GetNewMissile() {
        if (unusedMissiles.Count == 0) {
            PrespawnNewMissile();
        }
        return unusedMissiles.First();
    }

    public void PrespawnNewMissile() {
        GameObject missilePrefab = (GameObject)Resources.Load("Prefabs/HermesMissile");
        Missile newMissile = Instantiate(missilePrefab, Vector2.zero, Quaternion.identity, GetMissileTransform()).GetComponent<Missile>();
        newMissile.PrespawnMissile(this, missiles.Count, timeScale);
        missiles.Add(newMissile);
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
        Faction factionWon = CheckVictory();
        if (factionWon != null) {
            LocalPlayer.Instance.GetPlayerUI().FactionWon(factionWon, GetRealTime(), GetSimulationTime());
            battleState = BattleState.Ended;
            LocalPlayer.Instance.GetLocalPlayerInput().StopSimulationButtonPressed();
        }
        if (campaignController != null) {
            campaignController.UpdateController(deltaTime);
        }
    }

    public void LateUpdate() {
        LocalPlayer.Instance.GetLocalPlayerInput().UpdatePlayer();
        LocalPlayer.Instance.UpdatePlayer();
    }

    #region HelperMethods
    public Faction CheckVictory() {
        if (battleState != BattleState.Running || campaignController != null)
            return null;
        foreach (var faction in factions) {
            if (faction.units.Count > 0 && !faction.HasEnemy()) {
                return faction;
            }
        }
        return null;
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
        foreach (var unit in units) {
            unit.SetParticleSpeed(time);
        }
        foreach (var projectile in projectiles) {
            projectile.SetParticleSpeed(time);
        }
        foreach (var missile in missiles) {
            missile.SetParticleSpeed(time);
        }
        instantHit = time > 10;
    }

    /// <summary>
    /// Determines whether or not the effects will be shown or not.
    /// </summary>
    /// <param name="shown"></param>
    public void ShowEffects(bool shown) {
        ShowParticles(shown && LocalPlayer.Instance.GetPlayerUI().particles);
        foreach (var unit in units) {
            unit.ShowEffects(shown);
        }
        foreach (var projectile in projectiles) {
            projectile.ShowEffects(shown);
        }
        foreach (var missile in missiles) {
            missile.ShowEffects(shown);
        }
        foreach (var destroyedUnit in destroyedUnits) {
            destroyedUnit.ShowEffects(shown);
        }
    }

    /// <summary>
    /// Determines whether or not the particles in the game are rendered or not.
    /// Will not be called with the same shown value twice in a row.
    /// </summary>
    /// <param name="shown"></param>
    public void ShowParticles(bool shown) {
        foreach (var unit in units) {
            unit.ShowParticles(shown);
        }
        foreach (var projectile in projectiles) {
            projectile.ShowParticles(shown);
        }
        foreach (var missile in missiles) {
            missile.ShowParticles(shown);
        }
        foreach (var destroyedUnits in destroyedUnits) {
            destroyedUnits.ShowParticles(shown);
        }
    }

    /// <summary>
    /// Determines whether all units and icorns should show their unique faction color
    /// or their diplomatic status.
    /// </summary>
    /// <param name="shown"></param>
    public void ShowFactionColoring(bool shown) {
        foreach (var unit in units) {
            unit.ShowFactionColor(shown);
        }
    }

    public bool GetEffectsShown() {
        return PlayerUI.Instance.effects;
    }

    /// <summary>
    /// For particle emitters to figure out if they should emit when begging their emissions.
    /// </summary>
    /// <returns>whether or not the particles should be shown</returns>
    public bool GetParticlesShown() {
        return PlayerUI.Instance.effects && PlayerUI.Instance.particles;
    }

    public bool GetFactionColoringShown() {
        return PlayerUI.Instance.factionColoring;
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

    public void EndBattle() {
        battleState = BattleState.Ended;
    }

    public Transform GetFactionsTransform() {
        return transform.GetChild(0);
    }

    public Transform GetAsteroidFieldTransform() {
        return transform.GetChild(1);
    }

    public Transform GetGasCloudsTransform() {
        return transform.GetChild(2);
    }

    public Transform GetStarTransform() {
        return transform.GetChild(3);
    }

    public Transform GetPlanetsTransform() {
        return transform.GetChild(4);
    }

    public Transform GetProjectileTransform() {
        return transform.GetChild(5);
    }

    public Transform GetMissileTransform() {
        return transform.GetChild(6);
    }

    public Transform GetEventVisulationTransform() {
        return transform.GetChild(7);
    }

    public static GameObject GetSizeIndicatorPrefab() {
        return Resources.Load<GameObject>("Prefabs/SizeIndicator");
    }
    #endregion
}