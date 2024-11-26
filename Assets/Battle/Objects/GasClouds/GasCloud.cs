using UnityEngine;

public class GasCloud : BattleObject, IPositionConfirmer {
    public long resources;
    public CargoBay.CargoTypes gasCloudType;

    public GasCloud(BattleObjectData battleObjectData, BattleManager battleManager, long resources, CargoBay.CargoTypes gasCloudType) :
        base(battleObjectData, battleManager) {
        this.resources = resources;
        this.gasCloudType = gasCloudType;
        UnityEngine.Color temp = UnityEngine.Color.HSVToRGB(Random.Range(.25f, .29f), Random.Range(.8f, 1f), Random.Range(.6f, 8f));

        Spawn();
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            return targetPosition.Value;
        return positionGiver.position;
    }

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var blockingObject in battleManager.GetPositionBlockingObjects()) {
            if (Vector2.Distance(position, blockingObject.GetPosition()) <= minDistanceFromObject + GetSize() + blockingObject.GetSize()) {
                return false;
            }
        }

        return true;
    }

    /// <returns>The amount mined</returns>
    public long CollectGas(long amount) {
        if (resources > amount) {
            resources -= amount;
            return amount;
        }

        long returnValue = resources;
        resources = 0;
        return returnValue;
    }

    public bool HasResources() {
        return resources > 0;
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/GasCloud");
    }
}
