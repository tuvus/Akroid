using UnityEngine;

public class ReloadController {
    public float fireSpeed;
    public float reloadSpeed;
    public int maxAmmo;
    private float firetime;
    private float reloadTime;
    private int ammo;

    public ReloadController(float fireSpeed, float reloadSpeed, int maxAmmo) {
        this.fireSpeed = fireSpeed;
        this.reloadSpeed = reloadSpeed;
        this.maxAmmo = maxAmmo;
        firetime = 0;
        reloadTime = 0;
        ammo = maxAmmo;
    }

    public void UpdateReloadController(float time, float reloadModifier) {
        if (ammo == 0 || ammo != maxAmmo) {
            reloadTime = Mathf.Max(0, reloadTime - time * reloadModifier);
            if (reloadTime <= 0) {
                reloadTime = 0;
                ammo = maxAmmo;
            }
        }

        firetime = Mathf.Max(0, firetime - time);
    }

    public bool Fire() {
        if (ReadyToFire()) {
            ammo--;
            firetime = fireSpeed;
            reloadTime = reloadSpeed;
            return true;
        }

        return false;
    }

    public bool ReadyToFire() {
        return ammo > 0 && firetime <= 0;
    }

    public bool Empty() {
        return ammo == 0;
    }

    public bool ReadyToHibernate() {
        return ammo == maxAmmo && firetime <= 0 && reloadTime <= 0;
    }
}
