using UnityEngine;

public class ProjectileUI : BattleObjectUI, IParticleHolder {
    [SerializeField] private SpriteRenderer highlight;
    [SerializeField] private new ParticleSystem particleSystem;
    private bool hit;
    private LocalPlayerInput localPlayerInput;

    private Projectile projectile;

    public void ShowEffects(bool shown) { }

    public void SetParticleSpeed(float speed) {
        ParticleSystem.MainModule main = particleSystem.main;
        main.simulationSpeed = speed;
    }

    public override void Setup(BattleObject battleObject, UIManager uIManager) {
        base.Setup(battleObject, uIManager);
        projectile = (Projectile)battleObject;
        spriteRenderer.enabled = true;
        hit = false;
        highlight.enabled = uIManager.GetEffectsShown();
        localPlayerInput = uIManager.localPlayer.GetInputManager();
        uIManager.uiBattleManager.objectsToUpdate.Add(this);
        uIManager.uiBattleManager.particleHolders.Add(this);
        ParticleSystem.MainModule main = particleSystem.main;
        main.simulationSpeed = uIManager.GetParticleSpeed();
    }

    public override void UpdateObject() {
        if (!projectile.spawned) {
            spriteRenderer.enabled = false;
            highlight.enabled = false;
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            uIManager.uiBattleManager.objectsToUpdate.Remove(this);
        }
        base.UpdateObject();
        if (projectile.hit && !hit && localPlayerInput.ShouldShowCloseUpGraphics() &&
            localPlayerInput.IsObjectInViewingField(this)) {
            hit = true;
            if (uIManager.GetParticlesShown()) particleSystem.Play();
            highlight.enabled = false;
        }
    }

    public override void OnBattleObjectRemoved() {
        base.OnBattleObjectRemoved();
        uIManager.uiBattleManager.particleHolders.Remove(this);
        highlight.enabled = false;
        particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        spriteRenderer.enabled = false;
    }

    public void ShowParticles(bool shown) { }
}
