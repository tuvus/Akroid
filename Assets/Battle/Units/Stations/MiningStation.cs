using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningStation : Station {

    public List<Asteroid> nearbyAsteroids;
    [SerializeField] private int miningAmmount;
    [SerializeField] private int miningTime;
    [SerializeField] private int miningRange;

    public override void SetupUnit(string name, Faction faction, BattleManager.PositionGiver positionGiver, float rotation) {
        base.SetupUnit(name, faction, positionGiver, rotation);
        nearbyAsteroids = new List<Asteroid>(10);
    }

    public override Ship BuildShip(Ship.ShipClass shipClass, long cost, bool undock = false) {
        Ship ship = base.BuildShip(shipClass, cost, undock);
        GetMiningStationAI().AddTransportShip(ship);
        return ship;
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        List<AsteroidField> eligibleAsteroidFields = faction.GetClosestAvailableAsteroidFields(positionGiver.position);

        for (int i = 0; i < eligibleAsteroidFields.Count; i++) {
            Vector2 targetCenterPosition = Vector2.MoveTowards(eligibleAsteroidFields[i].GetPosition(), positionGiver.position, eligibleAsteroidFields[i].size + GetSize() + 10);
            Vector2? targetLocationAsteroidField = BattleManager.Instance.FindFreeLocationIncrament(new BattleManager.PositionGiver(targetCenterPosition, positionGiver.minDistance, positionGiver.maxDistance, positionGiver.incrementDistance, positionGiver.distanceFromObject, positionGiver.numberOfTries), this);
            if (targetLocationAsteroidField.HasValue)
                return targetLocationAsteroidField.Value;
        }
        Vector2? targetLocation = BattleManager.Instance.FindFreeLocationIncrament(positionGiver, this);
        if (targetLocation.HasValue)
            return targetLocation.Value;

        return positionGiver.position;
    }

    public int GetMiningAmmount() {
        return miningAmmount;
    }

    public int GetMiningTime() {
        return miningTime;
    }

    public int GetMiningRange() {
        return miningRange;
    }

    public MiningStationAI GetMiningStationAI() {
        return (MiningStationAI)stationAI;
    }
}
