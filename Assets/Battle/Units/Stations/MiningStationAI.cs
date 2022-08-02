using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningStationAI : StationAI {

    public List<Ship> transportShips;
    int transportShipsScheduled;

    public override void SetupStationAI(Station station) {
        base.SetupStationAI(station);
        transportShips = new List<Ship>(10);
    }

    public override void UpdateAI() {
        base.UpdateAI();
        UpdateMinningStation();
    }

    private void UpdateMinningStation() {
        if (waitTime <= 0) {
            MannageAsteroidMinning();
            waitTime = GetMiningStation().GetMiningTime();
            ManageMinningStationTransports();
        }
        if (cargoTime <= 0) {
            ManageMinningStationCargo();
        }
    }

    void MannageAsteroidMinning() {
        if (GetMiningStation().nearbyAsteroids.Count == 0) {
            List<Asteroid> tempAsteroids = new List<Asteroid>(10);
            foreach (var asteroidField in BattleManager.Instance.GetAllAsteroidFields()) {
                if (asteroidField.totalResources <= 0)
                    continue;
                float tempDistance = Vector2.Distance(transform.position, asteroidField.position);
                if (tempDistance <= GetMiningStation().GetMiningRange() + asteroidField.size) {
                    foreach (var asteroid in asteroidField.asteroids) {
                        tempAsteroids.Add(asteroid);
                    }
                }
            }
            while (tempAsteroids.Count > 0) {
                Asteroid closest = null;
                float closestDist = 0;
                for (int i = 0; i < tempAsteroids.Count; i++) {
                    float tempDist = Vector2.Distance(transform.position, tempAsteroids[i].GetPosition());
                    if (closest == null || tempDist < closestDist) {
                        closest = tempAsteroids[i];
                        closestDist = tempDist;
                    }
                }
                GetMiningStation().nearbyAsteroids.Add(closest);
                tempAsteroids.Remove(closest);
            }
        }
        while (GetMiningStation().nearbyAsteroids.Count >= 1 && (GetMiningStation().nearbyAsteroids[0] == null || !GetMiningStation().nearbyAsteroids[0].HasResources())) {
            GetMiningStation().nearbyAsteroids.RemoveAt(0);
        }
        if (GetMiningStation().nearbyAsteroids.Count > 0) {
            station.GetCargoBay().LoadCargo(GetMiningStation().nearbyAsteroids[0].MineAsteroid((int)(GetMiningStation().GetMiningAmmount())), CargoBay.CargoTypes.Metal);
        }
    }

    void ManageMinningStationCargo() {
        for (int i = 0; i < transportShips.Count; i++) {
            if (transportShips[i] == null) {
                transportShips.RemoveAt(i);
                i--;
            }
        }
        for (int i = 0; i < station.GetHanger().GetShips().Count; i++) {
            Ship ship = station.GetHanger().GetShips()[i];
            if (ship.GetShipType() == Ship.ShipType.Transport) {
                ship.GetCargoBay().LoadCargoFromBay(station.GetCargoBay(), CargoBay.CargoTypes.Metal, 600);
                if (ship.GetCargoBay().IsCargoFullOfType(CargoBay.CargoTypes.Metal) && station.faction.GetFleetCommand() != null) {
                    //ship.UndockShip(station.faction.GetFleetCommand().GetPosition());
                    ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station.faction.GetFleetCommand()), ShipAI.CommandAction.AddToEnd);
                    ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Idle), ShipAI.CommandAction.AddToEnd);
                    ship.shipAI.AddUnitAICommand(new UnitAICommand(UnitAICommand.CommandType.Dock, station), ShipAI.CommandAction.AddToEnd);
                    if (!transportShips.Contains(ship))
                        transportShips.Add(ship);
                    i--;
                }
                cargoTime = 1;
                break;
            }
        }
    }

    void ManageMinningStationTransports() {
        if (transportShips.Count + transportShipsScheduled < GetWantedTransportShips()) {
            AddTransportShipToBuildQueue();
        }
    }

    public void AddTransportShip(Ship ship) {
        transportShips.Add(ship);
        transportShipsScheduled = Mathf.Max(0, transportShipsScheduled - 1);
    }

    public int GetWantedTransportShips() {
        if (!station.IsBuilt())
            return 0;
        return 1 - transportShips.Count;
    }

    public MiningStation GetMiningStation() {
        return (MiningStation)station;
    }

    public void AddTransportShipToBuildQueue() {
        station.faction.GetFleetCommand().GetConstructionBay().AddConstructionToBeginningQueue(station.faction.GetTransportBlueprint());
        transportShipsScheduled++;
    }
}
