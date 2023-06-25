using UnityEngine;
using static Turret;

    [CreateAssetMenu(fileName = "Resources/Components/MassTurretScriptableObject", menuName = "Components/ProjectileTurret", order = 1)]
    class MassTurretScriptableObject : TurretScriptableObject {
        public float fireVelocity;
        public float fireAccuracy;
        public int minDamage;
        public int maxDamage;
        public float projectileRange;
        public GameObject projectilePrefab;

        public override float GetDamagePerSecond() {
            float time = reloadSpeed;
            if (maxAmmo > 1) {
                time += maxAmmo * fireSpeed;
            }
            float damage = (minDamage + maxDamage) / 2f * maxAmmo;
            return damage / time;
        }

        public void Awake() {
            targeting = TargetingBehaviors.closest;
            if (projectilePrefab == null)
                projectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
        }
    }
