using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyEffect : MonoBehaviour, IParticleHolder {
    Unit unit;
    SpriteRenderer unitRenderer;
    [SerializeField] ParticleSystem explosion;
    [SerializeField] ParticleSystem fragments;

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
    }

    public void Explode() {
        explosion.Play(false);
        fragments.Play(false);
    }

    public bool IsPlaying() {
        return explosion.isPlaying || fragments.isPlaying;
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