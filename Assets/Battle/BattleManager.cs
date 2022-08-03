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

    public float researchModifier { get; private set; }

    public List<Faction> factions;
    public List<Unit> units;
    public List<Ship> ships;
    public List<Station> stations;
    public List<Projectile> projectiles;
    public List<Missile> missiles;
    public List<Star> stars;
    public List<AsteroidField> asteroidFields;

    public List<Unit> destroyedUnits;
    public List<int> usedProjectiles;
    public List<int> unusedProjectiles;
    public List<int> usedMissiles;
    public List<int> unusedMissiles;
    public float timeScale;
    public static bool quickStart = true;

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

    protected virtual void Start() {
        if (quickStart == true) {
            Debug.Log("Seting up test scene");
            List<FactionData> tempFactions = new List<FactionData>();
            tempFactions.Add(new FactionData("Faction1", Random.Range(10000000, 100000000), 0, 4, 5));
            tempFactions.Add(new FactionData("Faction2", Random.Range(10000000, 100000000), 0, 4, 5));
            tempFactions.Add(new FactionData("Faction3", Random.Range(10000000, 100000000), 0, 4, 5));
            tempFactions.Add(new FactionData("Faction4", Random.Range(10000000, 100000000), 0, 4, 5));

            SetupBattle(3, 40, 1, 1.2f, tempFactions);
        }
    }

    public void SetupBattle(int starCount, int asteroidFieldCount, float asteroidCountModifier, float researchModifier, List<FactionData> factionDatas) {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
        this.researchModifier = researchModifier;
        factions = new List<Faction>(10);
        units = new List<Unit>(200);
        ships = new List<Ship>(150);
        stations = new List<Station>(50);
        stars = new List<Star>();
        asteroidFields = new List<AsteroidField>(100);
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
            CreateNewAteroidField(Vector2.zero, (int)Random.Range(6 * asteroidCountModifier, 14 * asteroidCountModifier));
        }
        transform.parent.Find("Player").GetComponent<LocalPlayer>().SetUpPlayer();

        for (int i = 0; i < factionDatas.Count; i++) {
            CreateNewFaction(factionDatas[i], 100);
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
    }


    /// <summary>
    /// Finds a position around a center location that is minDistanceFromStationOrAsteroid and less than maxDistance from the center point.
    /// If the center location is an asteroid or station isCenterObject should be true.
    /// </summary>
    /// <param name="centerLocation">The position to be close to</param>
    /// <param name="minDistanceFromStationOrAsteroid">The minimum distance to stations and asteroids</param>
    /// <param name="maxDistance">The maximum distance from the center location</param>
    /// <param name="isCenterObject">Is the centerLocation on a station or asteroid?</param>
    /// <param name="tyCount">The ammount of times to try</param>
    /// <returns></returns>
    public Vector2? FindFreeLocation(PositionGiver positionGiver, IPositionConfirmer positionConfirmer, float minRange, float maxRange) {
        for (int i = 0; i < positionGiver.numberOfTries; i++) {
            float distance = Random.Range(minRange, maxRange);
            Vector2 tryPos = positionGiver.position + Calculator.GetPositionOutOfAngleAndDistance(Random.Range(0f, 360f), distance);
            if (positionConfirmer.ConfirmPosition(tryPos, positionGiver.distanceFromObject)) {
                return tryPos;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds a position around a center location that is minDistanceFromStationOrAsteroid and less than maxDistance from the center point.
    /// If the center location is an asteroid or station isCenterObject should be true.
    /// If the point cannot be found then it increaces the search distance.
    /// </summary>
    /// <param name="centerLocation">The position to be close to</param>
    /// <param name="minDistanceFromStationOrAsteroid">The minimum distance to stations and asteroids</param>
    /// <param name="startingDistance">The startin distance to check</param>
    /// <param name="maxDistance">The maximum ammount that the distance can be incramented</param>
    /// <param name="incrementValue">The ammount to incrament by</param>
    /// <param name="isCenterObject">Is the centerLocation on a station or asteroid?</param>
    /// <param name="tyCount">The ammount of times to try for each incrament</param>
    /// <returns></returns>
    public Vector2? FindFreeLocationIncrament(PositionGiver positionGiver, IPositionConfirmer positionConfirmer) {
        float distance = positionGiver.minDistance;
        while (true) {
            Vector2? targetPosition = FindFreeLocation(positionGiver, positionConfirmer, distance, distance + positionGiver.incrementDistance);
            if (targetPosition.HasValue) {
                return targetPosition.Value;
            }
            distance += positionGiver.incrementDistance;
            if (distance > positionGiver.maxDistance - positionGiver.incrementDistance) {
                return null;
            }
        }
    }

    public Faction CreateNewFaction(FactionData factionData, int startingResearchCost) {
        Faction newFaction = Instantiate(Resources.Load<GameObject>("Prefabs/Faction"), GetFactionsTransform()).GetComponent<Faction>();
        factions.Add(newFaction);
        newFaction.SetUpFaction(factions.Count - 1, factionData, startingResearchCost);
        return newFaction;
    }

    public Ship CreateNewShip(ShipData shipData) {
        GameObject shipPrefab = (GameObject)Resources.Load("Prefabs/ShipPrefabs/" + shipData.shipClass.ToString());
        Ship newShip = Instantiate(shipPrefab, factions[shipData.faction].GetShipTransform()).GetComponent<Ship>();
        units.Add(newShip);
        ships.Add(newShip);
        newShip.SetupUnit(shipData.shipName, factions[shipData.faction], new PositionGiver(shipData.position), shipData.rotation);
        return newShip;
    }

    public Station CreateNewStation(StationData stationData) {
        GameObject stationPrefab = (GameObject)Resources.Load("Prefabs/StationPrefabs/" + stationData.stationType.ToString());
        Station newStation = Instantiate(stationPrefab, factions[stationData.faction].GetStationTransform()).GetComponent<Station>();
        newStation.SetupUnit(stationData.stationName, factions[stationData.faction], new PositionGiver(stationData.wantedPosition, 0, 1000, 200, 100, 2), stationData.rotation, stationData.stationType, stationData.built);
        units.Add(newStation);
        stations.Add(newStation);
        return newStation;
    }

    public void CreateNewStar() {
        GameObject starPrefab = (GameObject)Resources.Load("Prefabs/Star");
        Star newStar = Instantiate(starPrefab, GetStarTransform()).GetComponent<Star>();
        newStar.SetupStar(new PositionGiver(Vector2.zero, 1000, 100000, 100, 2000, 4));
        stars.Add(newStar);
    }

    public void CreateNewAteroidField(Vector2 center, int count) {
        GameObject asteroidFieldPrefab = (GameObject)Resources.Load("Prefabs/AsteroidField");
        AsteroidField newAsteroidField = Instantiate(asteroidFieldPrefab, center, Quaternion.identity, GetAsteroidFieldTransform()).GetComponent<AsteroidField>();
        for (int i = 0; i < count; i++) {
            GameObject asteroidPrefab = (GameObject)Resources.Load("Prefabs/Asteroids/Asteroid" + ((int)Random.Range(1, 4)).ToString());
            Asteroid newAsteroid = Instantiate(asteroidPrefab, newAsteroidField.transform).GetComponent<Asteroid>();
            float size = Random.Range(2f, 20f);
            newAsteroid.SetupAsteroid(newAsteroidField, new PositionGiver(newAsteroidField.GetPosition(), 0, 1000, 50, Random.Range(0, 20), 4), new AsteroidData(newAsteroidField.GetPosition(), Random.Range(0, 360), size, (int)(Random.Range(100, 1000) * size), CargoBay.CargoTypes.Metal));
            newAsteroidField.asteroids.Add(newAsteroid);
        }
        asteroidFields.Add(newAsteroidField);
        newAsteroidField.SetupAsteroidField(new PositionGiver(newAsteroidField.GetPosition(), 0, 100000, 500, 1000, 2));
    }

    public void DestroyShip(Ship ship) {
        units.Remove(ship);
        ships.Remove(ship);
        if (ship.faction != null)
            ship.faction.RemoveShip(ship);
        destroyedUnits.Add(ship);
    }

    public void DestroyStation(Station station) {
        units.Remove(station);
        stations.Remove(station);
        if (station.faction != null)
            station.faction.RemoveStation(station);
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
        newProjectile.PrespawnProjectile(projectiles.Count);
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
        GameObject missilePrefabe = (GameObject)Resources.Load("Prefabs/HermesMissile");
        Missile newMissile = Instantiate(missilePrefabe, Vector2.zero, Quaternion.identity, GetMissileTransform()).GetComponent<Missile>();
        newMissile.PrespawnMissile(missiles.Count);
        missiles.Add(newMissile);
    }

    public virtual void FixedUpdate() {
        for (int i = 0; i < factions.Count; i++) {
            Profiler.BeginSample("FactionsUpdate" + i);
            factions[i].UpdateFaction();
            Profiler.EndSample();
        }
        for (int i = 0; i < units.Count; i++) {
            Profiler.BeginSample("UnitsUpdate" + i);
            units[i].UpdateUnit();
            Profiler.EndSample();
        }
        Profiler.BeginSample("ProjectilesUpdate");
        for (int i = 0; i < usedProjectiles.Count; i++) {
            projectiles[usedProjectiles[i]].UpdateProjectile();
        }
        Profiler.EndSample();
        Profiler.BeginSample("MissilesUpdate");
        for (int i = 0; i < usedMissiles.Count; i++) {
            missiles[usedMissiles[i]].UpdateMissile();
        }
        Profiler.EndSample();
        Profiler.BeginSample("DestroyedUnitsUpdate");
        for (int i = 0; i < destroyedUnits.Count; i++) {
            destroyedUnits[i].UpdateDestroyedUnit();
        }
        Profiler.EndSample();
    }

    public void LateUpdate() {
        LocalPlayer.Instance.GetLocalPlayerInput().UpdatePlayer();
        LocalPlayer.Instance.UpdatePlayer();
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

    public Transform GetProjectileTransform() {
        return transform.GetChild(3);
    }

    public Transform GetMissileTransform() {
        return transform.GetChild(4);
    }
}