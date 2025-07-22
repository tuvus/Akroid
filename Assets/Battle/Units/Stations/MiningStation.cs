using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class MiningStation : Station {
    public MiningStationScriptableObject miningStationScriptableObject { get; }

    public MiningStation(BattleObjectData battleObjectData, BattleManager battleManager,
        MiningStationScriptableObject miningStationScriptableObject,
        bool built) : base(battleObjectData, battleManager, miningStationScriptableObject, built) {
        this.miningStationScriptableObject = miningStationScriptableObject;
        faction.AddMiningStation(this);
        if (this.built) {
            SetGroup(faction.CreateNewUnitGroup("MiningGroup" + faction.stations.Count, true, new HashSet<Unit>(10)));
        }
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;


        foreach (AsteroidField asteroidField in faction.GetClosestAvailableAsteroidFields(positionGiver.position)) {
            Vector2 targetCenterPosition = Vector2.MoveTowards(asteroidField.position, positionGiver.position,
                asteroidField.GetSize() + GetSize() + 10);
            Vector2? targetLocationAsteroidField = battleManager.FindFreeLocationIncrement(
                new BattleManager.PositionGiver(targetCenterPosition, positionGiver), this);
            if (targetLocationAsteroidField.HasValue)
                return targetLocationAsteroidField.Value;
        }

        Vector2? targetLocation = battleManager.FindFreeLocationIncrement(positionGiver, this);
        if (targetLocation.HasValue)
            return targetLocation.Value;

        return positionGiver.position;
    }

    public override bool BuildStation() {
        if (!built) {
            SetGroup(faction.CreateNewUnitGroup("MiningGroup" + faction.stations.Count, true, new HashSet<Unit>(10)));
        }

        return base.BuildStation();
    }

    public int GetMiningRange() {
        return (int)(miningStationScriptableObject.miningRange * battleManager.systemSizeModifier);
    }
}
