using UnityEngine;

public class GasCloud : BattleObject, IPositionConfirmer {
    public GasCloudScriptableObject gasCloudScriptableObject { get; private set; }
    public long resources;
    public Color color { get; private set; }


    public GasCloud(BattleObjectData battleObjectData, BattleManager battleManager, long resources,
        GasCloudScriptableObject gasCloudScriptableObject) : base(battleObjectData, battleManager) {
        this.gasCloudScriptableObject = gasCloudScriptableObject;
        this.resources = resources;
        color = UnityEngine.Color.HSVToRGB(Random.Range(.25f, .29f), Random.Range(.8f, 1f), Random.Range(.6f, 8f));
        Spawn();
        SetSize(SetupSize());
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

    protected override float SetupSize() {
        return GetSpriteSize() * scale.x;
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

    public override float GetSpriteSize() {
        return Calculator.GetSpriteSize(gasCloudScriptableObject.sprite, scale);
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/GasCloud");
    }
}
