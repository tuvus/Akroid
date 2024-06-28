using System;
using UnityEngine;

public class Star : BattleObject, IPositionConfirmer {
    SpriteRenderer glareRenderer;
    Color color;
    float targetBrightness;
    float brightnessSpeed;

    public void SetupStar(BattleManager battleManager, string name, BattleManager.PositionGiver positionGiver) {
        float scale = UnityEngine.Random.Range(30, 100);
        transform.localScale = new Vector2(scale, scale);
        base.SetupBattleObject(battleManager, positionGiver, UnityEngine.Random.Range(0, 360));
        objectName = name;
        glareRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        color = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(.8f, 1f), UnityEngine.Random.Range(.8f, 1f));
        spriteRenderer.color = color;
        glareRenderer.color = color;
        RandomiseGlareTarget();
        Spawn();
    }

    protected override float SetupSize() {
        return GetSpriteSize() * transform.localScale.x;
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

    public void UpdateStar(float deltaTime) {
        if (targetBrightness > color.a) {
            float changeRate = (targetBrightness - color.a) / brightnessSpeed * deltaTime;
            color = new Color(color.r, color.g, color.b, Math.Min(targetBrightness, color.a + changeRate));
            brightnessSpeed -= deltaTime;
            if (targetBrightness <= color.a || brightnessSpeed <= 0) {
                RandomiseGlareTarget();
            }
        } else {
            float changeRate = (targetBrightness - color.a) / brightnessSpeed * deltaTime;
            color = new Color(color.r, color.g, color.b, Math.Max(targetBrightness, color.a + changeRate));
            brightnessSpeed -= deltaTime;
            if (targetBrightness >= color.a || brightnessSpeed <= 0) {
                RandomiseGlareTarget();
            }
        }
        glareRenderer.color = color;
    }

    void RandomiseGlareTarget() {
        targetBrightness = UnityEngine.Random.Range(.5f, 1f);
        brightnessSpeed = UnityEngine.Random.Range(10f, 30f);
    }

    public override float GetSpriteSize() {
        return spriteRenderer.sprite.bounds.size.x / 2;
    }
}