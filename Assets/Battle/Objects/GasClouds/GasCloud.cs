using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class GasCloud : BattleObject, IPositionConfirmer {
    public long resources;
    public CargoBay.CargoTypes gasCloudType;

    public struct GasCloudData {
        public float rotation;
        public float size;
        public long resources;
        public CargoBay.CargoTypes gasCloudType;

        public GasCloudData(float rotation, float size, long resources, CargoBay.CargoTypes gasCloudType) {
            this.rotation = rotation;
            this.size = size;
            this.resources = resources;
            this.gasCloudType = gasCloudType;
        }
    }

    public void SetupGasCloud(BattleManager battleManager, BattleManager.PositionGiver positionGiver, GasCloudData gasCloudData) {
        transform.localScale = new Vector2(gasCloudData.size, gasCloudData.size);
        base.SetupBattleObject(battleManager, positionGiver, UnityEngine.Random.Range(0, 360));
        UnityEngine.Color temp = UnityEngine.Color.HSVToRGB(UnityEngine.Random.Range(.25f, .29f), UnityEngine.Random.Range(.8f, 1f), UnityEngine.Random.Range(.6f, 8f));
        SetRotation(gasCloudData.rotation);
        this.resources = gasCloudData.resources;
        this.gasCloudType = gasCloudData.gasCloudType;
        GetSpriteRenderer().color = new UnityEngine.Color(temp.r, temp.g, temp.b, Random.Range(0.15f, 0.3f));
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
}
