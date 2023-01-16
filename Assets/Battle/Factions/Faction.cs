using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public class Faction : ObjectGroup<Unit>, IPositionConfirmer {
    [SerializeField] FactionAI factionAI;
    [SerializeField] FactionCommManager commManager;
    [SerializeField] public new string name { get; private set; }
    [SerializeField] public long credits { get; private set; }
    [SerializeField] public long science { get; private set; }
    public long researchCost { get; private set; }
    private double researchCostExtra;
    public int Discoveries { get; private set; }

    public enum ImprovementAreas {
        HullStrength,
        ShieldHealth,
        ShieldRegen,
        ThrustPower,
        ProjectileDamage,
        ProjectileReload,
        ProjectileRange,
        LaserDamage,
        LaserReload,
        LaserRange,
        MissileDamage,
        MissileReload,
        MissileRange,
    }

    public enum ResearchAreas {
        Engineering,
        Electricity,
        Chemicals,
    }

    public float[] improvementModifiers { get; private set; }
    public int[] improvementDiscoveryCount { get; private set; }

    public int factionIndex { get; private set; }

    public List<Unit> units { get; private set; }
    public List<Unit> unitsNotInFleet { get; private set; }
    public List<Ship> ships { get; private set; }
    public List<Fleet> fleets { get; private set; }
    public List<Station> stations { get; private set; }
    public List<Station> stationBlueprints { get; private set; }

    public List<MiningStation> activeMiningStations { get; private set; }

    public List<Faction> enemyFactions { get; private set; }

    public struct FactionData {
        public Type factionAI;
        public string name;
        public Character leader;
        public long credits;
        public long science;
        public int ships;
        public int stations;

        public FactionData(Type factionAI, string name, Character leader, long credits, long science, int ships, int stations) {
            this.factionAI = factionAI;
            this.name = name;
            this.leader = leader;
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }

        public FactionData(string name, Character leader, long credits, long science, int ships, int stations) {
            this.factionAI = typeof(SimulationFactionAI);
            this.name = name;
            this.leader = leader;
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }

        public FactionData(Type factionAI, string name, long credits, long science, int ships, int stations) {
            this.factionAI = factionAI;
            this.name = name;
            this.leader = Character.GenerateCharacter();
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }

        public FactionData(string name, long credits, long science, int ships, int stations) {
            this.factionAI = typeof(SimulationFactionAI);
            this.name = name;
            this.leader = Character.GenerateCharacter();
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }
    }

    public void SetUpFaction(int factionIndex, FactionData factionData, BattleManager.PositionGiver positionGiver, int startingResearchCost) {
        units = new List<Unit>((factionData.ships + factionData.stations) * 5);
        base.SetupObjectGroup(units, false);
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            SetPosition(targetPosition.Value);
        else
            SetPosition(Vector2.zero);
        unitsNotInFleet = new List<Unit>((factionData.ships + factionData.stations) * 5);
        ships = new List<Ship>(factionData.ships * 5);
        fleets = new List<Fleet>(10);
        stations = new List<Station>(factionData.stations * 5);
        stationBlueprints = new List<Station>();
        activeMiningStations = new List<MiningStation>();
        enemyFactions = new List<Faction>();
        this.factionIndex = factionIndex;
        factionAI = (FactionAI)gameObject.AddComponent(factionData.factionAI);
        factionAI.SetupFactionAI(this);
        GenerateFaction(factionData, startingResearchCost);
        factionAI.GenerateFactionAI();
        commManager = GetComponent<FactionCommManager>();
        commManager.SetupCommunicationManager(this, factionData.leader);
    }

    public void GenerateFaction(FactionData factionData, int startingResearchCost) {
        name = factionData.name;
        credits = factionData.credits;
        science = factionData.science;
        researchCost = startingResearchCost;
        researchCostExtra = 0;
        Discoveries = 0;
        improvementModifiers = new float[13];
        for (int i = 0; i < improvementModifiers.Length; i++) {
            improvementModifiers[i] = 1;
        }
        improvementDiscoveryCount = new int[13];
        for (int i = 0; i < improvementDiscoveryCount.Length; i++) {
            improvementDiscoveryCount[i] = 0;
        }
        int shipCount = factionData.ships;
        if (factionData.stations > 0) {
            BattleManager.Instance.CreateNewStation(new Station.StationData(factionIndex, Station.StationType.FleetCommand, "FleetCommand", GetPosition(), Random.Range(0, 360)));
            for (int i = 0; i < factionData.stations - 1; i++) {
                MiningStation newStation = BattleManager.Instance.CreateNewStation(new Station.StationData(factionIndex, Station.StationType.MiningStation, "MiningStation", GetPosition(), Random.Range(0, 360))).GetComponent<MiningStation>();
                if (shipCount > 0) {
                    newStation.BuildShip(Ship.ShipClass.Transport);
                    shipCount--;
                }
            }
        }
        for (int i = 0; i < shipCount; i++) {
            if (GetFleetCommand() != null) {
                int randomNum = Random.Range(0, 3);
                if (randomNum == 0) {
                    GetFleetCommand().BuildShip(Ship.ShipClass.Aria);
                } else if (randomNum == 1) {
                    GetFleetCommand().BuildShip(Ship.ShipClass.Lancer);
                } else if (randomNum == 2) {
                    GetFleetCommand().BuildShip(Ship.ShipClass.Aterna);
                }
            } else
                BattleManager.Instance.CreateNewShip(new Ship.ShipData(factionIndex, Ship.ShipClass.Aria, "Aria", new Vector2(Random.Range(-100, 100), Random.Range(-100, 100)), Random.Range(0, 360)));
        }
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.GetAllStars()) {
            if (Vector2.Distance(position, star.GetPosition()) <= minDistanceFromObject + star.GetSize()) {
                return false;
            }
        }
        foreach (var asteroidField in BattleManager.Instance.GetAllAsteroidFields()) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <= minDistanceFromObject + asteroidField.GetSize()) {
                return false;
            }
        }
        foreach (var faction in BattleManager.Instance.GetAllFactions()) {
            if (faction == this)
                continue;
            if (Vector2.Distance(position, faction.GetPosition()) <= minDistanceFromObject + 5000) {
                return false;
            }
        }
        return true;
    }

    #region ObjectListControlls
    public void AddEnemyFaction(Faction faction) {
        enemyFactions.Add(faction);
        if (LocalPlayer.Instance.faction == this) {
            LocalPlayer.Instance.UpdateFactionColors();
        }
    }

    public void AddUnit(Unit unit) {
        units.Add(unit);
        unitsNotInFleet.Add(unit);
    }

    public void RemoveUnit(Unit unit) {
        units.Remove(unit);
        unitsNotInFleet.Remove(unit);
    }

    public void AddShip(Ship ship) {
        AddUnit(ship);
        ships.Add(ship);
    }

    public void RemoveShip(Ship ship) {
        RemoveUnit(ship);
        ships.Remove(ship);
    }

    public void AddStation(Station station) {
        AddUnit(station);
        stations.Add(station);
    }

    public void RemoveStation(Station station) {
        RemoveUnit(station);
        stations.Remove(station);
    }

    public void AddStationBlueprint(Station station) {
        stationBlueprints.Add(station);
    }

    public void RemoveStationBlueprint(Station station) {
        stationBlueprints.Remove(station);
    }

    public void AddMinningStation(MiningStation miningStation) {
        activeMiningStations.Add(miningStation);
    }

    public void RemoveMinningStation(MiningStation miningStation) {
        activeMiningStations.Remove(miningStation);
    }

    public Fleet CreateNewFleet(string fleetName, Ship ship) {
        Fleet newFleet = Instantiate((GameObject)Resources.Load("Prefabs/Fleet"), GetFleetTransform()).GetComponent<Fleet>();
        newFleet.SetupFleet(this, fleetName, ship);
        return newFleet;
    }

    public Fleet CreateNewFleet(string fleetName, List<Ship> ships) {
        Fleet newFleet = Instantiate((GameObject)Resources.Load("Prefabs/Fleet"), GetFleetTransform()).GetComponent<Fleet>();
        newFleet.SetupFleet(this, fleetName, ships);
        fleets.Add(newFleet);
        return newFleet;
    }

    public void RemoveFleet(Fleet fleetAI) {
        fleets.Remove(fleetAI);
    }

    #endregion

    #region CreditsAndScience
    public void AddCredits(long credits) {
        this.credits += credits;
    }

    public bool UseCredits(long credits) {
        if (this.credits > credits) {
            this.credits -= credits;
            return true;
        }
        return false;
    }

    public bool TransferCredits(long credits, Faction faction) {
        if (UseCredits(credits)) {
            faction.AddCredits(credits);
            return true;
        }
        return false;
    }

    public void AddScience(long science) {
        this.science += science;
    }

    /// <summary>
    /// Adds a discovery in the an improvement area in the given research area.
    /// </summary>
    /// <param name="researchArea">the given research area to improve</param>
    /// <param name="free">should the discovery cost science or not?</param>
    public void DiscoverResearchArea(ResearchAreas researchArea, bool free = false) {
        if (!free) {
            if (science < researchCost)
                return;
            science -= researchCost;
            researchCost = (int)(researchCost * BattleManager.Instance.researchModifier);
            researchCostExtra = researchCost * BattleManager.Instance.researchModifier - researchCost + researchCostExtra;
            if (researchCostExtra > 0) {
                researchCost += (int)researchCostExtra;
                researchCostExtra -= (int)researchCostExtra;
            }
        }
        Discoveries++;
        int improvementArea;
        switch (researchArea) {
            case ResearchAreas.Engineering:
                improvementArea = Random.Range(0, 4);
                if (improvementArea == 0) {
                    improvementModifiers[(int)ImprovementAreas.HullStrength] += .2f;
                    improvementDiscoveryCount[(int)ImprovementAreas.HullStrength]++;
                } else if (improvementArea == 1) {
                    improvementModifiers[(int)ImprovementAreas.ProjectileDamage] += .15f;
                    improvementDiscoveryCount[(int)ImprovementAreas.ProjectileDamage]++;
                } else if (improvementArea == 2) {
                    improvementModifiers[(int)ImprovementAreas.ProjectileReload] += .2f;
                    improvementDiscoveryCount[(int)ImprovementAreas.ProjectileReload]++;
                } else if (improvementArea == 3) {
                    improvementModifiers[(int)ImprovementAreas.ProjectileRange] += .15f;
                    improvementDiscoveryCount[(int)ImprovementAreas.ProjectileRange]++;
                    UpdateUnitWeaponRanges();
                }
                break;
            case ResearchAreas.Electricity:
                improvementArea = Random.Range(0, 5);
                if (improvementArea == 0) {
                    improvementModifiers[(int)ImprovementAreas.ShieldHealth] += .25f;
                    improvementDiscoveryCount[(int)ImprovementAreas.ShieldHealth]++;
                } else if (improvementArea == 1) {
                    improvementModifiers[(int)ImprovementAreas.ShieldRegen] += .3f;
                    improvementDiscoveryCount[(int)ImprovementAreas.ShieldRegen]++;
                } else if (improvementArea == 2) {
                    improvementModifiers[(int)ImprovementAreas.LaserDamage] += .2f;
                    improvementDiscoveryCount[(int)ImprovementAreas.LaserDamage]++;
                } else if (improvementArea == 3) {
                    improvementModifiers[(int)ImprovementAreas.LaserReload] += .15f;
                    improvementDiscoveryCount[(int)ImprovementAreas.LaserReload]++;
                } else if (improvementArea == 4) {
                    improvementModifiers[(int)ImprovementAreas.LaserRange] += .2f;
                    improvementDiscoveryCount[(int)ImprovementAreas.LaserRange]++;
                    UpdateUnitWeaponRanges();
                }
                break;
            case ResearchAreas.Chemicals:
                improvementArea = Random.Range(0, 4);
                if (improvementArea == 0) {
                    improvementModifiers[(int)ImprovementAreas.ThrustPower] += .15f;
                    improvementDiscoveryCount[(int)ImprovementAreas.ThrustPower]++;
                    UpdateShipThrustPower();
                } else if (improvementArea == 1) {
                    improvementModifiers[(int)ImprovementAreas.MissileDamage] += .2f;
                    improvementDiscoveryCount[(int)ImprovementAreas.MissileDamage]++;
                } else if (improvementArea == 2) {
                    improvementModifiers[(int)ImprovementAreas.MissileReload] += .15f;
                    improvementDiscoveryCount[(int)ImprovementAreas.MissileReload]++;
                } else if (improvementArea == 3) {
                    improvementModifiers[(int)ImprovementAreas.MissileRange] += .15f;
                    improvementDiscoveryCount[(int)ImprovementAreas.MissileRange]++;
                    UpdateUnitWeaponRanges();
                }
                break;
        }
    }
    #endregion

    #region Update
    public void UpdateFaction(float deltaTime) {
        factionAI.UpdateFactionAI(deltaTime);
        UpdateObjectGroup(true);
    }

    public void UpdateFleets(float deltaTime) {
        for (int i = 0; i < fleets.Count; i++) {
            Profiler.BeginSample("UpdateFleet");
            fleets[i].UpdateFleet(deltaTime);
            Profiler.EndSample();
        }
    }

    public void UpdateFactionResearch() {
        DiscoverResearchArea((ResearchAreas)Random.Range(0, 3));
    }

    void UpdateUnitWeaponRanges() {
        for (int i = 0; i < units.Count; i++) {
            units[i].SetupWeaponRanges();
        }
    }

    void UpdateShipThrustPower() {
        for (int i = 0; i < ships.Count; i++) {
            ships[i].SetupThrusters();
        }
    }
    #endregion

    #region HelperMethods
    public bool IsAtWarWithFaction(Faction faction) {
        return enemyFactions.Contains(faction);
    }

    /// <summary>
    /// Gets the ammount of asteroid fields that are not empty and do not have a freindly station nearby.
    /// </summary>
    /// <returns>the available asteroid field count</returns>
    public int GetAvailableAsteroidFieldsCount() {
        int count = 0;
        for (int i = 0; i < BattleManager.Instance.GetAllAsteroidFields().Count; i++) {
            AsteroidField targetAsteroidField = BattleManager.Instance.GetAllAsteroidFields()[i];
            if (targetAsteroidField.totalResources <= 0)
                continue;
            bool hasFeindlyStation = false;
            foreach (Station freindlyStation in stations) {
                if (freindlyStation.stationType == Station.StationType.MiningStation && Vector2.Distance(freindlyStation.GetPosition(), targetAsteroidField.GetPosition()) <= ((MiningStation)freindlyStation).GetMiningRange() + freindlyStation.GetSize() + targetAsteroidField.GetSize()) {
                    hasFeindlyStation = true;
                    break;
                }
            }
            if (hasFeindlyStation)
                continue;
            foreach (Station freindlyStation in stationBlueprints) {
                if (freindlyStation.stationType == Station.StationType.MiningStation && Vector2.Distance(freindlyStation.GetPosition(), targetAsteroidField.GetPosition()) <= ((MiningStation)freindlyStation).GetMiningRange() + freindlyStation.GetSize() + targetAsteroidField.GetSize()) {
                    hasFeindlyStation = true;
                    break;
                }
            }
            if (hasFeindlyStation)
                continue;
            else
                count++;
        }
        return count;
    }

    public List<AsteroidField> GetClosestAvailableAsteroidFields(Vector2 position) {
        List<AsteroidField> eligibleAsteroidFields = new List<AsteroidField>();
        List<float> distances = new List<float>();
        for (int i = 0; i < BattleManager.Instance.GetAllAsteroidFields().Count; i++) {
            AsteroidField targetAsteroidField = BattleManager.Instance.GetAllAsteroidFields()[i];
            if (targetAsteroidField.totalResources <= 0)
                continue;
            bool hasFeindlyStation = false;
            foreach (Station freindlyStation in stations) {
                if (freindlyStation.stationType == Station.StationType.MiningStation && Vector2.Distance(freindlyStation.GetPosition(), targetAsteroidField.GetPosition()) <= ((MiningStation)freindlyStation).GetMiningRange() + freindlyStation.GetSize() + targetAsteroidField.GetSize()) {
                    hasFeindlyStation = true;
                }
            }

            foreach (Station freindlyStation in stationBlueprints) {
                if (freindlyStation.stationType == Station.StationType.MiningStation && Vector2.Distance(freindlyStation.GetPosition(), targetAsteroidField.GetPosition()) <= ((MiningStation)freindlyStation).GetMiningRange() + freindlyStation.GetSize() + targetAsteroidField.GetSize()) {
                    hasFeindlyStation = true;
                }
            }
            if (!hasFeindlyStation) {
                float distance = Vector2.Distance(position, targetAsteroidField.GetPosition());
                for (int f = 0; f < eligibleAsteroidFields.Count + 1; f++) {
                    if (f == eligibleAsteroidFields.Count) {
                        eligibleAsteroidFields.Add(targetAsteroidField);
                        distances.Add(distance);
                        break;
                    }
                    if (distance < Vector2.Distance(position, eligibleAsteroidFields[f].GetPosition())) {
                        eligibleAsteroidFields.Insert(f, targetAsteroidField);
                        distances.Insert(f, distance);
                        break;
                    }
                }
            }
        }
        return eligibleAsteroidFields;
    }

    /// <summary>
    /// Gets the closest enemy station to the position or null if there isen't any.
    /// </summary>
    /// <param name="position">the given position</param>
    /// <returns>the closest enemy station to the position</returns>
    public Station GetClosestEnemyStation(Vector2 position) {
        Station station = null;
        float distance = 0;
        foreach (var faction in enemyFactions) {
            for (int i = 0; i < faction.stations.Count; i++) {
                Station targetStation = faction.stations[i];
                if (targetStation == null || !targetStation.IsTargetable())
                    continue;
                float targetDistance = Vector2.Distance(position, targetStation.GetPosition());
                if (station == null || targetDistance < distance) {
                    station = targetStation;
                    distance = targetDistance;
                }
            }
        }
        return station;
    }

    /// <summary>
    /// Gets the closest enemy unit to the position or null if there isen't any.
    /// </summary>
    /// <param name="position">the given position</param>
    /// <returns>the closest enemy unit to the position</returns>
    public Unit GetClosestEnemyUnit(Vector2 position) {
        Unit unit = null;
        float distance = 0;
        foreach (var faction in enemyFactions) {
            for (int i = 0; i < faction.units.Count; i++) {
                Unit targetUnit = faction.units[i];
                if (targetUnit == null || !targetUnit.IsTargetable())
                    continue;
                float targetDistance = Vector2.Distance(position, targetUnit.GetPosition());
                if (unit == null || targetDistance < distance) {
                    unit = targetUnit;
                    distance = targetDistance;
                }
            }
        }
        return unit;
    }

    public Station GetClosestStation(Vector2 position) {
        Station station = null;
        float distance = 0;
        for (int i = 0; i < stations.Count; i++) {
            Station targetStation = stations[i];
            if (targetStation == null || !targetStation.IsSpawned())
                continue;
            float targetDistance = Vector2.Distance(position, targetStation.GetPosition());
            if (station == null || targetDistance < distance) {
                station = targetStation;
                distance = targetDistance;
            }
        }
        return station;
    }

    /// <summary>
    /// Gets the closest minning station that wants transports to the position
    /// </summary>
    /// <param name="position">the given position</param>
    /// <returns>the closest minning station</returns>
    public MiningStation GetClosestMinningStationWantingTransport(Vector2 position) {
        MiningStation minningStation = null;
        float distance = 0;
        int wantedTransportShips = int.MinValue;
        for (int i = 0; i < activeMiningStations.Count; i++) {
            MiningStation targetMinningStation = activeMiningStations[i];
            if (targetMinningStation == null || !targetMinningStation.IsSpawned() || !targetMinningStation.activelyMinning)
                continue;
            float targetDistance = Vector2.Distance(position, targetMinningStation.GetPosition());
            int? targetWantedTransportShips = targetMinningStation.GetMiningStationAI().GetWantedTransportShips();
            if (!targetWantedTransportShips.HasValue)
                continue;
            if ((targetWantedTransportShips > 0 && (targetDistance < distance || wantedTransportShips <= 0)) || (targetWantedTransportShips <= 0 && targetWantedTransportShips > wantedTransportShips)) {
                minningStation = targetMinningStation;
                distance = targetDistance;
                wantedTransportShips = targetWantedTransportShips.Value;
            }
        }
        return minningStation;
    }

    /// <summary>
    /// Gets the total wanted transports of all the factions minning stations
    /// </summary>
    /// <returns>the total wanted transports throughout the faction</returns>
    public int GetTotalWantedTransports() {
        int count = 0;
        for (int i = 0; i < activeMiningStations.Count; i++) {
            MiningStation targetMinningStation = activeMiningStations[i];
            if (targetMinningStation != null && targetMinningStation.IsSpawned()) {
                int? wantedTransportShips = targetMinningStation.GetMiningStationAI().GetWantedTransportShips();
                if (wantedTransportShips.HasValue) {
                    count += wantedTransportShips.Value;
                }
            }
        }
        return count;
    }

    /// <summary>
    /// gets all ships of the given ShipType
    /// </summary>
    /// <param name="shipType">the given ShipType</param>
    /// <returns>all ships of the given ShipiType</returns>
    public int GetShipsOfType(Ship.ShipType shipType) {
        int count = 0;
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].GetShipType() == shipType) {
                count++;
            }
        }
        return count;
    }

    public Ship GetTransportShip(int index) {
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].IsTransportShip()) {
                if (index == 0) {
                    return ships[i];
                }
                index--;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the closest star to the given position
    /// </summary>
    /// <param name="position">the given position</param>
    /// <returns>the closest star</returns>
    public Star GetClosestStar(Vector2 position) {
        Star star = null;
        float distance = 0;
        for (int i = 0; i < BattleManager.Instance.stars.Count; i++) {
            if (BattleManager.Instance.stars[i] == null)
                continue;
            float targetDistance = Vector2.Distance(position, BattleManager.Instance.stars[i].GetPosition());
            if (star == null || targetDistance < distance) {
                star = BattleManager.Instance.stars[i];
                distance = targetDistance;
            }
        }
        return star;
    }

    /// <summary>
    /// Gets the improvement modifier alligned with the given improvement area
    /// </summary>
    /// <param name="improvementArea">the given improvement area</param>
    /// <returns>the improvement modifier of the area</returns>
    public float GetImprovementModifier(ImprovementAreas improvementArea) {
        return improvementModifiers[(int)improvementArea];
    }

    public bool HasEnemy() {
        foreach (var enemyFaction in enemyFactions) {
            if (enemyFaction.units.Count > 0) {
                return true;
            }
        }
        return false;
    }

    public Transform GetShipTransform() {
        return transform.GetChild(0);
    }

    public Transform GetStationTransform() {
        return transform.GetChild(1);
    }

    public Transform GetFleetTransform() {
        return transform.GetChild(2);
    }

    public Shipyard GetFleetCommand() {
        if (factionAI != null && factionAI is SimulationFactionAI) {
            return ((SimulationFactionAI)factionAI).fleetCommand;
        }
        return null;
    }

    public FactionAI GetFactionAI() {
        return factionAI;
    }

    public FactionCommManager GetFactionCommManager() {
        return commManager;
    }
    #endregion
}