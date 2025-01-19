using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class Faction : ObjectGroup<Unit>, IPositionConfirmer {
    [SerializeField] FactionAI factionAI;
    [SerializeField] FactionCommManager commManager;
    [field: SerializeField] public Color color { get; protected set; }
    [field: SerializeField] public string name { get; protected set; }
    [field: SerializeField] public string abbreviatedName { get; protected set; }
    [field: SerializeField] public long credits { get; protected set; }
    [field: SerializeField] public long science { get; protected set; }
    public long researchCost { get; protected set; }
    private double researchCostExtra;
    public int Discoveries { get; protected set; }

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

    public HashSet<Unit> units { get; private set; }
    public HashSet<Ship> ships { get; private set; }
    public HashSet<Fleet> fleets { get; private set; }
    public HashSet<Station> stations { get; private set; }
    public HashSet<Planet> planets { get; private set; }
    public HashSet<Station> stationBlueprints { get; private set; }

    public HashSet<MiningStation> activeMiningStations { get; private set; }

    public HashSet<Faction> enemyFactions { get; private set; }
    public HashSet<UnitGroup> unitGroups { get; private set; }

    /// <summary>
    /// Stores close enemy groups stored in ascending order of distance to the faction.
    /// </summary>
    public List<UnitGroup> closeEnemyGroups { get; private set; }

    /// <summary>
    /// The distance to each of the target groups sorted in ascending order of distance to the faction.
    /// The size of the target group is taken into account
    /// </summary>
    public List<float> closeEnemyGroupsDistance { get; private set; }

    public UnitGroup baseGroup { get; private set; }

    private Random random;

    public struct FactionData {
        public Type factionAI;
        public string name;
        public string abbreviatedName;
        public Color color;
        public Character leader;
        public long credits;
        public long science;
        public int ships;
        public int stations;

        public FactionData(Type factionAI, string name, string abbreviatedName, Color color, Character leader, long credits, long science,
            int ships, int stations) {
            this.factionAI = factionAI;
            this.name = name;
            this.abbreviatedName = abbreviatedName;
            this.color = color;
            this.leader = leader;
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }

        public FactionData(string name, string abbreviatedName, Color color, Character leader, long credits, long science, int ships,
            int stations) {
            this.factionAI = typeof(SimulationFactionAI);
            this.name = name;
            this.abbreviatedName = abbreviatedName;
            this.color = color;
            this.leader = leader;
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }

        public FactionData(Type factionAI, string name, string abbreviatedName, Color color, long credits, long science, int ships,
            int stations) {
            this.factionAI = factionAI;
            this.name = name;
            this.abbreviatedName = abbreviatedName;
            this.color = color;
            this.leader = Character.GenerateCharacter();
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }

        public FactionData(string name, string abbreviatedName, Color color, long credits, long science, int ships, int stations) {
            this.factionAI = typeof(SimulationFactionAI);
            this.name = name;
            this.abbreviatedName = abbreviatedName;
            this.color = color;
            this.leader = Character.GenerateCharacter();
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }
    }

    public Faction() {
        credits = 0;
    }

    public Faction(BattleManager battleManager, FactionData factionData, BattleManager.PositionGiver positionGiver) :
        base(battleManager, new HashSet<Unit>((factionData.ships + factionData.stations) * 5), false) {
        random = new Random(battleManager.GetRandomSeed());
        units = battleObjects;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            SetPosition(targetPosition.Value);
        else
            SetPosition(Vector2.zero);
        this.color = factionData.color;
        ships = new HashSet<Ship>(factionData.ships * 5);
        fleets = new HashSet<Fleet>(10);
        stations = new HashSet<Station>(factionData.stations * 5);
        planets = new HashSet<Planet>();
        stationBlueprints = new HashSet<Station>();
        activeMiningStations = new HashSet<MiningStation>();
        enemyFactions = new HashSet<Faction>();
        unitGroups = new HashSet<UnitGroup>(100);
        closeEnemyGroups = new List<UnitGroup>(100);
        closeEnemyGroupsDistance = new List<float>(100);
        baseGroup = CreateNewUnitGroup("BaseGroup", false, new HashSet<Unit>(100));
        factionAI = (FactionAI)Activator.CreateInstance(factionData.factionAI, battleManager, this);
        commManager = new FactionCommManager(battleManager, this, factionData.leader);
    }

    public void GenerateFaction(FactionData factionData, int startingResearchCost) {
        name = factionData.name;
        abbreviatedName = factionData.abbreviatedName;
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
            BattleManager.PositionGiver stationPositionGiver = new BattleManager.PositionGiver(GetPosition(), 0, 2000, 50, 10, 2);
            Station fleetCommand = battleManager.CreateNewStation(
                new BattleObject.BattleObjectData("FleetCommand", stationPositionGiver, random.NextFloat(0f, 360f), this),
                battleManager.GetStationBlueprint(Station.StationType.FleetCommand).stationScriptableObject, true);
            stationPositionGiver = new BattleManager.PositionGiver(fleetCommand.position, stationPositionGiver);
            for (int i = 0; i < factionData.stations - 1; i++) {
                MiningStation newStation = battleManager.CreateNewMiningStation(
                    new BattleObject.BattleObjectData("MiningStation", stationPositionGiver, random.NextFloat(0, 360), this),
                    (MiningStationScriptableObject)battleManager.GetStationBlueprint(Station.StationType.MiningStation)
                        .stationScriptableObject, true);
                if (shipCount > 0) {
                    newStation.BuildShip(Ship.ShipClass.Transport);
                    shipCount--;
                }
            }
        }

        for (int i = 0; i < math.min(shipCount, 3); i++) {
            GetFleetCommand().BuildShip(Ship.ShipType.GasCollector);
            shipCount--;
        }

        for (int i = 0; i < shipCount; i++) {
            if (GetFleetCommand() != null) {
                int randomNum = random.NextInt(0, 10);
                if (randomNum < 3) {
                    GetFleetCommand().BuildShip(Ship.ShipClass.Aria);
                } else if (randomNum < 8) {
                    GetFleetCommand().BuildShip(Ship.ShipClass.Lancer);
                } else if (randomNum <= 10) {
                    GetFleetCommand().BuildShip(Ship.ShipClass.Aterna);
                }
            } else
                BattleManager.Instance.CreateNewShip(
                    new BattleObject.BattleObjectData("Aria", new Vector2(random.NextFloat(-100, 100), random.NextFloat(-100, 100)),
                        random.NextFloat(0, 360), this), battleManager.GetShipBlueprint(Ship.ShipClass.Aria).shipScriptableObject);
        }
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        if (Vector2.Distance(Vector2.zero, position) <= minDistanceFromObject * 5)
            return false;
        foreach (var star in battleManager.stars) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject * 2 + star.GetSize() + 1000)
                return false;
        }

        foreach (var planet in battleManager.planets) {
            if (Vector2.Distance(position, planet.position) <= minDistanceFromObject + planet.GetSize() + 200)
                return false;
        }

        foreach (var asteroidField in battleManager.asteroidFields) {
            if (Vector2.Distance(position, asteroidField.GetPosition()) <= minDistanceFromObject + asteroidField.GetSize())
                return false;
        }

        foreach (var faction in battleManager.factions) {
            if (faction == this)
                continue;
            if (Vector2.Distance(position, faction.position) <= minDistanceFromObject * 5 + 1000)
                return false;
        }

        return true;
    }

    #region ObjectListControlls

    public void StartWar(Faction otherFaction) {
        AddEnemyFaction(otherFaction);
        otherFaction.AddEnemyFaction(this);
    }

    public void EndWar(Faction otherFaction) {
        RemoveEnemyFaction(otherFaction);
        otherFaction.RemoveEnemyFaction(this);
    }

    public void AddEnemyFaction(Faction otherFaction) {
        enemyFactions.Add(otherFaction);
    }

    public void RemoveEnemyFaction(Faction otherFaction) {
        enemyFactions.Remove(otherFaction);
    }

    public void AddUnit(Unit unit) {
        units.Add(unit);
        unit.SetGroup(baseGroup);
    }

    public void RemoveUnit(Unit unit) {
        units.Remove(unit);
    }

    public void AddShip(Ship ship) {
        AddUnit(ship);
        ships.Add(ship);
    }

    public void RemoveShip(Ship ship) {
        RemoveUnit(ship);
        ships.Remove(ship);
        factionAI.RemoveShip(ship);
    }

    public void TransferShipTo(Ship ship, Faction to) {
        if (!ships.Contains(ship))
            throw new InvalidOperationException("The ship to transfer from this faction isn't owned by this faction.");
        // TODO: Update owned units when a ship is transfered
        // if (LocalPlayer.Instance.GetFaction() == this && LocalPlayer.Instance.player.ownedUnits.Contains(ship)) {
        //     LocalPlayer.Instance.RemoveOwnedUnit(ship);
        // }

        RemoveShip(ship);
        to.AddShip(ship);
        ship.SetFaction(to);
        // if (LocalPlayer.Instance.GetFaction() == to && !LocalPlayer.Instance.lockedOwnedUnits) {
        //     battleManager.GetLocalPlayer().AddOwnedUnit(ship);
        // }
    }

    public void AddStation(Station station) {
        AddUnit(station);
        stations.Add(station);
    }

    public void RemoveStation(Station station) {
        RemoveUnit(station);
        stations.Remove(station);
    }

    public void TransferStationTo(Station station, Faction to) {
        if (!stations.Contains(station))
            throw new InvalidOperationException("The station to transfer from this faction isn't owned by this faction.");
        // TODO: Update owned units when a station is transfered
        // if (LocalPlayer.Instance.GetFaction() == this && LocalPlayer.Instance.player.ownedUnits.Contains(station)) {
        //     LocalPlayer.Instance.RemoveOwnedUnit(station);
        // }

        RemoveStation(station);
        to.AddStation(station);
        station.SetFaction(to);
        // if (LocalPlayer.Instance.GetFaction() == to && !LocalPlayer.Instance.lockedOwnedUnits) {
        //     battleManager.GetLocalPlayer().AddOwnedUnit(station);
        // }
    }

    public void AddStationBlueprint(Station station) {
        stationBlueprints.Add(station);
    }

    public void RemoveStationBlueprint(Station station) {
        stationBlueprints.Remove(station);
    }

    public void AddMiningStation(MiningStation miningStation) {
        activeMiningStations.Add(miningStation);
    }

    public void RemoveMiningStation(MiningStation miningStation) {
        activeMiningStations.Remove(miningStation);
    }

    public void AddPlanet(Planet planet) {
        planets.Add(planet);
    }

    public void RemovePlanet(Planet planet) {
        planets.Remove(planet);
    }

    public Fleet CreateNewFleet(string fleetName, HashSet<Ship> ships) {
        Fleet newFleet = new Fleet(battleManager, this, fleetName, ships);
        fleets.Add(newFleet);
        unitGroups.Add(newFleet);
        battleManager.CreateFleet(newFleet);
        return newFleet;
    }

    public void RemoveFleet(Fleet fleet) {
        fleets.Remove(fleet);
        factionAI.RemoveFleet(fleet);
        unitGroups.Remove(fleet);
        battleManager.RemoveFleet(fleet);
    }

    public UnitGroup CreateNewUnitGroup(string groupName, bool deleteWhenEmpty, HashSet<Unit> units) {
        UnitGroup newUnitGroup = new UnitGroup(battleManager, units, deleteWhenEmpty, true);
        unitGroups.Add(newUnitGroup);
        return newUnitGroup;
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

    /// <summary>
    /// Transferes credits from this faction to the other faction.
    /// </summary>
    /// <param name="faction">The other faction to transfer credits to.</param>
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
    /// Adds a discovery in the improvement area in the given research area.
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
                improvementArea = random.NextInt(0, 4);
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
                improvementArea = random.NextInt(0, 5);
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
                improvementArea = random.NextInt(0, 4);
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

    public void EarlyUpdateFaction() {
        UpdateObjectGroup(true);
        foreach (var unitGroup in unitGroups.ToList()) {
            unitGroup.UpdateObjectGroup();
        }
    }

    public void UpdateNearbyEnemyUnits() {
        closeEnemyGroups.Clear();
        closeEnemyGroupsDistance.Clear();
        foreach (var enemyFaction in enemyFactions) {
            if (Vector2.Distance(GetPosition(), enemyFaction.position) > GetSize() * 1.2 + enemyFaction.GetSize() + 3000)
                continue;
            foreach (var enemyGroup in enemyFaction.unitGroups) {
                AddEnemyGroup(enemyGroup);
            }
        }
    }

    public void UpdateFaction(float deltaTime) {
        commManager.UpdateCommunications();
        factionAI.UpdateFactionAI(deltaTime);
    }

    /// <summary>
    /// Inserts the target group if it is close enough based to closestEnemyGroups.
    /// </summary>
    void AddEnemyGroup(UnitGroup targetGroup) {
        if (targetGroup == null || !targetGroup.IsTargetable()) return;
        float distance = math.max(0, Vector2.Distance(GetPosition(), targetGroup.GetPosition()) - targetGroup.GetSize());
        if (distance > GetSize() * 1.2f + 3000) return;

        for (int f = 0; f < closeEnemyGroupsDistance.Count; f++) {
            if (closeEnemyGroupsDistance[f] < distance) continue;
            closeEnemyGroupsDistance.Insert(f, distance);

            closeEnemyGroups.Insert(f, targetGroup);
            return;
        }

        //Has not been added yet
        closeEnemyGroups.Add(targetGroup);
        closeEnemyGroupsDistance.Add(distance);
    }

    public void UpdateFleets(float deltaTime) {
        fleets.ToList().ForEach(f => f.UpdateFleet(deltaTime));
    }

    public void UpdateFactionResearch() {
        DiscoverResearchArea((ResearchAreas)random.NextFloat(0, 3));
    }

    void UpdateUnitWeaponRanges() {
        foreach (var unit in units) {
            unit.SetupWeaponRanges();
        }
    }

    void UpdateShipThrustPower() {
        foreach (var ship in ships) {
            ship.SetupThrusters();
        }
    }

    #endregion

    #region HelperMethods

    public bool IsAtWarWithFaction(Faction faction) {
        return enemyFactions.Contains(faction);
    }

    /// <summary>
    /// Gets the amount of asteroid fields that are not empty and do not have a friendly station nearby.
    /// </summary>
    /// <returns>the available asteroid field count</returns>
    public int GetAvailableAsteroidFieldsCount() {
        return battleManager.asteroidFields.Count(IsAsteroidAvailableForNewMiningStation);
    }

    public IOrderedEnumerable<AsteroidField> GetClosestAvailableAsteroidFields(Vector2 position) {
        return battleManager.asteroidFields.Where(IsAsteroidAvailableForNewMiningStation)
            .OrderBy(a => Vector2.Distance(a.position, position));
    }

    bool IsAsteroidAvailableForNewMiningStation(AsteroidField asteroidField) {
        if (asteroidField.totalResources <= 0)
            return false;
        return !stations.Concat(stationBlueprints).Any(friendlyStation =>
            friendlyStation.GetStationType() == Station.StationType.MiningStation
            && Vector2.Distance(friendlyStation.GetPosition(), asteroidField.GetPosition()) <=
            ((MiningStation)friendlyStation).GetMiningRange() + friendlyStation.GetSize() + asteroidField.GetSize());
    }

    /// <summary>
    /// Gets the closest enemy station to the position or null if there isn't any.
    /// </summary>
    /// <param name="position">the given position</param>
    /// <returns>the closest enemy station to the position</returns>
    public Station GetClosestEnemyStation(Vector2 position) {
        Station station = null;
        float distance = 0;
        foreach (var faction in enemyFactions) {
            foreach (var targetStation in faction.stations) {
                if (!targetStation.IsTargetable()) continue;
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
    /// Gets the closest enemy unit to the position or null if there isn't any.
    /// </summary>
    /// <param name="position">the given position</param>
    /// <returns>the closest enemy unit to the position</returns>
    public Unit GetClosestEnemyUnit(Vector2 position) {
        Unit unit = null;
        float distance = 0;
        foreach (var faction in enemyFactions) {
            foreach (var targetUnit in faction.units) {
                if (!targetUnit.IsTargetable()) continue;
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
        foreach (var targetStation in stations) {
            if (!targetStation.IsSpawned()) continue;
            float targetDistance = Vector2.Distance(position, targetStation.GetPosition());
            if (station == null || targetDistance < distance) {
                station = targetStation;
                distance = targetDistance;
            }
        }

        return station;
    }

    /// <summary>
    /// Gets the closest mining station that wants transports to the position
    /// </summary>
    /// <param name="position"> The given position </param>
    /// <returns> The closest mining station </returns>
    public MiningStation GetClosestMiningStationWantingTransport(Vector2 position) {
        MiningStation miningStation = null;
        float distance = 0;
        int wantedTransportShips = int.MinValue;
        foreach (var targetMiningStation in activeMiningStations) {
            if (!targetMiningStation.IsSpawned() || !targetMiningStation.activelyMining)
                continue;
            float targetDistance = Vector2.Distance(position, targetMiningStation.GetPosition());
            int? targetWantedTransportShips = targetMiningStation.GetMiningStationAI().GetWantedTransportShips();
            if (!targetWantedTransportShips.HasValue)
                continue;
            if ((targetWantedTransportShips > 0 && (targetDistance < distance || wantedTransportShips <= 0)) ||
                (targetWantedTransportShips <= 0 && targetWantedTransportShips > wantedTransportShips)) {
                miningStation = targetMiningStation;
                distance = targetDistance;
                wantedTransportShips = targetWantedTransportShips.Value;
            }
        }

        return miningStation;
    }

    /// <summary>
    /// Gets the total wanted transports of all the factions mining stations
    /// </summary>
    /// <returns> The total wanted transports throughout the faction </returns>
    public int GetTotalWantedTransports() {
        int temp = activeMiningStations.Where(station => station.IsSpawned())
            .Sum(station => station.GetMiningStationAI().GetWantedTransportShips().GetValueOrDefault(0));
        return activeMiningStations.Where(station => station.IsSpawned())
            .Sum(station => station.GetMiningStationAI().GetWantedTransportShips().GetValueOrDefault(0));
    }

    /// <summary>
    /// Gets the count all ships of the given ShipType
    /// </summary>
    /// <param name="shipType"> The given ShipType </param>
    /// <returns> All ships of the given ShipType </returns>
    public int GetShipCountOfType(Ship.ShipType shipType) {
        return ships.Count(s => s.GetShipType() == shipType);
    }

    public Ship GetTransportShip(int index) {
        List<Ship> transportShips = ships.Where(s => s.IsTransportShip()).ToList();
        if (index >= transportShips.Count) return null;
        return transportShips[index];
    }

    /// <summary>
    /// Gets the closest star to the given position
    /// </summary>
    /// <param name="position"> The given position </param>
    /// <returns> The closest star </returns>
    public Star GetClosestStar(Vector2 position) {
        Star closestStar = null;
        float distance = 0;
        foreach (var star in battleManager.stars) {
            float targetDistance = Vector2.Distance(position, star.position);
            if (closestStar == null || targetDistance < distance) {
                closestStar = star;
                distance = targetDistance;
            }
        }

        return closestStar;
    }

    /// <summary>
    /// Gets the closest gas cloud to the given position
    /// </summary>
    /// <param name="position"> The given position </param>
    /// <returns> The closest gas cloud </returns>
    public GasCloud GetClosestGasCloud(Vector2 position) {
        GasCloud closestGasCloud = null;
        float distance = 0;
        foreach (var gasCloud in battleManager.gasClouds) {
            if (gasCloud.resources <= 0) continue;
            float targetDistance = Vector2.Distance(position, gasCloud.position);
            if (closestGasCloud == null || targetDistance < distance) {
                closestGasCloud = gasCloud;
                distance = targetDistance;
            }
        }

        return closestGasCloud;
    }

    /// <summary>
    /// Gets the improvement modifier aligned with the given improvement area
    /// </summary>
    /// <param name="improvementArea"> The given improvement area </param>
    /// <returns> The improvement modifier of the area </returns>
    public float GetImprovementModifier(ImprovementAreas improvementArea) {
        return improvementModifiers[(int)improvementArea];
    }

    public bool HasEnemy() {
        return enemyFactions.ToList().Any(e => e.units.Count > 0);
    }

    public Station GetFleetCommand() {
        return factionAI.GetFleetCommand();
    }

    public FactionAI GetFactionAI() {
        return factionAI;
    }

    public FactionCommManager GetFactionCommManager() {
        return commManager;
    }

    public Color GetColorTint() {
        return new Color(.4f + color.r * .6f, .4f + color.g * .6f, .4f + color.b * .6f);
    }

    public Color GetColorBackgroundTint(float alpha = .1f) {
        return new Color(color.r, color.g, color.b, alpha);
    }

    #endregion
}
