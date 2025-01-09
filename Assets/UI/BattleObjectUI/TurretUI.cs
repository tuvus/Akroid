using UnityEngine;
using UnityEngine.Audio;

public class TurretUI : ComponentUI {
    private Turret turret;
    private AudioSource audioSource;

    public override void Setup(BattleObject battleObject, UIManager uIManager, UnitUI unitUI) {
        base.Setup(battleObject, uIManager, unitUI);
        turret = (Turret)battleObject;
        spriteRenderer.sprite = turret.turretScriptableObject.turretSprite;
        spriteRenderer.enabled = true;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.resource = Resources.Load<AudioResource>("Audio/TurretFire");
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 20;
        audioSource.maxDistance = 120;
        audioSource.dopplerLevel = 0;
        audioSource.volume = .2f;
        turret.OnFire += FireTurret;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (uIManager.GetFactionColoringShown()) spriteRenderer.color = unitUI.unit.faction.GetColorTint();
        else spriteRenderer.color = Color.white;
    }

    protected void FireTurret() {
        audioSource.Play();
    }

    public override void OnUnitDestroyed() {
        spriteRenderer.enabled = false;
    }
}
