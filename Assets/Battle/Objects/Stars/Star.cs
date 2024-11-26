using System;
using UnityEngine;

public class Star : BattleObject, IPositionConfirmer {
    public StarScriptableObject starScriptableObject { get; private set; }
    public Color color { get; private set; }
    float targetBrightness;
    float brightnessSpeed;

    public Star(BattleObjectData battleObjectData, BattleManager battleManager, StarScriptableObject starScriptableObject) :
        base(battleObjectData, battleManager) {
        this.starScriptableObject = starScriptableObject;
        color = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(.8f, 1f), UnityEngine.Random.Range(.8f, 1f));
        RandomiseGlareTarget();
        Spawn();
        SetSize(SetupSize());
    }

    protected override float SetupSize() {
        return GetSpriteSize() * scale.x;
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
    }

    void RandomiseGlareTarget() {
        targetBrightness = UnityEngine.Random.Range(.5f, 1f);
        brightnessSpeed = UnityEngine.Random.Range(10f, 30f);
    }

    public override float GetSpriteSize() {
        Sprite sprite = starScriptableObject.sprite;
        return Mathf.Max(Vector2.Distance(sprite.bounds.center, new Vector2(sprite.bounds.size.x, sprite.bounds.size.y)),
            Vector2.Distance(sprite.bounds.center, new Vector2(sprite.bounds.size.y, sprite.bounds.size.z)),
            Vector2.Distance(sprite.bounds.center, new Vector2(sprite.bounds.size.z, sprite.bounds.size.x))) / 2 * scale.y;
    }

    public override GameObject GetPrefab() {
        return (GameObject)Resources.Load("Prefabs/Star");
    }
}
