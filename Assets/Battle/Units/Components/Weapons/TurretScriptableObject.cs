using UnityEngine;
using UnityEngine.Serialization;
using static Turret;

public abstract class TurretScriptableObject : ComponentScriptableObject {
    public float DPS;
    public Sprite turretSprite;
    public float turretOffset;

    public float range;
    public float rotateSpeed;
    public TargetingBehaviors targeting;
    private float findNewTargetUpdateSpeed;
    public float fireSpeed;
    public float reloadSpeed;
    public int maxAmmo;
    public Vector2 baseScale = Vector2.one;

    public virtual float GetDamagePerSecond() {
        return 0;
    }

    public override void OnValidate() {
        DPS = GetDamagePerSecond();
        base.OnValidate();
    }
}
