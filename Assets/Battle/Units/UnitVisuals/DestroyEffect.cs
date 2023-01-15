using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;

public class DestroyEffect : MonoBehaviour, IParticleHolder {
    Unit unit;
    SpriteRenderer unitRenderer;
    [SerializeField] ParticleSystem explosion;
    [SerializeField] ParticleSystem fragments;
    [SerializeField] LensFlare flare;
    [SerializeField] public float flareSpeed;
    [SerializeField] public float fadeSpeed;
    float targetBrightness;
    float flareTime;

    public void SetupDestroyEffect(Unit unit, SpriteRenderer targetRenderer) {
        this.unit = unit;
        unitRenderer = targetRenderer;
        float newScale = unit.GetSpriteSize() * transform.parent.localScale.x;
        transform.localScale = new Vector2(newScale, newScale);
        var shape = explosion.shape;
        shape.spriteRenderer = targetRenderer;
        shape.scale = new Vector2(transform.parent.localScale.x, transform.parent.localScale.x);
        shape = fragments.shape;
        shape.spriteRenderer = targetRenderer;
        shape.scale = new Vector2(transform.parent.localScale.x, transform.parent.localScale.x);
        flare.enabled = false;
        flare.brightness = 0;
        targetBrightness = unit.GetSize() * 80;
        flareTime = 0;
    }

    public void Explode() {
        if (BattleManager.Instance.GetParticlesShown()) {
            explosion.Play(false);
            fragments.Play(false);
        }
        if (BattleManager.Instance.GetEffectsShown())
            flare.enabled = true;
        UpdateExplosion(0);
    }

    public void UpdateExplosion(float deltaTime) {
        flareTime += deltaTime;
        if (flareTime <= flareSpeed) {
            flare.brightness = targetBrightness * flareTime / flareSpeed;
        } else if (flareTime <= flareSpeed + fadeSpeed) {
            flare.brightness = targetBrightness * (1 / Mathf.Pow(1 + flareTime - flareSpeed, 2));
        } else if (flare.enabled) {
            flare.brightness = 0;
            flare.enabled = false;
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
}