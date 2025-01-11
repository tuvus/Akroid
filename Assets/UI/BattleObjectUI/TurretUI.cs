using UnityEngine;
using UnityEngine.Audio;

public class TurretUI : ComponentUI {
    private Turret turret;
    private AudioSource audioSource;
    private bool fire;

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
        turret.OnFire += () => fire = true;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (fire) {
            audioSource.Play();
            fire = false;
        }

        if (uIManager.GetFactionColoringShown()) spriteRenderer.color = unitUI.unit.faction.GetColorTint();
        else spriteRenderer.color = Color.white;
    }

    public override void OnUnitDestroyed() {
        spriteRenderer.enabled = false;
    }
}
