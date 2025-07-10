using UnityEngine;
using UnityEngine.Audio;
using static Turret;

public abstract class TurretScriptableObject : ComponentScriptableObject {
    public float DPS;
    public Sprite turretSprite;

    public float range;
    public float rotateSpeed;
    public TargetingBehaviors targeting;
    public float fireSpeed;
    public float reloadSpeed;
    public int maxAmmo;
    public Vector2 baseScale = Vector2.one;
    public AudioResource turretFire;
    public float turretFirePitch;
    private float findNewTargetUpdateSpeed;
    public Vector2 spriteBounds { get; private set; }
    public float turretOffset { get; private set; }

    public virtual void Awake() {
        if (turretFire == null) turretFire = Resources.Load<AudioResource>("Prefabs/Audio/TurretFire");
    }

    public override void OnValidate() {
        DPS = GetDamagePerSecond();
        base.OnValidate();
        if (turretSprite != null) {
            spriteBounds = Calculator.GetSpriteBounds(turretSprite);
            turretOffset = (turretSprite.rect.size.y - turretSprite.pivot.y) /
                turretSprite.pixelsPerUnit;

        }
    }

    public virtual float GetDamagePerSecond() {
        return 0;
    }
}
