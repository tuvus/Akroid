using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;

public class LaserUI : BattleObjectUI {
    [SerializeField] private SpriteRenderer startHighlight;
    [SerializeField] private SpriteRenderer endHighlight;

    private Laser laser;
    private bool fireing;
    private AudioSource audioSource;

    public void Setup(BattleObject battleObject, UIManager uIManager, LaserTurretUI laserTurret) {
        base.Setup(battleObject, uIManager);
        laser = (Laser)battleObject;
        spriteRenderer.enabled = false;
        startHighlight.enabled = false;
        endHighlight.enabled = false;
        transform.localScale = new Vector2(laser.laserTurret.laserTurretScriptableObject.laserSize, 1) / laserTurret.laserTurret.scale;
        startHighlight.transform.localScale = new Vector2(.2f, .2f);
        endHighlight.transform.localScale = new Vector2(.2f, .2f);
        audioSource = transform.GetChild(2).gameObject.GetComponent<AudioSource>();
        audioSource.resource = Resources.Load<AudioResource>("Audio/Laser");
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 20;
        audioSource.maxDistance = 120;
        audioSource.dopplerLevel = 0;
        audioSource.volume = .2f;
        audioSource.loop = true;
    }

    public override void UpdateObject() {
        base.UpdateObject();
        if (laser.fireing) {
            if (!fireing) {
                startHighlight.enabled = uIManager.GetEffectsShown();
                audioSource.Play();
            }

            fireing = true;

            transform.localPosition = new Vector2(0, 0);
            transform.rotation = transform.parent.rotation;
            float laserLength = laser.laserLength / transform.lossyScale.y;
            spriteRenderer.size = new Vector2(spriteRenderer.size.x, laserLength);
            transform.Translate(Vector2.up * (laserLength / 2 + laser.laserTurret.GetTurretOffSet()));

            if (laser.hitPoint != null) {
                endHighlight.transform.localPosition = new Vector2(0, laserLength / 2);
                endHighlight.enabled = uIManager.GetEffectsShown();
            } else {
                endHighlight.enabled = false;
            }

            startHighlight.transform.localPosition = new Vector2(0, -laserLength / 2);

            if (laser.fireTime > 0) {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.b, spriteRenderer.color.g, .8f);
                startHighlight.color = new Color(startHighlight.color.r, startHighlight.color.b, startHighlight.color.g, .8f);
                endHighlight.color = new Color(endHighlight.color.r, endHighlight.color.b, endHighlight.color.g, 1);
            } else {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.b, spriteRenderer.color.g,
                    laser.fadeTime / laser.laserTurret.GetFireDuration());
                startHighlight.color = new Color(startHighlight.color.r, startHighlight.color.b, startHighlight.color.g,
                    laser.fadeTime / laser.laserTurret.GetFadeDuration());
                endHighlight.color = new Color(endHighlight.color.r, endHighlight.color.b, endHighlight.color.g,
                    laser.fadeTime / laser.laserTurret.GetFadeDuration());
            }

            Vector2 cameraPosition = uIManager.localPlayer.GetLocalPlayerInput().GetCamera().transform.position;
            // Move the audio source to the closest point on the laser to the camera.
            transform.GetChild(2).position = Vector2.MoveTowards(transform.position,
                Calculator.GetClosestPointToAPointOnALine(transform.position, laser.laserTurret.GetWorldRotation(), cameraPosition),
                math.min(Vector2.Distance(transform.position, cameraPosition), laserLength / 2));
        } else if (!laser.fireing && fireing) {
            fireing = false;
            startHighlight.enabled = false;
            endHighlight.enabled = false;
            audioSource.Stop();
        }
    }

    public void ShowEffects(bool shown) {
        startHighlight.enabled = fireing && shown;
        endHighlight.enabled = fireing && shown;
    }
}
