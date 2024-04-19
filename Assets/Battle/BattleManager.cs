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
    public HashSet<ShipBlueprint> shipBlueprints;
    public HashSet<StationBlueprint> stationBlueprints;

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
    bool simulationEnded;
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
        if (quickStart == true) {
            Debug.Log("Setting up test scene");
            List<FactionData> tempFactions = new List<FactionData> {
                new FactionData("Faction1", "F1", Random.Range(10000000, 100000000), 0, 50, 1),
                new FactionData("Faction2", "F2", Random.Range(10000000, 100000000), 0, 50, 1)
            };
            SetupBattle(0, 0, 1, 0.1f, 1.1f, tempFactions);
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
    public void SetupBattle(int starCount, int asteroidFieldCount, float asteroidCountModifier, float systemSizeModifier, float researchModifier, List<FactionData> factionDatas) {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
        this.systemSizeModifier = systemSizeModifier;
        this.researchModifier = researchModifier;
        factions = new HashSet<Faction>(10);
        units = new HashSet<Unit>(200);
        ships = new HashSet<Ship>(150);
        stations = new HashSet<Station>(50);
        stars = new HashSet<Star>();
        planets = new HashSet<Planet>();
        asteroidFields = new HashSet<AsteroidField>(asteroidFieldCount);
        projectiles = new HashSet<Projectile>(500);
        destroyedUnits = new HashSet<Unit>(200);

        for (int i = 0; i < 100; i++) {
            PreSpawnNewProjectile();
        }
        for (int i = 0; i < 20; i++) {
            PrespawnNewMissile();
        }
        for (int i = 0; i < starCount; i++) {
            CreateNewStar("Star" + (i + 1));
        }
        for (int i = 0; i < asteroidFieldCount; i++) {
            CreateNewAsteroidField(Vector2.zero, (int)Random.Range(6 * asteroidCountModifier, 14 * asteroidCountModifier));
        }
        transform.parent.Find("Player").GetComponent<LocalPlayer>().SetUpPlayer();

        for (int i = 0; i < factionDatas.Count; i++) {
            CreateNewFaction(factionDatas[i], new PositionGiver(Vector2.zero, 0, 1000000, 100, 1000, 10), 100);
        }

        foreach (var faction in factions) {
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
        simulationEnded = false;
        if (CheckVictory())
            simulationEnded = true;
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
        }
        this.campaignController = campaignControler;
        systemSizeModifier = campaignControler.systemSizeModifier;
        researchModifier = campaignControler.researchModifier;
        factions = new HashSet<Faction>(0);
        units = new HashSet<Unit>(100);
        ships = new HashSet<Ship>(50);
        stations = new HashSet<Station>(50);
        stars = new HashSet<Star>();
        planets = new HashSet<Planet>();
        asteroidFields = new HashSet<AsteroidField>(20);
        projectiles = new HashSet<Projectile>(500);
        destroyedUnits = new HashSet<Unit>(50);
        startOfSimulation = Time.time;
        simulationEnded = false;
        transform.parent.Find("Player").GetComponent<LocalPlayer>().SetUpPlayer();
        LocalPlayer.Instance.SetupFaction(null);
        campaignControler.SetupBattle(this);
        simulationEnded = true;
        foreach (var faction in factions) {
            faction.UpdateObjectGroup();
        }
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
        newShip.SetupUnit(shipData.shipName, shipData.faction, new PositionGiver(shipData.position), shipData.rotation, timeScale, shipData.shipScriptableObject);
        return newShip;
    }

    public Station CreateNewStation(StationData stationData) {
        return CreateNewStation(stationData, new PositionGiver(stationData.wantedPosition, 0, 1000, 20, 100, 2));
    }

    public Station CreateNewStation(StationData stationData, PositionGiver positionGiver) {
        GameObject stationPrefab = (GameObject)Resources.Load(stationData.stationScriptableObject.prefabPath);
        Station newStation = Instantiate(stationPrefab, stationData.faction.GetStationTransform()).GetComponent<Station>();
        newStation.SetupUnit(stationData.stationName, stationData.faction, positionGiver, stationData.rotation, stationData.built, timeScale, stationData.stationScriptableObject);
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
        newStar.SetupStar(name, new PositionGiver(Vector2.zero, 1000, 100000, 100, 5000, 4));
        stars.Add(newStar);
    }

    public Planet CreateNewPlanet(string name, Faction faction, PositionGiver positionGiver, long population, float landFactor, double populationGrowthRate = 0.01) {
        GameObject planetPrefab = (GameObject)Resources.Load("Prefabs/Planet");
        Planet newPlanet = Instantiate(planetPrefab, GetPlanetsTransform()).GetComponent<Planet>();
        newPlanet.SetupPlanet(name, faction, positionGiver, population, populationGrowthRate, Random.Range(0, 360), landFactor);
        planets.Add(newPlanet);
        return newPlanet;
    }

    public void CreateNewAsteroidField(Vector2 center, int count, float resourceModifier = 1) {
        CreateNewAsteroidField(new PositionGiver(center, 0, 100000, 500, 1000, 2), count, resourceModifier);
    }

    public void CreateNewAsteroidField(PositionGiver positionGiver, int count, float resourceModifier = 1) {
        GameObject asteroidFieldPrefab = (GameObject)Resources.Load("Prefabs/AsteroidField");
        AsteroidField newAsteroidField = Instantiate(asteroidFieldPrefab, Vector2.zero, Quaternion.identity, GetAsteroidFieldTransform()).GetComponent<AsteroidField>();
        for (int i = 0; i < count; i++) {
            GameObject asteroidPrefab = (GameObject)Resources.Load("Prefabs/Asteroids/Asteroid" + ((int)Random.Range(1, 4)).ToString());
            Asteroid newAsteroid = Instantiate(asteroidPrefab, newAsteroidField.transform).GetComponent<Asteroid>();
            float size = Random.Range(8f, 20f);
            newAsteroid.SetupAsteroid(newAsteroidField, new PositionGiver(Vector2.zero, 0, 1000, 50, Random.Range(0, 100), 4), new AsteroidData(newAsteroidField.GetPosition(), Random.Range(0, 360), size, (int)(Random.Range(100, 1000) * size * resourceModifier), CargoBay.CargoTypes.Metal));
            newAsteroidField.asteroids.Add(newAsteroid);
        }
        asteroidFields.Add(newAsteroidField);
        //newAsteroidField.SetupAsteroidField(new PositionGiver(center, 0, 100000, 500, 1000, 2));
        newAsteroidField.SetupAsteroidField(positionGiver);
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
        newProjectile.PrespawnProjectile(projectiles.Count, timeScale);
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
        newMissile.PrespawnMissile(missiles.Count, timeScale);
        missiles.Add(newMissile);
    }
    #endregion

    /// <summary>
    /// Updates the faction AI, units, projectiles etc owned by this faction based on the time elapsed.
    /// 
    /// Also has profiling for most method calls.
    /// </summary>
    public virtual void FixedUpdate() {
        if (campaignController != null) {
            campaignController.UpdateController();
        }
        float deltaTime = Time.fixedDeltaTime * timeScale;
        simulationTime += deltaTime;
        foreach (var faction in factions) {
            Profiler.BeginSample("EarlyFactionUpdate");
            faction.EarlyUpdateFaction();
            Profiler.EndSample();
        }
        foreach (var faction in factions) {
            Profiler.BeginSample("FactionUpdate");
            faction.UpdateFaction(deltaTime);
            Profiler.EndSample();
        }
        foreach (var faction in factions) {
            faction.UpdateFleets(deltaTime);
        }
        foreach (var unit in units) {
            Profiler.BeginSample("UnitUpdate");
            Profiler.BeginSample(unit.GetUnitName());
            unit.UpdateUnit(deltaTime);
            Profiler.EndSample();
            Profiler.EndSample();
        }
        Profiler.BeginSample("ProjectilesUpdate");
        foreach (var projectile in projectiles) {
            projectile.UpdateProjectile(deltaTime);

        }
        Profiler.EndSample();
        Profiler.BeginSample("MissilesUpdate");
        foreach (var missile in usedMissiles) {
            missile.UpdateMissile(deltaTime);
        }
        Profiler.EndSample();
        Profiler.BeginSample("DestroyedUnitsUpdate");
        foreach (var destroyedUnit in destroyedUnits) {
            destroyedUnit.UpdateDestroyedUnit(deltaTime);
        }
        Profiler.EndSample();
        Profiler.BeginSample("StarsUpdate");
        foreach (var star in stars) {
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
            LocalPlayer.Instance.GetPlayerUI().FactionWon(factionWon.name, GetRealTime(), GetSimulationTime());
            simulationEnded = true;
            LocalPlayer.Instance.GetLocalPlayerInput().StopSimulationButtonPressed();
        }
    }

    public void LateUpdate() {
        LocalPlayer.Instance.GetLocalPlayerInput().UpdatePlayer();
        LocalPlayer.Instance.UpdatePlayer();
    }

    #region HelperMethods
    public Faction CheckVictory() {
        if (simulationEnded)
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

    public double GetRealTime() {
        return Time.unscaledTime - startOfSimulation;
    }

    public ShipBlueprint GetShipBlueprint(ShipClass shipClass) {
        shipBlueprints.ToList().First(ship => ship.shipScriptableObject.shipClass == shipClass);
        return null;
    }

    public ShipBlueprint GetShipBlueprint(ShipType shipType) {
        shipBlueprints.ToList().First(ship => ship.shipScriptableObject.shipType == shipType);
        return null;
    }

    public StationBlueprint GetStationBlueprint(StationType stationType) {
        stationBlueprints.ToList().First(station => station.stationScriptableObject.stationType == stationType);
        return null;
    }

    public Transform GetFactionsTransform() {
        return transform.GetChild(0);
    }

    public Transform GetAsteroidFieldTransform() {
        return transform.GetChild(1);
    }

    public Transform GetStarTransform() {
        return transform.GetChild(2);
    }

    public Transform GetPlanetsTransform() {
        return transform.GetChild(3);
    }

    public Transform GetProjectileTransform() {
        return transform.GetChild(4);
    }

    public Transform GetMissileTransform() {
        return transform.GetChild(5);
    }

    public static GameObject GetSizeIndicatorPrefab() {
        return Resources.Load<GameObject>("Prefabs/SizeIndicator");
    }
    #endregion
}