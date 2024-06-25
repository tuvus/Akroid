using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyEffect : MonoBehaviour, IParticleHolder {
    BattleObject battleObject;
    SpriteRenderer unitRenderer;
    [SerializeField] ParticleSystem explosion;
    [SerializeField] ParticleSystem fragments;
    [SerializeField] LensFlare flare;
    [SerializeField] public float flareSpeed;
    [SerializeField] public float fadeSpeed;
    float targetBrightness;
    float flareTime;
    FlareState flareState;

    enum FlareState {
        FlaringUp,
        FadeToNormal,
        KeepNormal,
        Fade,
        End,
    }

    public void SetupDestroyEffect(BattleObject battleObject, SpriteRenderer targetRenderer) {
        this.battleObject = battleObject;
        unitRenderer = targetRenderer;
        float newScale = battleObject.GetSpriteSize() * transform.parent.localScale.x;
        transform.localScale = new Vector2(newScale, newScale);
        var shape = explosion.shape;
        shape.spriteRenderer = targetRenderer;
        shape.scale = new Vector2(transform.parent.localScale.x, transform.parent.localScale.x);
        shape = fragments.shape;
        shape.spriteRenderer = targetRenderer;
        shape.scale = new Vector2(transform.parent.localScale.x, transform.parent.localScale.x);
        flare.enabled = false;
        flare.brightness = 0;
        targetBrightness = getBaseFlareSize() * 2;
        flareTime = 0;
        flareState = FlareState.FlaringUp;
    }

    public void Explode() {
        if (BattleManager.Instance.GetParticlesShown()) {
            explosion.Play(false);
            fragments.Play(false);
        }
        if (BattleManager.Instance.GetEffectsShown())
            flare.enabled = true;
        flareTime = 0;
        UpdateExplosion(0);
    }

    public void UpdateExplosion(float deltaTime) {
        flareTime += deltaTime;
        switch (flareState) {
            case FlareState.FlaringUp:
                flare.brightness = targetBrightness * flareTime / flareSpeed;
                if (flareTime > flareSpeed) {
                    flareState = FlareState.FadeToNormal;
                    flareTime = 0;
                }
                break;
            case FlareState.FadeToNormal:
                flare.brightness = (targetBrightness - getBaseFlareSize() + 3) / Mathf.Pow(1 + 8 * flareTime / fadeSpeed, 3) + getBaseFlareSize() - 3;
                if (flare.brightness <= getBaseFlareSize() * 1.01) {
                    flareState = FlareState.KeepNormal;
                    flareTime = 0;
                }
                break;
            case FlareState.KeepNormal:
                flare.brightness = getBaseFlareSize();
                if (!explosion.isEmitting) {
                    battleObject.GetSpriteRenderers().ForEach(r => r.enabled = false);
                    flareState = FlareState.Fade;
                    flareTime = 0;
                }
                break;
            case FlareState.Fade:
                flare.brightness = getBaseFlareSize() * (1 / Mathf.Pow(1 + flareTime, 2));
                if (explosion.particleCount == 0 || flare.brightness < 2) {
                    flare.brightness = 0;
                    flare.enabled = false;
                    flareState = FlareState.End;
                }
                break;
        }
    }

    public bool IsPlaying() {
        return explosion.isPlaying || fragments.isPlaying || (flare.brightness > 0 && flare.enabled);
    }

    public void ShowEffects(bool shown) {
        flare.enabled = shown;
    }

    public void SetParticleSpeed(float speed) {
        var main = explosion.main;
        main.simulationSpeed = speed;
        main = fragments.main;
        main.simulationSpeed = speed;
    }

    public void ShowParticles(bool shown) {
        if (!shown && explosion.IsAlive()) {
            explosion.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (!shown && fragments.IsAlive()) {
            fragments.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private float getBaseFlareSize() {
        return battleObject.GetSpriteSize() * 30;
    }
}