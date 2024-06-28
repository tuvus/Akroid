using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class GasCloud : BattleObject, IPositionConfirmer {
    public void SetupGasCloud(BattleManager battleManager, BattleManager.PositionGiver positionGiver) {
        float scale = UnityEngine.Random.Range(25, 35);
        transform.localScale = new Vector2(scale, scale);
        base.SetupBattleObject(battleManager, positionGiver, UnityEngine.Random.Range(0, 360));
        UnityEngine.Color temp = UnityEngine.Color.HSVToRGB(UnityEngine.Random.Range(.25f, .29f), UnityEngine.Random.Range(.8f, 1f), UnityEngine.Random.Range(.6f, 8f));
        GetSpriteRenderer().color = new UnityEngine.Color(temp.r, temp.g, temp.b, Random.Range(0.15f, 0.3f));
        this.position = transform.position;
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
}
