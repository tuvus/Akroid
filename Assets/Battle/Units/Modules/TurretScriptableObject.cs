using UnityEngine;
using static Turret;

abstract class TurretScriptableObject : ComponentScriptableObject {
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

    public virtual float GetDamagePerSecond() {
        return 0;
    }

    public void OnValidate() {
        DPS = GetDamagePerSecond();
    }
}
