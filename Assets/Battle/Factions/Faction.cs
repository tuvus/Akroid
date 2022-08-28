using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Faction : MonoBehaviour, IPositionConfirmer {
    public Vector2 factionPosition;
    [SerializeField] FactionAI factionAI;
    [SerializeField] public new string name { get; private set; }
    [SerializeField] public long credits { get; private set; }
    [SerializeField] public long science { get; private set; }
    private long researchCost;
    public int Discoveries { get; private set; }
    public float HealthModifier { get; private set; }
    public float ShieldHealthModifier { get; private set; }
    public float ShieldRegenModifier { get; private set; }
    public float ProjectileDamageModifier { get; private set; }
    public float ProjectileReloadModifier { get; private set; }
    public float ProjectileRangeModifier { get; private set; }
    public float LaserDamageModifier { get; private set; }
    public float LaserChargeModifier { get; private set; }
    public float LaserRangeModifier { get; private set; }
    public float MissileDamageModifier { get; private set; }
    public float MissileReloadModifier { get; private set; }
    public float MissileRangeModifier { get; private set; }
    public float ThrusterPowerModifier { get; private set; }

    public int factionIndex { get; private set; }

    public List<Unit> units { get; private set; }
    public List<Ship> ships { get; private set; }
    public List<Station> stations { get; private set; }
    public List<Station> stationBlueprints { get; private set; }

    public List<MiningStation> activeMiningStations { get; private set; }

    public List<Faction> enemyFactions { get; private set; }

    public float factionUnitsSize;

    public struct FactionData {
        public Type factionAI;
        public string name;
        public long credits;
        public long science;
        public int ships;
        public int stations;

        public FactionData(Type factionAI, string name, long credits, long science, int ships, int stations) {
            this.factionAI = factionAI;
            this.name = name;
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }

        public FactionData(string name, long credits, long science, int ships, int stations) {
            this.factionAI = typeof(SimulationFactionAI);
            this.name = name;
            this.credits = credits;
            this.science = science;
            this.ships = ships;
            this.stations = stations;
        }
    }

    public void SetUpFaction(int factionIndex, FactionData factionData, BattleManager.PositionGiver positionGiver, int startingResearchCost) {
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrament(positionGiver, this);
        if (targetPosition.HasValue)
            factionPosition = targetPosition.Value;
        else
            factionPosition = Vector2.zero;
        units = new List<Unit>();
        ships = new List<Ship>();
        stations = new List<Station>();
        stationBlueprints = new List<Station>();
        activeMiningStations = new List<MiningStation>();
        enemyFactions = new List<Faction>();
        this.factionIndex = factionIndex;
        factionAI = (FactionAI)gameObject.AddComponent(factionData.factionAI);
        factionAI.SetupFactionAI(this);
        GenerateFaction(factionData, startingResearchCost);
        factionAI.GenerateFactionAI();
    }

    public void GenerateFaction(FactionData factionData, int startingResearchCost) {
        name = factionData.name;
        credits = factionData.credits;
        science = factionData.science;
        researchCost = startingResearchCost;
        Discoveries = 0;
        HealthModifier = 1;
        ShieldHealthModifier = 1;
        ShieldRegenModifier = 1;
        ProjectileDamageModifier = 1;
        ProjectileReloadModifier = 1;
        ProjectileRangeModifier = 1;
        LaserDamageModifier = 1;
        LaserChargeModifier = 1;
        LaserRangeModifier = 1;
        MissileDamageModifier = 1;
        MissileReloadModifier = 1;
        MissileRangeModifier = 1;
        ThrusterPowerModifier = 1;
        int shipCount = factionData.ships;
        if (factionData.stations > 0) {
            BattleManager.Instance.CreateNewStation(new Station.StationData(factionIndex, Station.StationType.FleetCommand, "FleetCommand", factionPosition, Random.Range(0, 360)));
            for (int i = 0; i < factionData.stations - 1; i++) {
                MiningStation newStation = BattleManager.Instance.CreateNewStation(new Station.StationData(factionIndex, Station.StationType.MiningStation, "MiningStation", factionPosition, Random.Range(0, 360))).GetComponent<MiningStation>();
                if (shipCount > 0) {
                    Ship newTransport = newStation.BuildShip(Ship.ShipClass.Transport, 0, false);
                    shipCount--;
                }
            }
        }
        for (int i = 0; i < shipCount; i++) {
            if (GetFleetCommand() != null)
                GetFleetCommand().BuildShip(Ship.ShipClass.Lancer, 0);
            else
                BattleManager.Instance.CreateNewShip(new Ship.ShipData(factionIndex, Ship.ShipClass.Aria, "Aria", new Vector2(Random.Range(-100, 100), Random.Range(-100, 100)), Random.Range(0, 360)));
        }
    }

    public void AddEnemyFaction(Faction faction) {
        enemyFactions.Add(faction);
    }

    public void RemoveUnit(Unit unit) {
        units.Remove(unit);
    }

    public void AddShip(Ship ship) {
        ships.Add(ship);
        units.Add(ship);
    }

    public void RemoveShip(Ship ship) {
        units.Remove(ship);
        ships.Remove(ship);
    }

    public void AddStation(Station station) {
        stations.Add(station);
        units.Add(station);
    }

    public void RemoveStation(Station station) {
        units.Remove(station);
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

    public bool UseCredits(long credits) {
        if (this.credits > credits) {
            this.credits -= credits;
            return true;
        }
        return false;
    }

    public void AddScience(long science) {
        this.science += science;
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.GetAllStars()) {
            if (Vector2.Distance(position, star.position) <= minDistanceFromObject + star.GetSize()) {
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
            if (Vector2.Distance(position, faction.factionPosition) <= minDistanceFromObject + 5000) {
                return false;
            }
        }
        return true;
    }

    public void UpdateFaction() {
        factionAI.UpdateFactionAI();
        UpdateFactionTotalUnitSize();
    }

    public void UpdateFactionResearch() {
        if (science >= researchCost) {
            Discoveries++;
            science -= researchCost;
            researchCost = Mathf.RoundToInt(researchCost * BattleManager.Instance.researchModifier);
            int improveArea = Random.Range(0, 13);
            if (improveArea == 0) {
                HealthModifier += .2f;
            } else if (improveArea == 1) {
                ShieldHealthModifier += .15f;
            } else if (improveArea == 2) {
                ShieldRegenModifier += .1f;
            } else if (improveArea == 3) {
                ProjectileDamageModifier += .1f;
            } else if (improveArea == 4) {
                ProjectileReloadModifier += .12f;
            } else if (improveArea == 5) {
                ProjectileRangeModifier += .2f;
                UpdateUnitTurretRanges();
            } else if (improveArea == 6) {
                LaserDamageModifier += .15f;
            } else if (improveArea == 7) {
                LaserChargeModifier += .12f;
            } else if (improveArea == 8) {
                LaserRangeModifier += .15f;
                UpdateUnitTurretRanges();
            } else if (improveArea == 9) {
                MissileDamageModifier += .2f;
            } else if (improveArea == 10) {
                MissileReloadModifier += .2f;
            } else if (improveArea == 11) {
                MissileRangeModifier += .15f;
                UpdateUnitTurretRanges();
            } else if (improveArea == 12) {
                ThrusterPowerModifier += .15f;
                UpdateShipThrustPower();
            }
        }
    }

    public void UpdateFactionTotalUnitSize() {
        factionUnitsSize = 0;
        for (int i = 0; i < stations.Count; i++) {
            factionUnitsSize = Mathf.Max(factionUnitsSize, Vector2.Distance(factionPosition, stations[i].GetPosition()) + stations[i].GetSize());
        }
        for (int i = 0; i < ships.Count; i++) {
            factionUnitsSize = Mathf.Max(factionUnitsSize, Vector2.Distance(factionPosition, ships[i].GetPosition()) + ships[i].GetSize());
        }
    }

    void UpdateUnitTurretRanges() {
        for (int i = 0; i < units.Count; i++) {
            units[i].SetupWeaponRanges();
        }
    }

    void UpdateShipThrustPower() {
        for (int i = 0; i < ships.Count; i++) {
            ships[i].SetupThrusters();
        }
    }

    #region GetMethods
    public bool IsAtWarWithFaction(Faction faction) {
        return enemyFactions.Contains(faction);
    }

    public List<Station> GetAllFactionStations() {
        return stations;
    }

    public int GetAvailableAsteroidFieldsCount() {
        int count = 0;
        for (int i = 0; i < BattleManager.Instance.GetAllAsteroidFields().Count; i++) {
            AsteroidField targetAsteroidField = BattleManager.Instance.GetAllAsteroidFields()[i];
            if (targetAsteroidField.totalResources <= 0)
                continue;
            bool hasFeindlyStation = false;
            foreach (Station freindlyStation in GetAllFactionStations()) {
                if (freindlyStation.stationType == Station.StationType.MiningStation && Vector2.Distance(freindlyStation.GetPosition(), targetAsteroidField.GetPosition()) <= ((MiningStation)freindlyStation).GetMiningRange() + freindlyStation.GetSize() + targetAsteroidField.GetSize()) {
                    hasFeindlyStation = true;
                }
            }
            if (!hasFeindlyStation) {
                count++;
            }
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
            foreach (Station freindlyStation in GetAllFactionStations()) {
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

    public Station GetClosestEnemyStation(Vector2 position) {
        Station station = null;
        float distance = 0;
        foreach (var faction in enemyFactions) {
            for (int i = 0; i < faction.GetAllFactionStations().Count; i++) {
                Station targetStation = faction.GetAllFactionStations()[i];
                if (targetStation == null || !targetStation.IsSpawned())
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
            if (targetWantedTransportShips > wantedTransportShips || (targetWantedTransportShips == wantedTransportShips && targetDistance < distance)) {
                minningStation = targetMinningStation;
                distance = targetDistance;
                wantedTransportShips = targetWantedTransportShips.Value;
            }
        }
        return minningStation;
    }

    public int GetTotalWantedTransports() {
        int count = 0;
        for (int i = 0; i < activeMiningStations.Count; i++) {
            MiningStation targetMinningStation = activeMiningStations[i];
            if (targetMinningStation == null || !targetMinningStation.IsSpawned()) {
                int? wantedTransportShips = targetMinningStation.GetMiningStationAI().GetWantedTransportShips();
                if (wantedTransportShips.HasValue) {
                    count += wantedTransportShips.Value;
                }
            }

        }
        return count;
    }

    public int GetShipsOfType(Ship.ShipType shipType) {
        int count = 0;
        for (int i = 0; i < ships.Count; i++) {
            if (ships[i].GetShipType() == shipType) {
                count++;
            }
        }
        return count;
    }

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

    public bool HasEnemy() {
        foreach (var enemyFaction in enemyFactions) {
            if (enemyFaction.stations.Count >= 1) {
                return true;
            }
        }
        return false;
    }

    public Ship.ShipBlueprint GetTransportBlueprint() {
        return new Ship.ShipBlueprint(Ship.ShipClass.Transport, "Transport", 3000,
            new List<CargoBay.CargoTypes>() { CargoBay.CargoTypes.Metal }, new List<float>() { 2400 });
    }

    public Transform GetShipTransform() {
        return transform.GetChild(0);
    }

    public Transform GetStationTransform() {
        return transform.GetChild(1);
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
    #endregion
}