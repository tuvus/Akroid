using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using static Asteroid;
using static Faction;
using static Ship;
using static Station;

public class BattleManager : MonoBehaviour {
    public static BattleManager Instance { get; protected set; }
    CampaingController campaignController;

    public float researchModifier { get; private set; }
    public float systemSizeModifier { get; private set; }
    public List<ShipBlueprint> shipBlueprints;

    public List<Faction> factions;
    public List<Unit> units;
    public List<Ship> ships;
    public List<Station> stations;
    public List<Station> stationBlueprints;
    public List<Projectile> projectiles;
    public List<Missile> missiles;
    public List<Star> stars;
    public List<Planet> planets;
    public List<AsteroidField> asteroidFields;

    public List<Unit> destroyedUnits;
    public List<int> usedProjectiles;
    public List<int> unusedProjectiles;
    public List<int> usedMissiles;
    public List<int> unusedMissiles;
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
            Debug.Log("Seting up test scene");
            List<FactionData> tempFactions = new List<FactionData> {
                new FactionData("Faction1", Random.Range(10000000, 100000000), 0, 14, 1),
                new FactionData("Faction2", Random.Range(10000000, 100000000), 0, 14, 1)
            };
            SetupBattle(1, 0, 1, 0.1f, 1.1f, tempFactions);
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
        factions = new List<Faction>(10);
        units = new List<Unit>(200);
        ships = new List<Ship>(150);
        stations = new List<Station>(50);
        stars = new List<Star>();
        planets = new List<Planet>();
        asteroidFields = new List<AsteroidField>(asteroidFieldCount);
        projectiles = new List<Projectile>(500);
        destroyedUnits = new List<Unit>(200);

        for (int i = 0; i < 100; i++) {
            PreSpawnNewProjectile();
        }
        for (int i = 0; i < 20; i++) {
            PrespawnNewMissile();
        }
        for (int i = 0; i < starCount; i++) {
            CreateNewStar();
        }
        for (int i = 0; i < asteroidFieldCount; i++) {
            CreateNewAsteroidField(Vector2.zero, (int)Random.Range(6 * asteroidCountModifier, 14 * asteroidCountModifier));
        }
        transform.parent.Find("Player").GetComponent<LocalPlayer>().SetUpPlayer();

        for (int i = 0; i < factionDatas.Count; i++) {
            CreateNewFaction(factionDatas[i], new PositionGiver(Vector2.zero, 0, 1000000, 250, 5000, 10), 100);
        }

        for (int i = 0; i < factions.Count; i++) {
            for (int f = 0; f < factions.Count; f++) {
                if (f == i)
                    continue;
                factions[i].AddEnemyFaction(factions[f]);
            }
        }

