using UnityEngine;

public class GasCloud : BattleObject, IPositionConfirmer {
    public GasCloudScriptableObject gasCloudScriptableObject { get; private set; }
    public long resources;
    public Color color { get; private set; }


    public GasCloud(BattleObjectData battleObjectData, BattleManager battleManager, long resources,
        GasCloudScriptableObject gasCloudScriptableObject) : base(battleObjectData, battleManager) {
        this.gasCloudScriptableObject = gasCloudScriptableObject;
        this.resources = resources;
        color = UnityEngine.Color.HSVToRGB(random.NextFloat(.25f, .29f), random.NextFloat(.8f, 1f), random.NextFloat(.6f, 8f));
        color = new Color(color.r, color.g, color.b, .5f);
        visible = true;
        Spawn();
        SetSize(SetupSize());
    }

    protected override Vector2 GetSetupPosition(BattleManager.PositionGiver positionGiver) {
        if (positionGiver.isExactPosition)
            return positionGiver.position;
        Vector2? targetPosition = battleManager.FindFreeLocationIncrement(positionGiver, this);
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

    public override float GetSpriteSize() {
        return Calculator.GetSpriteSizeFromBounds(gasCloudScriptableObject.spriteBounds, scale);
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/GasCloud");
    }
}
