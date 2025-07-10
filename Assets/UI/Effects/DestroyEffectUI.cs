using System;
using UnityEngine;
using FlareState = DestroyEffect.FlareState;

public class DestroyEffectUI : MonoBehaviour, IParticleHolder {
    [SerializeField] private ParticleSystem explosion;
    [SerializeField] private ParticleSystem fragments;
    [SerializeField] private LensFlare flare;
    private BattleObjectUI battleObjectUI;
    private DestroyEffect destroyEffect;
    private DestroyEffectScriptableObject destroyEffectScriptableObject;
    private UIManager uIManager;

    public void ShowEffects(bool shown) {
        flare.enabled = shown;
    }

    public void SetParticleSpeed(float speed) {
        ParticleSystem.MainModule main = explosion.main;
        main.simulationSpeed = speed;
        main = fragments.main;
        main.simulationSpeed = speed;
    }

    public void SetupDestroyEffect(BattleObjectUI battleObjectUI,
        DestroyEffectScriptableObject destroyEffectScriptableObject,
        UIManager uIManager, SpriteRenderer targetRenderer) {
        this.battleObjectUI = battleObjectUI;
        this.uIManager = uIManager;
        this.destroyEffectScriptableObject = destroyEffectScriptableObject;
        float newScale = this.battleObjectUI.battleObject.GetSpriteSize() * battleObjectUI.transform.localScale.x;
        transform.localScale = new Vector2(newScale, newScale);
        explosion.transform.localScale = transform.localScale;
        uIManager.uiBattleManager.particleHolders.Add(this);

        ParticleSystem.MainModule explosionMain = explosion.main;
        explosionMain.startLifetime = destroyEffectScriptableObject.flareNormalSpeed +
            destroyEffectScriptableObject.flareFadeSpeed;
        explosionMain.simulationSpeed = uIManager.GetParticleSpeed();
        ParticleSystem.MainModule fragmentsMain = fragments.main;
        fragmentsMain.startLifetime = destroyEffectScriptableObject.flareNormalSpeed +
            destroyEffectScriptableObject.flareFadeSpeed;
        fragmentsMain.simulationSpeed = uIManager.GetParticleSpeed();
        ParticleSystem.ShapeModule explosionShape = explosion.shape;
        explosionShape.spriteRenderer = targetRenderer;
        explosionShape.scale = new Vector2(transform.parent.localScale.x, transform.parent.localScale.x);
        ParticleSystem.ShapeModule fragmentsShape = fragments.shape;
        fragmentsShape.spriteRenderer = targetRenderer;
        fragmentsShape.scale = new Vector2(transform.parent.localScale.x, transform.parent.localScale.x);
        ParticleSystem.EmissionModule explosionEmission = explosion.emission;
        explosionEmission.enabled = true;
        ParticleSystem.EmissionModule fragmentEmission = fragments.emission;
        fragmentEmission.enabled = true;

        flare.enabled = false;
        flare.brightness = 0;
    }

    public void Explode(DestroyEffect destroyEffect) {
        this.destroyEffect = destroyEffect;
        if (uIManager.GetParticlesShown() &&
            uIManager.localPlayer.GetInputManager().IsObjectInViewingField(battleObjectUI, 120)) {
            explosion.Play(false);
            fragments.Play(false);
        }

        if (uIManager.GetEffectsShown())
            flare.enabled = true;
        UpdateExplosion();
    }

    public void UpdateExplosion() {
        if (!uIManager.GetEffectsShown()) {
            ShowEffects(false);
            ShowParticles(false);
        } else if (!uIManager.GetParticlesShown()) {
            ShowParticles(false);
        }

        switch (destroyEffect.flareState) {
            case FlareState.FlaringUp:
                flare.brightness = GetFlareUpSize() * destroyEffect.flareTime /
                    destroyEffectScriptableObject.flareUpSpeed;
                break;
            case FlareState.FadeToNormal:
                flare.brightness = GetBaseFlareSize() + (GetFlareUpSize() - GetBaseFlareSize()) *
                    (float)(1 - Math.Pow(destroyEffect.flareTime / destroyEffectScriptableObject.flareUpFadeSpeed, 2));
                break;
            case FlareState.KeepNormal:
                ParticleSystem.EmissionModule explosionEmission = explosion.emission;
                explosionEmission.enabled = false;
                ParticleSystem.EmissionModule fragmentEmission = fragments.emission;
                fragmentEmission.enabled = false;
                flare.brightness = GetBaseFlareSize();
                break;
            case FlareState.Fade:
                flare.brightness = GetBaseFlareSize() *
                    (float)(1 - Math.Pow(destroyEffect.flareTime / destroyEffectScriptableObject.flareFadeSpeed, 2));
                break;
            case FlareState.End:
                flare.brightness = 0;
                flare.enabled = false;
                break;
        }
    }

    public void OnBattleObjectRemoved() {
        uIManager.uiBattleManager.particleHolders.Remove(this);
    }

    public void ShowParticles(bool shown) {
        if (!shown && explosion.IsAlive()) {
            explosion.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (!shown && fragments.IsAlive()) {
            fragments.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private float GetBaseFlareSize() {
        return battleObjectUI.battleObject.GetSpriteSize() * 30 * destroyEffectScriptableObject.flareSizeMult;
    }

    private float GetFlareUpSize() {
        return battleObjectUI.battleObject.GetSpriteSize() * 30 * destroyEffectScriptableObject.flareSizeMult *
            destroyEffectScriptableObject.flareUpSizeMult;
    }
}