        if (GetAllFactions().Count > 0)
            LocalPlayer.Instance.SetupFaction(factions[0]);
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
        factions = new List<Faction>(0);
        units = new List<Unit>(100);
        ships = new List<Ship>(50);
        stations = new List<Station>(50);
        stars = new List<Star>();
        planets = new List<Planet>();
        asteroidFields = new List<AsteroidField>(20);
        projectiles = new List<Projectile>(500);
        destroyedUnits = new List<Unit>(50);
        startOfSimulation = Time.time;
        simulationEnded = false;
        transform.parent.Find("Player").GetComponent<LocalPlayer>().SetUpPlayer();
        LocalPlayer.Instance.SetupFaction(null);
        campaignControler.SetupBattle();
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
        newFaction.SetUpFaction(factions.Count - 1, factionData, positionGiver, startingResearchCost);
        return newFaction;
    }

    public Ship CreateNewShip(ShipData shipData) {
        GameObject shipPrefab = (GameObject)Resources.Load("Prefabs/ShipPrefabs/" + shipData.shipClass.ToString());
        Ship newShip = Instantiate(shipPrefab, factions[shipData.faction].GetShipTransform()).GetComponent<Ship>();
        units.Add(newShip);
        ships.Add(newShip);
        newShip.SetupUnit(shipData.shipName, factions[shipData.faction], new PositionGiver(shipData.position), shipData.rotation, timeScale);
        return newShip;
    }

    public Station CreateNewStation(StationData stationData) {
        return CreateNewStation(stationData, new PositionGiver(stationData.wantedPosition, 0, 1000, 200, 100, 2));
    }

    public Station CreateNewStation(StationData stationData, PositionGiver positionGiver) {
        GameObject stationPrefab = (GameObject)Resources.Load(stationData.path);
        Station newStation = Instantiate(stationPrefab, factions[stationData.faction].GetStationTransform()).GetComponent<Station>();
        newStation.SetupUnit(stationData.stationName, factions[stationData.faction], positionGiver, stationData.rotation, stationData.built, timeScale);
        if (stationData.built) {
            units.Add(newStation);
            stations.Add(newStation);
        } else {
            stationBlueprints.Add(newStation);
        }
        return newStation;
    }

    public void CreateNewStar() {
        GameObject starPrefab = (GameObject)Resources.Load("Prefabs/Star");
        Star newStar = Instantiate(starPrefab, GetStarTransform()).GetComponent<Star>();
        newStar.SetupStar(new PositionGiver(Vector2.zero, 1000, 100000, 100, 2000, 4));
        stars.Add(newStar);
    }

    public Planet CreateNewPlanet(string name, Faction faction, PositionGiver positionGiver, long population, double populationGrowthRate = 0.01) {
        GameObject planetPrefab = (GameObject)Resources.Load("Prefabs/Planet");
        Planet newPlanet = Instantiate(planetPrefab, GetPlanetsTransform()).GetComponent<Planet>();
        newPlanet.SetupPlanet(name, faction, positionGiver, population, populationGrowthRate, Random.Range(0, 360));
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
            newAsteroid.SetupAsteroid(newAsteroidField, new PositionGiver(Vector2.zero, 0, 1000, 50, Random.Range(0, 20), 4), new AsteroidData(newAsteroidField.GetPosition(), Random.Range(0, 360), size, (int)(Random.Range(100, 1000) * size * resourceModifier), CargoBay.CargoTypes.Metal));
            newAsteroidField.asteroids.Add(newAsteroid);
        }
        asteroidFields.Add(newAsteroidField);
        //newAsteroidField.SetupAsteroidField(new PositionGiver(center, 0, 100000, 500, 1000, 2));
        newAsteroidField.SetupAsteroidField(positionGiver);
    }
    #endregion

    #region ObjectLists
    public void BuildStationBlueprint(Station station) {
        stationBlueprints.Remove(station);
        units.Add(station);
        stations.Add(station);
    }

    public void DestroyShip(Ship ship) {
        units.Remove(ship);
        ships.Remove(ship);
        if (ship.faction != null)
            ship.faction.RemoveShip(ship);
        destroyedUnits.Add(ship);
    }

    public void DestroyStation(Station station) {
        if (station.IsBuilt()) {
            units.Remove(station);
            stations.Remove(station);
            if (station.faction != null)
                station.faction.RemoveStation(station);
        } else {
            stationBlueprints.Remove(station);
            if (station.faction != null)
                station.faction.RemoveStationBlueprint(station);

        }
        destroyedUnits.Add(station);
    }

    public void RemoveDestroyedUnit(Unit unit) {
        destroyedUnits.Remove(unit);
    }

    public void AddProjectile(Projectile projectile) {
        usedProjectiles.Add(projectile.projectileIndex);
        unusedProjectiles.Remove(projectile.projectileIndex);
    }

    public void RemoveProjectile(Projectile projectile) {
        usedProjectiles.Remove(projectile.projectileIndex);
        unusedProjectiles.Add(projectile.projectileIndex);
    }

    public Projectile GetNewProjectile() {
        if (unusedProjectiles.Count == 0) {
            PreSpawnNewProjectile();
        }
        return projectiles[unusedProjectiles[0]];
    }

    public void PreSpawnNewProjectile() {
        GameObject projectilePrefab = (GameObject)Resources.Load("Prefabs/Projectile");
        Projectile newProjectile = Instantiate(projectilePrefab, Vector2.zero, Quaternion.identity, GetProjectileTransform()).GetComponent<Projectile>();
        newProjectile.PrespawnProjectile(projectiles.Count, timeScale);
        projectiles.Add(newProjectile);
    }

    public void AddMissile(Missile missile) {
        usedMissiles.Add(missile.missileIndex);
        unusedMissiles.Remove(missile.missileIndex);
    }

    public void RemoveMissile(Missile missile) {
        usedMissiles.Remove(missile.missileIndex);
        unusedMissiles.Add(missile.missileIndex);
    }

    public Missile GetNewMissile() {
        if (unusedMissiles.Count == 0) {
            PrespawnNewMissile();
        }
        return missiles[unusedMissiles[0]];
    }

    public void PrespawnNewMissile() {
        GameObject missilePrefab = (GameObject)Resources.Load("Prefabs/HermesMissile");
        Missile newMissile = Instantiate(missilePrefab, Vector2.zero, Quaternion.identity, GetMissileTransform()).GetComponent<Missile>();
        newMissile.PrespawnMissile(missiles.Count, timeScale);
        missiles.Add(newMissile);
    }
    #endregion

    public virtual void FixedUpdate() {
        if (campaignController != null) {
            campaignController.UpdateControler();
        }
        float deltaTime = Time.fixedDeltaTime * timeScale;
        simulationTime += deltaTime;
        for (int i = 0; i < factions.Count; i++) {
            Profiler.BeginSample("EarlyFactionUpdate");
            factions[i].EarlyUpdateFaction();
            Profiler.EndSample();
        }
        for (int i = 0; i < factions.Count; i++) {
            Profiler.BeginSample("FactionUpdate");
            factions[i].UpdateFaction(deltaTime);
            Profiler.EndSample();
        }
        for (int i = 0; i < factions.Count; i++) {
            factions[i].UpdateFleets(deltaTime);
        }
        for (int i = 0; i < units.Count; i++) {
            Profiler.BeginSample("UnitUpdate");
            Profiler.BeginSample(units[i].GetUnitName());
            units[i].UpdateUnit(deltaTime);
            Profiler.EndSample();
            Profiler.EndSample();
        }
        Profiler.BeginSample("ProjectilesUpdate");
        for (int i = 0; i < usedProjectiles.Count; i++) {
            projectiles[usedProjectiles[i]].UpdateProjectile(deltaTime);
        }
        Profiler.EndSample();
        Profiler.BeginSample("MissilesUpdate");
        for (int i = 0; i < usedMissiles.Count; i++) {
            missiles[usedMissiles[i]].UpdateMissile(deltaTime);
        }
        Profiler.EndSample();
        Profiler.BeginSample("DestroyedUnitsUpdate");
        for (int i = 0; i < destroyedUnits.Count; i++) {
            destroyedUnits[i].UpdateDestroyedUnit(deltaTime);
        }
        Profiler.EndSample();
        Profiler.BeginSample("StarsUpdate");
        for (int i = 0; i < stars.Count; i++) {
            stars[i].UpdateStar(deltaTime);
        }
        Profiler.EndSample();
        Profiler.BeginSample("PlanetsUpdate");
        for (int i = 0; i < planets.Count; i++) {
            planets[i].UpdatePlanet(deltaTime);
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
        for (int i = 0; i < factions.Count; i++) {
            if (factions[i].units.Count > 0 && !factions[i].HasEnemy()) {
                return factions[i];
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
        for (int i = 0; i < units.Count; i++) {
            units[i].SetParticleSpeed(time);
        }
        for (int i = 0; i < projectiles.Count; i++) {
            projectiles[i].SetParticleSpeed(time);
        }
        for (int i = 0; i < missiles.Count; i++) {
            missiles[i].SetParticleSpeed(time);
        }
    }

    /// <summary>
    /// Determines whether or not the effects will be shown or not.
    /// </summary>
    /// <param name="shown"></param>
    public void ShowEffects(bool shown) {
        ShowParticles(shown && LocalPlayer.Instance.GetPlayerUI().particles);
        for (int i = 0; i < units.Count; i++) {
            units[i].ShowEffects(shown);
        }
        for (int i = 0; i < projectiles.Count; i++) {
            projectiles[i].ShowEffects(shown);
        }
        for (int i = 0; i < missiles.Count; i++) {
            missiles[i].ShowEffects(shown);
        }
        for (int i = 0; i < destroyedUnits.Count; i++) {
            destroyedUnits[i].ShowEffects(shown);
        }
    }

    /// <summary>
    /// Determines whether or not the particles in the game are rendered or not.
    /// Will not be called with the same shown value twice in a row.
    /// </summary>
    /// <param name="shown"></param>
    public void ShowParticles(bool shown) {
        for (int i = 0; i < units.Count; i++) {
            units[i].ShowParticles(shown);
        }
        for (int i = 0; i < projectiles.Count; i++) {
            projectiles[i].ShowParticles(shown);
        }
        for (int i = 0; i < missiles.Count; i++) {
            missiles[i].ShowParticles(shown);
        }
        for (int i = 0; i < destroyedUnits.Count; i++) {
            destroyedUnits[i].ShowParticles(shown);
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
        for (int i = 0; i < shipBlueprints.Count; i++) {
            if (shipBlueprints[i].shipClass == shipClass) {
                return shipBlueprints[i];
            }
        }
        return null;
    }

    public List<Ship> GetAllShips() {
        return ships;
    }

    public List<Station> GetAllStations() {
        return stations;
    }

    public List<Faction> GetAllFactions() {
        return factions;
    }

    public List<Unit> GetAllUnits() {
        return units;
    }

    public List<Star> GetAllStars() {
        return stars;
    }

    public List<AsteroidField> GetAllAsteroidFields() {
        return asteroidFields;
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