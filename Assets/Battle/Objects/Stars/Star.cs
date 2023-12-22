using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Star : BattleObject, IPositionConfirmer {
    SpriteRenderer glareRenderer;
    Color color;
    float targetBrightness;
    float brightnessSpeed;

    public void SetupStar(string name, BattleManager.PositionGiver positionGiver) {
        float scale = UnityEngine.Random.Range(30, 100);
        transform.localScale = new Vector2(scale, scale);
        base.SetupBattleObject(positionGiver, UnityEngine.Random.Range(0, 360));
        objectName = name;
        glareRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        color = Color.HSVToRGB(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(.8f, 1f), UnityEngine.Random.Range(.8f, 1f));
        spriteRenderer.color = color;
        glareRenderer.color = color;
        Vector2? targetPosition = BattleManager.Instance.FindFreeLocationIncrement(positionGiver, this);
        if (targetPosition.HasValue)
            transform.position = targetPosition.Value;
        else
            transform.position = positionGiver.position;
        this.position = transform.position;
        RandomiseGlareTarget();
        Spawn();
    }

    protected override float SetupSize() {
        return GetSpriteSize() * transform.localScale.x;
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

    public bool ConfirmPosition(Vector2 position, float minDistanceFromObject) {
        foreach (var star in BattleManager.Instance.stars) {
            float dist = Vector2.Distance(position, star.position);
            if (dist <= minDistanceFromObject + GetSize() + star.GetSize()) {
                return false;
            }
        }
        foreach (var planet in BattleManager.Instance.planets) {
            if (Vector2.Distance(position, planet.GetPosition()) <= minDistanceFromObject + planet.GetSize() + GetSize()) {
                return false;
            }
        }
        foreach (var station in BattleManager.Instance.stations) {
            if (Vector2.Distance(position, station.GetPosition()) <= minDistanceFromObject + station.GetSize() + GetSize()) {
                return false;
            }
        }
        return true;
    }

    public override float GetSpriteSize() {
        return spriteRenderer.sprite.bounds.size.x / 2;
    }
}